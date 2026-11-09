namespace ServiceLib.Events;

public static class AppEvents
{
    public static readonly EventChannel<RxVoid> AddServerViaClipboardRequested = new();
    public static readonly EventChannel<bool> HasUpdateNotified = new();

    public static readonly EventChannel<ServerSpeedItem> DispatcherStatisticsRequested = new();

    public static readonly EventChannel<string> SendSnackMsgRequested = new();
    public static readonly EventChannel<string> SendMsgViewRequested = new();

    public static readonly EventChannel<RxVoid> AppExitRequested = new();
    public static readonly EventChannel<bool> ShutdownRequested = new();

    /// <summary>更新当前订阅：先直连，失败才走代理（工具栏一键按钮）</summary>
    public static readonly EventChannel<RxVoid> SubGroupUpdateBothRequested = new();

    public static readonly EventChannel<ESysProxyType> SysProxyChangeRequested = new();
}
