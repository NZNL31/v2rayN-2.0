using Avalonia;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DialogHostAvalonia;
using v2rayN.Desktop.Common;

namespace v2rayN.Desktop.Views;

public partial class ProfilesView
{    /// <summary>列表为空时给个提示，避免一片空白</summary>
    private void UpdateEmptyState()
    {
        try
        {
            if (txtEmptyState is null)
            {
                return;
            }

            var count = lstProfiles?.ItemsSource?.Cast<object>().Count() ?? 0;
            txtEmptyState.IsVisible = count == 0;
            txtEmptyState.Text = txtServerFilter.Text.IsNullOrEmpty()
                ? ResUI.TipEmptyServers
                : ResUI.TipEmptySearch;
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
        }
    }

    private void BtnClearFilter_Click(object? sender, RoutedEventArgs e)
    {
        txtServerFilter.Text = string.Empty;
        ViewModel?.RefreshServers();
        lstProfiles.Focus();
    }

    /// <summary>输入即过滤（防抖 250ms），不用每次按回车</summary>
    private void TxtServerFilter_TextChanged(object? sender, TextChangedEventArgs e)
    {
        btnClearFilter.IsVisible = !txtServerFilter.Text.IsNullOrEmpty();

        _filterDebounce ??= new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) =>
        {
            _filterDebounce?.Stop();
            ViewModel?.RefreshServers();
        });
        _filterDebounce.Stop();
        _filterDebounce.Start();
    }

    /// <summary>Ctrl+F 聚焦过滤框；过滤框里按 Esc 清空</summary>
    private void ProfilesView_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers is KeyModifiers.Control or KeyModifiers.Meta && e.Key == Key.F)
        {
            txtServerFilter.Focus();
            txtServerFilter.SelectAll();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && txtServerFilter.IsFocused && !txtServerFilter.Text.IsNullOrEmpty())
        {
            txtServerFilter.Text = string.Empty;
            e.Handled = true;
        }
    }

    private void TxtServerFilter_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Return)
        {
            ViewModel?.RefreshServers();
        }
    }
}
