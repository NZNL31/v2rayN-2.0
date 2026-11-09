namespace v2rayN.Desktop.Base;

public class WindowBase<TViewModel> : ReactiveWindow<TViewModel> where TViewModel : class
{
    public WindowBase()
    {
        Loaded += OnLoaded;
        Loaded += (s, e) =>
        {
            if (Owner != null && !ShowInTaskbar)
            {
                CanMinimize = false;
            }
        };
    }

    private void ReactiveWindowBase_Closed(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    protected virtual void OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            var sizeItem = ConfigHandler.GetWindowSizeItem(AppManager.Instance.Config, GetType().Name);
            if (sizeItem is null)
            {
                return;
            }

            var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
            var scaling = screen.Scaling > 0 ? screen.Scaling : 1.0;
            var workingArea = screen.WorkingArea;

            var width = Math.Min(sizeItem.Width, workingArea.Width / scaling);
            var height = Math.Min(sizeItem.Height, workingArea.Height / scaling);

            Width = width;
            Height = height;

            var frameDiff = (FrameSize ?? ClientSize) - ClientSize;
            var totalWidth = (width + frameDiff.Width) * scaling;
            var totalHeight = (height + frameDiff.Height) * scaling;

            var x = workingArea.X + ((workingArea.Width - totalWidth) / 2);
            var y = workingArea.Y + ((workingArea.Height - totalHeight) / 2);
            Position = new PixelPoint((int)x, (int)y);

            // 有保存过位置且那块屏幕还在（窗口仍可见）就恢复，否则维持居中
            if (sizeItem.X != 0 || sizeItem.Y != 0)
            {
                var saved = new PixelPoint(sizeItem.X, sizeItem.Y);
                var targetScreen = Screens.ScreenFromPoint(saved) ?? screen;
                var wa = targetScreen.WorkingArea;
                var visible = saved.X >= wa.X - 40 && saved.Y >= wa.Y - 40
                              && saved.X <= wa.Right - 120 && saved.Y <= wa.Bottom - 80;
                if (visible)
                {
                    Position = new PixelPoint(
                        Math.Clamp(saved.X, wa.X - 40, wa.Right - 120),
                        Math.Clamp(saved.Y, wa.Y - 40, wa.Bottom - 80));
                }
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog("WindowBase.OnLoaded", ex);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        try
        {
            // 最大化/最小化时宽高不是用户意图，不覆盖已保存的尺寸与位置
            if (WindowState == WindowState.Normal)
            {
                ConfigHandler.SaveWindowSizeItem(AppManager.Instance.Config, GetType().Name, Width, Height, Position.X, Position.Y);
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog("WindowBase.OnClosed", ex);
        }
    }
}
