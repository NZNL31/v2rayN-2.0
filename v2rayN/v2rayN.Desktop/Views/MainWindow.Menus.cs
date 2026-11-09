using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using DialogHostAvalonia;
using v2rayN.Desktop.Base;
using v2rayN.Desktop.Common;
using v2rayN.Desktop.Manager;

namespace v2rayN.Desktop.Views;

public partial class MainWindow
{    private void BtnHeaderSetting_Click(object? sender, RoutedEventArgs e)
    {
        _sideMenuAnchor = sender as Control;
        ShowMenuAt(menuSetting, PlacementMode.BottomEdgeAlignedRight);
    }

    /// <summary>顶栏纸飞机按钮：弹出原「帮助」菜单</summary>
    private void BtnHeaderHelp_Click(object? sender, RoutedEventArgs e)
    {
        _sideMenuAnchor = sender as Control;
        ShowMenuAt(menuHelp, PlacementMode.BottomEdgeAlignedRight);
    }

    /// <summary>左栏「订阅分组」小标题右侧的 ⋯：弹出订阅分组菜单</summary>
    private void BtnRailSubMenu_Click(object? sender, RoutedEventArgs e)
    {
        _sideMenuAnchor = sender as Control;
        ShowMenuAt(menuSubscription, PlacementMode.BottomEdgeAlignedRight);
    }

    /// <summary>左侧栏「配置项」按钮：展开原配置项菜单</summary>
    public void OpenConfigMenu(Control? anchor = null)
    {
        _sideMenuAnchor = anchor;
        ShowMenuAt(menuServers);
    }

    /// <summary>左侧栏「设置」按钮：展开原设置菜单</summary>
    public void OpenSettingMenu(Control? anchor = null)
    {
        _sideMenuAnchor = anchor;
        ShowMenuAt(menuSetting);
    }

    /// <summary>左栏「配置项」按钮：弹出原配置项菜单（向右展开）</summary>
    private void BtnRailConfig_Click(object? sender, RoutedEventArgs e)
    {
        _sideMenuAnchor = sender as Control;
        ShowMenuAt(menuServers, PlacementMode.RightEdgeAlignedTop);
    }
    /// <summary>
    /// 把某个菜单的子项临时搬进 MenuFlyout，在锚点旁边弹出；关闭后再搬回去
    /// </summary>
    private void ShowMenuAt(MenuItem source, PlacementMode placement = PlacementMode.BottomEdgeAlignedLeft)
    {
        try
        {
            var anchor = _sideMenuAnchor;
            if (anchor is null)
            {
                source.Open();
                return;
            }

            var flyout = new MenuFlyout { Placement = placement };
            var moved = new List<object>();

            while (source.Items.Count > 0)
            {
                var child = source.Items[0];
                source.Items.RemoveAt(0);
                flyout.Items.Add(child);
                moved.Add(child);
            }

            flyout.Closed += (_, _) =>
            {
                foreach (var item in moved)
                {
                    flyout.Items.Remove(item);
                    source.Items.Add(item);
                }
            };

            flyout.ShowAt(anchor);
        }
        catch (Exception ex)
        {
            Logging.SaveLog("ShowMenuAt", ex);
            source.Open();
        }
    }
    private Control? _sideMenuAnchor;

    private bool _logCollapsed = true;

    private void MenuSettingsSetUWP_Click(object? sender, RoutedEventArgs e)
    {
        ProcUtils.ProcessStart(Utils.GetBinPath("EnableLoopback.exe"));
    }

    public async Task AddServerViaClipboardAsync()
    {
        var clipboardData = await AvaUtils.GetClipboardData(this);
        if (clipboardData.IsNotEmpty() && ViewModel != null)
        {
            await ViewModel.AddServerViaClipboardAsync(clipboardData);
        }
    }

    public async Task ScanScreenTaskAsync()
    {
        ShowHideWindow(false);

        await Task.Delay(200);

        var bytes = QRCodeAvaloniaUtils.CaptureScreen();
        if (bytes != null && ViewModel != null)
        {
            await ViewModel.ScanScreenResult(bytes);
        }

        ShowHideWindow(true);
    }

    private void MenuCheckUpdate_Click(object? sender, RoutedEventArgs e)
    {
        _checkUpdateView ??= new CheckUpdateView();
        _checkUpdateView.ViewModel = ViewModel?.CheckUpdateViewModel;
        DialogHost.Show(_checkUpdateView);

        AppEvents.HasUpdateNotified.Publish(false);
    }

    private void MenuBackupAndRestore_Click(object? sender, RoutedEventArgs e)
    {
        _backupAndRestoreView ??= new BackupAndRestoreView();
        _backupAndRestoreView.ViewModel = ViewModel?.BackupAndRestoreViewModel;
        DialogHost.Show(_backupAndRestoreView);
    }

    private async void MenuClose_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (await UI.ShowYesNo(ResUI.menuExitTips) != ButtonResult.Yes)
            {
                return;
            }

            _blCloseByUser = true;
            StorageUI();

            await AppManager.Instance.AppExitAsync(true);
        }
        catch
        {
            // Ignore
        }
    }

    private void Shutdown(bool obj)
    {
        if (obj is bool b && _blCloseByUser == false)
        {
            _blCloseByUser = b;
        }
        StorageUI();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            HotkeyManager.Instance.Dispose();
            desktop.Shutdown();
        }
    }

    private ServiceLib.Models.Entities.SubItem? _railDragSub;
    private ServiceLib.Models.Entities.SubItem? _railSelectedBefore;
    private Point _railDragStart;
    private bool _railDragMoved;

    private void RailGroup_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _railDragSub = null;
        _railDragMoved = false;

        if (!e.GetCurrentPoint(lstGroupRail).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var container = (e.Source as Visual)?.FindAncestorOfType<ListBoxItem>();
        if (container?.DataContext is not ServiceLib.Models.Entities.SubItem sub || sub.Id.IsNullOrEmpty())
        {
            return;
        }

        _railDragSub = sub;
        _railSelectedBefore = ViewModel?.ProfilesViewModel?.SelectedSub;
        _railDragStart = e.GetPosition(lstGroupRail);
    }

    private void RailGroup_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_railDragSub is null || !e.GetCurrentPoint(lstGroupRail).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var pos = e.GetPosition(lstGroupRail);
        if (!_railDragMoved && Math.Abs(pos.Y - _railDragStart.Y) < 5)
        {
            return;
        }

        if (!_railDragMoved)
        {
            _railDragMoved = true;
            e.Pointer.Capture(lstGroupRail);
        }
    }

    private async void RailGroup_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var moved = _railDragMoved;
        var item = _railDragSub;
        _railDragSub = null;
        _railDragMoved = false;
        e.Pointer.Capture(null);

        var vm = ViewModel?.ProfilesViewModel;
        if (!moved || item is null || vm is null)
        {
            _railSelectedBefore = null;
            return;
        }

        var items = vm.SubItems;
        var from = items.IndexOf(item);
        var hit = lstGroupRail.InputHitTest(e.GetPosition(lstGroupRail)) as Visual;
        var container = hit?.FindAncestorOfType<ListBoxItem>();
        var target = container is null ? -1 : lstGroupRail.IndexFromContainer(container);

        if (from >= 1 && target >= 1 && target < items.Count && target != from)
        {
            items.Move(from, target);
            await vm.SaveSubOrderAsync();
        }

        if (_railSelectedBefore is not null && !ReferenceEquals(vm.SelectedSub, _railSelectedBefore))
        {
            vm.SelectedSub = _railSelectedBefore;
        }

        _railSelectedBefore = null;
    }
}
