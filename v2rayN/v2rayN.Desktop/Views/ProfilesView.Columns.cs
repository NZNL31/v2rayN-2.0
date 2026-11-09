using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DialogHostAvalonia;
using v2rayN.Desktop.Common;

namespace v2rayN.Desktop.Views;

public partial class ProfilesView
{    /// <summary>窄屏时按这个顺序优先隐藏（越靠前越先隐藏）</summary>
    private static readonly string[] OptionalColumns =
    [
        "SubRemarks", "Network", "StreamSecurity", "IpInfo",
        "TodayUp", "TodayDown", "TotalUp", "TotalDown", "RowActions", "Port", "SpeedVal", "DelayVal"
    ];

    /// <summary>被自动隐藏的列（不代表用户意愿）</summary>
    private readonly HashSet<string> _autoHiddenCols = [];

    private bool _adjustingCols;

    /// <summary>
    /// 列区域之外被占用的宽度：行头列 + 边框 2 + 竖向滚动条 8 + 余量。
    /// 不扣掉它，窄屏时列宽会多算 ~46px，最后一列会被裁。
    /// </summary>
    private const double GridChromeWidth = 46;

    /// <summary>别名占弹性区的比例：默认 0.4（=2:3），用户拖过列宽后会被记住</summary>
    private double _flexRatio = 0.4;

    private const double FlexRatioMin = 0.15;
    private const double FlexRatioMax = 0.85;


    private DispatcherTimer? _adjustDebounce;

    private DispatcherTimer? _filterDebounce;

    private DataGridColumn? FindColumn(string tag) =>
        lstProfiles.Columns.FirstOrDefault(c => c.Tag?.ToString() == tag);

    /// <summary>拖窗口时防抖，避免每一帧都重算列宽</summary>
    private void ScheduleAdjustColumns()
    {
        _adjustDebounce ??= new DispatcherTimer(TimeSpan.FromMilliseconds(120), DispatcherPriority.Background, (_, _) =>
        {
            _adjustDebounce?.Stop();
            AdjustColumnsForWidth();
        });
        _adjustDebounce.Stop();
        _adjustDebounce.Start();
    }

    /// <summary>用户拖过别名/地址的列宽后记住比例（记比例而不是像素，窗口缩放时仍然自适应）</summary>
    private void CaptureFlexRatio()
    {
        var nameCol = FindColumn("Remarks");
        var addrCol = FindColumn("Address");
        if (nameCol is null || addrCol is null)
        {
            return;
        }

        var nw = nameCol.ActualWidth;
        var aw = addrCol.ActualWidth;
        if (nw <= 0 || aw <= 0)
        {
            return;
        }

        var ratio = nw / (nw + aw);

        // 与当前比例相差 8% 以内视为没动过（避开窄屏下 MinWidth 夹出来的小偏差）
        if (Math.Abs(ratio - _flexRatio) <= 0.08)
        {
            return;
        }

        _flexRatio = Math.Clamp(ratio, FlexRatioMin, FlexRatioMax);
    }

    /// <summary>
    /// 放不下就隐藏低优先级列，放得下就恢复；别名/地址用星号比例交给布局引擎分配，
    /// 由 DataGrid 自己算剩余宽度（配合禁用横向滚动条，不会再出现残余滚动条）。
    /// </summary>
    private void AdjustColumnsForWidth(int retries = 1)
    {
        if (_adjustingCols || lstProfiles?.Columns is null)
        {
            return;
        }

        var avail = lstProfiles.Bounds.Width - GridChromeWidth;
        if (avail <= 0)
        {
            return;
        }

        _adjustingCols = true;
        try
        {
            CaptureFlexRatio();

            const double flexMin = 300; // 别名 140 + 地址 160

            double FixedUsed()
            {
                var used = 0d;
                foreach (var col in lstProfiles.Columns)
                {
                    if (!col.IsVisible || IsFlexColumn(col))
                    {
                        continue;
                    }
                    used += col.ActualWidth > 0 ? col.ActualWidth : 90;
                }
                return used;
            }

            var changed = false;

            // 1) 仍然放不下：从最不重要的开始隐藏
            foreach (var tag in OptionalColumns)
            {
                if (FixedUsed() + flexMin <= avail)
                {
                    break;
                }
                var col = FindColumn(tag);
                if (col is null || !col.IsVisible)
                {
                    continue;
                }
                col.IsVisible = false;
                _autoHiddenCols.Add(tag);
                changed = true;
            }

            // 2) 放得下就尽量恢复（最后隐藏的最先回来）
            for (var i = OptionalColumns.Length - 1; i >= 0; i--)
            {
                var tag = OptionalColumns[i];
                if (!_autoHiddenCols.Contains(tag))
                {
                    continue;
                }
                var col = FindColumn(tag);
                if (col is null)
                {
                    _autoHiddenCols.Remove(tag);
                    continue;
                }
                if (FixedUsed() + flexMin + 100 <= avail)
                {
                    col.IsVisible = true;
                    _autoHiddenCols.Remove(tag);
                    changed = true;
                }
            }

            // 3) 别名 / 地址按记住的比例给星号权重，剩余宽度由布局引擎分配
            var nameCol = FindColumn("Remarks");
            var addrCol = FindColumn("Address");
            if (nameCol is not null && addrCol is not null)
            {
                nameCol.Width = new DataGridLength(_flexRatio, DataGridLengthUnitType.Star);
                addrCol.Width = new DataGridLength(1 - _flexRatio, DataGridLengthUnitType.Star);
            }

            // 隐藏/恢复列会改变布局，拿到的 ActualWidth 是旧的：有变化就再复核一轮（最多 1 次）
            if (changed && retries > 0)
            {
                Dispatcher.UIThread.Post(() => AdjustColumnsForWidth(retries - 1), DispatcherPriority.Background);
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
        }
        finally
        {
            _adjustingCols = false;
        }
    }

    private void BtnAutofitColumnWidth_Click(object? sender, RoutedEventArgs e)
    {
        AutofitColumnWidth();
    }

    /// <summary>工具栏「更新当前订阅」：先不通过代理，再通过代理</summary>
    private void BtnSubUpdateTop_Click(object? sender, RoutedEventArgs e)
    {
        AppEvents.SubGroupUpdateBothRequested.Publish();
    }

    private void AutofitColumnWidth()
    {
        // 手动「自动调整列宽」= 放弃记住的比例，回到 2:3
        _flexRatio = 0.4;

        try
        {
            //First scroll horizontally to the initial position to avoid the control crash bug
            if (lstProfiles.SelectedIndex >= 0)
            {
                lstProfiles.ScrollIntoView(lstProfiles.SelectedItem, lstProfiles.Columns[0]);
            }
            else
            {
                var model = lstProfiles.ItemsSource.Cast<ProfileItemModel>();
                if (model.Any())
                {
                    lstProfiles.ScrollIntoView(model.First(), lstProfiles.Columns[0]);
                }
                else
                {
                    return;
                }
            }

            // 短列先按内容自适应；别名/地址不参与，避免超长 IPv6 把整行撑爆
            foreach (var it in lstProfiles.Columns)
            {
                if (!it.IsVisible || IsFlexColumn(it))
                {
                    continue;
                }
                it.Width = new DataGridLength(1, DataGridLengthUnitType.Auto);
            }

            // 等一次布局后：短列夹到合理区间，再把剩下的宽度按 2:3 分给别名/地址
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    foreach (var it in lstProfiles.Columns)
                    {
                        if (!it.IsVisible || IsFlexColumn(it))
                        {
                            continue;
                        }
                        var w = it.ActualWidth;
                        if (w <= 0)
                        {
                            continue;
                        }
                        it.Width = new DataGridLength(Math.Clamp(Math.Round(w), 56, 220), DataGridLengthUnitType.Pixel);
                    }

                    // 交给统一的宽度分配：必要时隐藏低优先级列，别名/地址吃剩余
                    AdjustColumnsForWidth();
                }
                catch (Exception ex)
                {
                    Logging.SaveLog(_tag, ex);
                }
            }, DispatcherPriority.Background);
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
        }
    }

    /// <summary>别名 / 地址：按比例伸缩，不参与「自动调整列宽」的内容测量</summary>
    private static bool IsFlexColumn(DataGridColumn col)
    {
        var tag = col.Tag?.ToString();
        return tag is "Remarks" or "Address";
    }

    private void RestoreUI()
    {
        try
        {
            var lvColumnItem = _config.UiItem.MainColumnItem.OrderBy(t => t.Index).ToList();
            var displayIndex = 0;

            // 上次记住的别名宽度比例（存的是比例，不是像素，窗口缩放仍然自适应）
            var savedRatio = lvColumnItem.FirstOrDefault(t => t.Name == "Remarks")?.Ratio ?? 0;
            if (savedRatio > 0)
            {
                _flexRatio = Math.Clamp(savedRatio, FlexRatioMin, FlexRatioMax);
            }
            foreach (var item in lvColumnItem)
            {
                foreach (var item2 in lstProfiles.Columns)
                {
                    if (item2.Tag == null)
                    {
                        continue;
                    }
                    if (item2.Tag.Equals(item.Name))
                    {
                        if (item.Width < 0)
                        {
                            item2.IsVisible = false;
                        }
                        else
                        {
                            // 星号列（别名/地址）保持按比例伸缩，不被配置里的像素宽度覆盖
                            if (!item2.Width.IsStar)
                            {
                                item2.Width = new DataGridLength(item.Width, DataGridLengthUnitType.Pixel);
                            }
                            item2.DisplayIndex = displayIndex++;
                        }
                        if (item.Name.StartsWith("to", StringComparison.CurrentCultureIgnoreCase))
                        {
                            // 用户手动隐藏过就不再强制显示
                            item2.IsVisible = item.Width >= 0 && _config.GuiItem.EnableStatistics;
                        }
                        if (item.Name.Equals("IpInfo", StringComparison.CurrentCultureIgnoreCase))
                        {
                            item2.IsVisible = item.Width >= 0 && _config.SpeedTestItem.IPAPIUrl.IsNotEmpty() && !_config.UiItem.HideColumnIpInfo;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
        }
    }

    private void StorageUI()
    {
        try
        {
            List<ColumnItem> lvColumnItem = [];
            foreach (var item2 in lstProfiles.Columns)
            {
                if (item2.Tag == null)
                {
                    continue;
                }
                lvColumnItem.Add(new()
                {
                    Name = (string)item2.Tag,
                    Width = _autoHiddenCols.Contains((string)item2.Tag)
                        ? 100
                        : (int)(item2.IsVisible == true ? item2.ActualWidth : -1),
                    Index = item2.DisplayIndex,
                    Ratio = (string)item2.Tag == "Remarks" ? _flexRatio : 0
                });
            }
            _config.UiItem.MainColumnItem = lvColumnItem;
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
        }
    }
}
