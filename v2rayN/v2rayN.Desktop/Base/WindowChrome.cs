using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace v2rayN.Desktop.Base;

/// <summary>
/// 给子窗口套一层自绘标题栏，避免出现系统原生（跟随主题色的棕色）标题栏。
/// 主窗口有自己的顶栏，不走这里。
/// </summary>
public static class WindowChrome
{
    private const double BarHeight = 34;

    public static void Apply(Window window)
    {
        if (window.GetType().Name == "MainWindow")
        {
            return;
        }

        window.Loaded += (_, _) => Wrap(window);
    }

    private static void Wrap(Window window)
    {
        if (window.Content is not Control content)
        {
            return;
        }

        if (content is Border { Name: "chromeRoot" })
        {
            return;
        }

        window.WindowDecorations = WindowDecorations.None;

        // 先把内容从 ContentPresenter 上摘下来，等这一帧布局跑完再重新挂到自绘外壳上，
        // 否则会报 "already has a visual parent" 直接崩溃
        window.Content = null;

        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                if (window.Content is not null)
                {
                    return;
                }

                var bar = BuildBar(window);

                var grid = new Grid
                {
                    Name = "chromeGrid",
                    RowDefinitions = new RowDefinitions($"{BarHeight},*"),
                };
                Grid.SetRow(bar, 0);
                Grid.SetRow(content, 1);
                grid.Children.Add(bar);
                grid.Children.Add(content);

                window.Content = new Border
                {
                    Name = "chromeRoot",
                    BorderBrush = Res("AppCardBorder", "#E0E0E0"),
                    BorderThickness = new Thickness(1),
                    Child = grid,
                };
            }
            catch
            {
                try
                {
                    window.Content ??= content;
                }
                catch
                {
                    // 放弃自绘外壳，至少别让窗口空着
                }
            }
        }, DispatcherPriority.Background);
    }

    private static Control BuildBar(Window window)
    {
        var title = new TextBlock
        {
            Text = window.Title ?? string.Empty,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            FontSize = 12.5,
            Foreground = Res("AppTextPrimary", "#1A1A1A"),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        var close = new Button
        {
            Content = new TextBlock
            {
                Text = "\u2715",
                FontSize = 12,
                Foreground = Res("AppTextPrimary", "#1A1A1A"),
            },
            Width = 42,
            Height = BarHeight,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(close, "关闭");
        close.Click += (_, _) => window.Close();

        var dock = new DockPanel();
        DockPanel.SetDock(close, Dock.Right);
        dock.Children.Add(close);
        dock.Children.Add(title);

        var bar = new Border
        {
            Height = BarHeight,
            Background = Res("AppSidebarBg", "#F0F0F0"),
            BorderBrush = Res("AppCardBorder", "#E0E0E0"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = dock,
        };

        bar.PointerPressed += (_, e) =>
        {
            if (!e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
            {
                return;
            }

            if (e.Source is Visual v && (v is Button || v.FindAncestorOfType<Button>() is not null))
            {
                return;
            }

            try
            {
                window.BeginMoveDrag(e);
            }
            catch
            {
                // 不支持拖动时忽略
            }
        };

        return bar;
    }

    private static IBrush Res(string key, string fallback)
    {
        var app = Application.Current;
        if (app is not null && app.TryGetResource(key, app.ActualThemeVariant, out var value) && value is IBrush brush)
        {
            return brush;
        }

        return new SolidColorBrush(Color.Parse(fallback));
    }

    private static T? FindAncestorOfType<T>(this Visual? visual) where T : Visual
    {
        for (var cur = visual; cur is not null; cur = cur.GetVisualParent())
        {
            if (cur is T typed)
            {
                return typed;
            }
        }

        return null;
    }
}
