using Avalonia.Input.Platform;

namespace v2rayN.Desktop.Common;

internal class AvaUtils
{
    public static async Task<string?> GetClipboardData(Window owner)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(owner)?.Clipboard;
            if (clipboard == null)
            {
                return null;
            }

            return await clipboard.TryGetTextAsync();
        }
        catch
        {
            return null;
        }
    }

    public static async Task SetClipboardData(Visual? visual, string strData)
    {
        try
        {
            var clipboard = TopLevel.GetTopLevel(visual)?.Clipboard;
            if (clipboard == null)
            {
                return;
            }

            await clipboard.SetTextAsync(strData);
        }
        catch
        {
        }
    }

    /// <summary>
    /// 按模式缓存图标：反复新建 WindowIcon 会不断产生新的 HICON，
    /// 连续切换几次后系统托盘就可能拿不到有效图标（图标消失）。
    /// </summary>
    private static readonly Dictionary<ESysProxyType, WindowIcon> _appIconCache = [];

    /// <summary>Bitmap 必须保活：WindowIcon 不会复制像素，提前释放会导致托盘图标变空</summary>
    private static readonly List<Bitmap> _appIconBitmaps = [];

    public static WindowIcon GetAppIcon(ESysProxyType sysProxyType)
    {
        if (_appIconCache.TryGetValue(sysProxyType, out var cached))
        {
            return cached;
        }

        var index = (int)sysProxyType + 1;

        // 托盘图标用单帧 32px PNG：多帧 ICO 每次切换都要新建 HICON，连续切换几次后系统托盘会拿不到有效图标
        WindowIcon icon;
        var pngFile = Utils.GetPath($"NotifyIcon{index}.png");
        if (File.Exists(pngFile))
        {
            icon = new(pngFile);
        }
        else
        {
            var uri = new Uri(Path.Combine(Global.AvaAssets, $"NotifyIcon{index}.png"));
            var bitmap = new Bitmap(AssetLoader.Open(uri));
            _appIconBitmaps.Add(bitmap);
            icon = new(bitmap);
        }

        _appIconCache[sysProxyType] = icon;
        return icon;
    }
}
