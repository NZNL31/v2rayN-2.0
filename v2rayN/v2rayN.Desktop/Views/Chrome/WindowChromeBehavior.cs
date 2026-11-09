using Avalonia;
using Avalonia.Controls;

namespace v2rayN.Desktop.Views.Chrome;

/// <summary>
/// 给对话框统一套一层自绘标题栏（去掉系统原生标题栏）。
/// 由 Assets/CustomStyles.axaml 里的 <c>Style Selector="Window"</c> 统一开启，
/// 因此各个对话框的 XAML 不需要任何改动（也就不会与上游产生冲突）。
/// </summary>
public static class WindowChromeBehavior
{
    public static readonly AttachedProperty<bool> TitleBarProperty =
        AvaloniaProperty.RegisterAttached<Window, bool>("TitleBar", typeof(WindowChromeBehavior));

    static WindowChromeBehavior()
    {
        TitleBarProperty.Changed.AddClassHandler<Window>(OnTitleBarChanged);
    }

    public static bool GetTitleBar(Window window) => window.GetValue(TitleBarProperty);

    public static void SetTitleBar(Window window, bool value) => window.SetValue(TitleBarProperty, value);

    private static void OnTitleBarChanged(Window window, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true)
        {
            return;
        }

        // 主窗口自己有整套自绘顶栏（logo / 图标按钮 / 窗口按钮），不要重复套
        if (window is MainWindow)
        {
            return;
        }

        window.WindowDecorations = WindowDecorations.None;

        if (window.Content is Control content)
        {
            Wrap(window, content);
        }
        else
        {
            // 内容稍后才设置的情况，等窗口打开时再包一层
            window.Opened += (_, _) =>
            {
                if (window.Content is Control late && !IsWrapped(late))
                {
                    Wrap(window, late);
                }
            };
        }
    }

    private static bool IsWrapped(Control content) =>
        content is DockPanel { Tag: ChromeRootTag };

    private const string ChromeRootTag = "chrome-root";

    private static void Wrap(Window window, Control content)
    {
        if (IsWrapped(content))
        {
            return;
        }

        var root = new DockPanel { Tag = ChromeRootTag };
        var bar = new WindowTitleBar();
        DockPanel.SetDock(bar, Dock.Top);

        window.Content = null;
        root.Children.Add(bar);
        root.Children.Add(content);
        window.Content = root;
    }
}
