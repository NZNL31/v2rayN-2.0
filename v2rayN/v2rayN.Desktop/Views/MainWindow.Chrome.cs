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
{    private void Header_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        // 点在菜单/按钮上时不能启动窗口拖动，否则指针被窗口捕获，
        // 下拉菜单里的条目会点不动
        for (var cur = e.Source as Visual; cur is not null && !ReferenceEquals(cur, appHeader); cur = cur.GetVisualParent())
        {
            if (cur is Menu or MenuItem or Button or ToggleButton)
            {
                return;
            }
        }

        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        BeginMoveDrag(e);
    }

    private void BtnWinMin_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void BtnWinMax_Click(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void BtnWinClose_Click(object? sender, RoutedEventArgs e) => Close();

    private void ResizeTop_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.North, e);

    private void ResizeBottom_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.South, e);

    private void ResizeLeft_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.West, e);

    private void ResizeRight_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.East, e);

    private void ResizeTopLeft_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.NorthWest, e);

    private void ResizeTopRight_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.NorthEast, e);

    private void ResizeBottomLeft_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.SouthWest, e);

    private void ResizeBottomRight_PointerPressed(object? sender, PointerPressedEventArgs e) => BeginResizeDrag(WindowEdge.SouthEast, e);

    /// <summary>顶栏齿轮按钮：弹出原「设置」菜单</summary>
    private void BtnToggleLog_Click(object? sender, RoutedEventArgs e) => ToggleSidePanel();

    private void BtnToggleSide_Click(object? sender, RoutedEventArgs e) => ToggleSidePanel();

    private void ToggleSidePanel()
    {
        _logCollapsed = !_logCollapsed;
        ApplySidePanelState();

        _config.UiItem.SidePanelCollapsed = _logCollapsed;
        _ = ConfigHandler.SaveConfig(_config);
    }

    /// <summary>按 _logCollapsed 应用右侧信息面板的显示/隐藏</summary>
    private void ApplySidePanelState()
    {
        if (gridMain.RowDefinitions.Count > 1)
        {
            gridMain.RowDefinitions[1].Height = _logCollapsed ? new GridLength(0) : new GridLength(10);
            gridMain.RowDefinitions[2].Height = _logCollapsed ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
            gridSplitter.IsVisible = !_logCollapsed;
        }
        else if (gridMain.ColumnDefinitions.Count > 1)
        {
            gridMain.ColumnDefinitions[1].Width = _logCollapsed ? new GridLength(0) : new GridLength(10);
            gridMain.ColumnDefinitions[2].Width = _logCollapsed ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
            gridSplitter.IsVisible = !_logCollapsed;
        }

        txtToggleGlyph.RenderTransform = new RotateTransform(_logCollapsed ? 180 : 0);
    }
}
