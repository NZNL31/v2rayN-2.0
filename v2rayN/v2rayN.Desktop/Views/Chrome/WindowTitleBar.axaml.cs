using Avalonia.Controls;
using Avalonia.Input;

namespace v2rayN.Desktop.Views.Chrome;

public partial class WindowTitleBar : UserControl
{
    public WindowTitleBar()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window is not null)
            {
                txtTitle.Text = window.Title ?? string.Empty;
            }
        };
    }

    private void Root_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.BeginMoveDrag(e);
        }
    }

    private void BtnClose_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        (TopLevel.GetTopLevel(this) as Window)?.Close();
    }
}
