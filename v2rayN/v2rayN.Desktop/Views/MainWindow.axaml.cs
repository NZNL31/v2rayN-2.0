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

public partial class MainWindow : WindowBase<MainWindowViewModel>
{
    private static Config _config;
    private readonly WindowNotificationManager? _manager;
    private CheckUpdateView? _checkUpdateView;
    private BackupAndRestoreView? _backupAndRestoreView;
    private bool _blCloseByUser = false;

    private MsgView? _msgView;
    private ProfilesView? _profilesView;
    private ClashConnectionsView? _clashConnectionsView;
    private ClashProxiesView? _clashProxiesView;

    public MainWindow()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;
        txtAppVersion.Text = Utils.GetVersionInfo();
        _manager = new WindowNotificationManager(TopLevel.GetTopLevel(this)) { MaxItems = 3, Position = NotificationPosition.TopRight };

        KeyDown += MainWindow_KeyDown;
        menuSettingsSetUWP.Click += MenuSettingsSetUWP_Click;
        menuCheckUpdate.Click += MenuCheckUpdate_Click;
        btnNewUpdate.Click += MenuCheckUpdate_Click;
        menuBackupAndRestore.Click += MenuBackupAndRestore_Click;
        menuClose.Click += MenuClose_Click;

        conTheme.Content ??= new ThemeSettingView();

        lstGroupRail.AddHandler(PointerPressedEvent, RailGroup_PointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
        lstGroupRail.AddHandler(PointerMovedEvent, RailGroup_PointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
        lstGroupRail.AddHandler(PointerReleasedEvent, RailGroup_PointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);

        // 导轨一级入口统一用 MenuFlyout 弹出（见 BtnRailConfig_Click）

        this.WhenActivated(disposables =>
        {
            //servers
            this.BindCommand(ViewModel, vm => vm.AddVmessServerCmd, v => v.menuAddVmessServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddVlessServerCmd, v => v.menuAddVlessServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddShadowsocksServerCmd, v => v.menuAddShadowsocksServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddSocksServerCmd, v => v.menuAddSocksServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddHttpServerCmd, v => v.menuAddHttpServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddTrojanServerCmd, v => v.menuAddTrojanServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddHysteria2ServerCmd, v => v.menuAddHysteria2Server).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddTuicServerCmd, v => v.menuAddTuicServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddWireguardServerCmd, v => v.menuAddWireguardServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddAnytlsServerCmd, v => v.menuAddAnytlsServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddNaiveServerCmd, v => v.menuAddNaiveServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddMasqueServerCmd, v => v.menuAddMasqueServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddCustomServerCmd, v => v.menuAddCustomServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddCustomOutboundServerCmd, v => v.menuAddCustomOutboundServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddPolicyGroupServerCmd, v => v.menuAddPolicyGroupServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddProxyChainServerCmd, v => v.menuAddProxyChainServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddServerViaClipboardCmd, v => v.menuAddServerViaClipboard).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddServerViaScanCmd, v => v.menuAddServerViaScan).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddServerViaImageCmd, v => v.menuAddServerViaImage).DisposeWith(disposables);

            //sub
            this.BindCommand(ViewModel, vm => vm.SubSettingCmd, v => v.menuSubSetting).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SubUpdateCmd, v => v.menuSubUpdate).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SubUpdateViaProxyCmd, v => v.menuSubUpdateViaProxy).DisposeWith(disposables);

            //setting
            this.BindCommand(ViewModel, vm => vm.OptionSettingCmd, v => v.menuOptionSetting).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RoutingSettingCmd, v => v.menuRoutingSetting).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.DNSSettingCmd, v => v.menuDNSSetting).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.FullConfigTemplateCmd, v => v.menuFullConfigTemplate).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.GlobalHotkeySettingCmd, v => v.menuGlobalHotkeySetting).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RebootAsAdminCmd, v => v.menuRebootAsAdmin).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ClearServerStatisticsCmd, v => v.menuClearServerStatistics).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.OpenTheFileLocationCmd, v => v.menuOpenTheFileLocation).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RegionalPresetDefaultCmd, v => v.menuRegionalPresetsDefault).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RegionalPresetRussiaCmd, v => v.menuRegionalPresetsRussia).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RegionalPresetIranCmd, v => v.menuRegionalPresetsIran).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.ReloadCmd, v => v.menuReload).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlReloadEnabled, v => v.menuReload.IsEnabled).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlNewUpdate, v => v.btnNewUpdate.IsVisible).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.StatusBarViewModel, v => v.contentStatusBarView.Content).DisposeWith(disposables);

            var layoutBindings = new SingleReplaceableDisposable().DisposeWith(disposables);
            this.WhenAnyValue(v => v.ViewModel.MainGirdOrientation)
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(orientation =>
                {
                    UpdateLayout(orientation, layoutBindings);
                })
                .DisposeWith(disposables);

            ViewModel.ReadTextFromClipboardInteraction.RegisterHandler(async interaction =>
            {
                var result = await AvaUtils.GetClipboardData(this);
                interaction.SetOutput(result);
            }).DisposeWith(disposables);

            ViewModel.ScanScreenInteraction.RegisterHandler(async interaction =>
            {
                ShowHideWindow(false);
                await Task.Delay(200);
                var result = QRCodeAvaloniaUtils.CaptureScreen();
                ShowHideWindow(true);
                interaction.SetOutput(result);
            }).DisposeWith(disposables);

            ViewModel.BrowseImageFileInteraction.RegisterHandler(async interaction =>
            {
                var result = await UI.OpenFileDialog(null);
                interaction.SetOutput(result);
            }).DisposeWith(disposables);

            ViewModel.ShowHideWindowInteraction.RegisterHandler(interaction =>
            {
                ShowHideWindow(interaction.Input);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            AppEvents.SendSnackMsgRequested
              .AsObservable()
              .ObserveOn(RxSchedulers.MainThreadScheduler)
              .Subscribe(async content => await DelegateSnackMsg(content))
              .DisposeWith(disposables);

            AppEvents.AppExitRequested
              .AsObservable()
              .ObserveOn(RxSchedulers.MainThreadScheduler)
              .Subscribe(_ => StorageUI())
              .DisposeWith(disposables);

            AppEvents.ShutdownRequested
              .AsObservable()
              .ObserveOn(RxSchedulers.MainThreadScheduler)
              .Subscribe(Shutdown)
              .DisposeWith(disposables);
        });

        if (Utils.IsWindows())
        {
            Title = $"{Utils.GetVersion()} - {(Utils.IsAdministrator() ? ResUI.RunAsAdmin : ResUI.NotRunAsAdmin)}";

            if (!Design.IsDesignMode)
            {
                ThreadPool.RegisterWaitForSingleObject(Program.ProgramStarted, OnProgramStarted, null, -1, false);
                HotkeyManager.Instance.Init(_config, OnHotkeyHandler);
            }
        }
        else
        {
            Title = $"{Utils.GetVersion()}";
            menuAddServerViaScan.IsVisible = false;
        }

        if (_config.UiItem.AutoHideStartup && Utils.IsWindows())
        {
            WindowState = WindowState.Minimized;
        }

        AddHelpMenuItem();
        Global.BootMark("MainWindow ctor done");
    }

    #region Event

    private void OnProgramStarted(object state, bool timeout)
    {
        Dispatcher.UIThread.Post(() =>
                ShowHideWindow(true),
            DispatcherPriority.Default);
    }

    private async Task DelegateSnackMsg(string content)
    {
        _manager?.Show(new Avalonia.Controls.Notifications.Notification(null, content, NotificationType.Information));
        await Task.CompletedTask;
    }

    private void OnHotkeyHandler(EGlobalHotkey e)
    {
        switch (e)
        {
            case EGlobalHotkey.ShowForm:
                Dispatcher.UIThread.Post(() => ShowHideWindow(null));
                break;

            case EGlobalHotkey.SystemProxyClear:
            case EGlobalHotkey.SystemProxySet:
            case EGlobalHotkey.SystemProxyUnchanged:
            case EGlobalHotkey.SystemProxyPac:
                AppEvents.SysProxyChangeRequested.Publish((ESysProxyType)((int)e - 1));
                break;
        }
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (_blCloseByUser)
        {
            return;
        }

        Logging.SaveLog("OnClosing -> " + e.CloseReason.ToString());

        switch (e.CloseReason)
        {
            case WindowCloseReason.OwnerWindowClosing or WindowCloseReason.WindowClosing:
                e.Cancel = true;
                ShowHideWindow(false);
                break;

            case WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown:
                await AppManager.Instance.AppExitAsync(false);
                break;
        }

        base.OnClosing(e);
    }

    private async void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers is KeyModifiers.Control or KeyModifiers.Meta)
        {
            switch (e.Key)
            {
                case Key.V:
                    await AddServerViaClipboardAsync();
                    break;

                case Key.S:
                    await ScanScreenTaskAsync();
                    break;
            }
        }
        else
        {
            if (e.Key == Key.F5)
            {
                ViewModel?.Reload();
            }
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
    private void Header_PointerPressed(object? sender, PointerPressedEventArgs e)
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
    private void BtnHeaderSetting_Click(object? sender, RoutedEventArgs e)
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

    #endregion Event

    #region UI

    public void ShowHideWindow(bool? blShow)
    {
        var bl = blShow ??
                    (Utils.IsLinux() || Utils.IsMacOS()
                    ? (!AppManager.Instance.ShowInTaskbar ^ (WindowState == WindowState.Minimized))
                    : !AppManager.Instance.ShowInTaskbar);
        if (bl)
        {
            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
            Focus();
        }
        else
        {
            if (Utils.IsLinux() && _config.UiItem.Hide2TrayWhenClose == false)
            {
                WindowState = WindowState.Minimized;
                return;
            }

            foreach (var ownedWindow in OwnedWindows)
            {
                ownedWindow.Close();
            }
            Hide();
        }

        AppManager.Instance.ShowInTaskbar = bl;
    }

    protected override void OnLoaded(object? sender, RoutedEventArgs e)
    {
        base.OnLoaded(sender, e);
        Global.BootMark("MainWindow OnLoaded");
        if (_config.UiItem.AutoHideStartup)
        {
            ShowHideWindow(false);
        }
    }

    private void RestoreUI()
    {
        if (_config.UiItem.MainGirdHeight1 > 0 && _config.UiItem.MainGirdHeight2 > 0)
        {
            if (_config.UiItem.MainGirdOrientation == EGirdOrientation.Horizontal)
            {
                gridMain.ColumnDefinitions[0].Width = new GridLength(_config.UiItem.MainGirdHeight1, GridUnitType.Star);
                gridMain.ColumnDefinitions[2].Width = new GridLength(_config.UiItem.MainGirdHeight2, GridUnitType.Star);
            }
            else if (_config.UiItem.MainGirdOrientation == EGirdOrientation.Vertical)
            {
                gridMain.RowDefinitions[0].Height = new GridLength(_config.UiItem.MainGirdHeight1, GridUnitType.Star);
                gridMain.RowDefinitions[2].Height = new GridLength(_config.UiItem.MainGirdHeight2, GridUnitType.Star);
            }
        }

        // 右侧信息面板默认折叠，只有用户主动展开时才显示
        _logCollapsed = _config.UiItem.SidePanelCollapsed;
        ApplySidePanelState();
    }

    private void StorageUI()
    {
        if (WindowState == WindowState.Normal)
        {
            ConfigHandler.SaveWindowSizeItem(_config, GetType().Name, Width, Height, Position.X, Position.Y);
        }

        // 折叠状态下宽度/高度为 0，别把原来的分栏比例覆盖掉
        if (_logCollapsed)
        {
            return;
        }

        if (_config.UiItem.MainGirdOrientation == EGirdOrientation.Horizontal)
        {
            ConfigHandler.SaveMainGirdHeight(_config, gridMain.ColumnDefinitions[0].ActualWidth, gridMain.ColumnDefinitions[2].ActualWidth);
        }
        else if (_config.UiItem.MainGirdOrientation == EGirdOrientation.Vertical)
        {
            ConfigHandler.SaveMainGirdHeight(_config, gridMain.RowDefinitions[0].ActualHeight, gridMain.RowDefinitions[2].ActualHeight);
        }
    }

    private void UpdateLayout(EGirdOrientation orientation, SingleReplaceableDisposable disposable)
    {
        var currentLayoutDisposables = new MultipleDisposable();
        disposable.Create(currentLayoutDisposables);
        gridMain.IsVisible = true;
        _msgView ??= new MsgView
        {
            DataContext = ViewModel?.MsgViewModel,
        };
        _profilesView ??= new ProfilesView
        {
            DataContext = ViewModel?.ProfilesViewModel,
        };
        _clashProxiesView ??= new ClashProxiesView
        {
            DataContext = ViewModel?.ClashProxiesViewModel,
        };
        _clashConnectionsView ??= new ClashConnectionsView
        {
            DataContext = ViewModel?.ClashConnectionsViewModel,
        };

        switch (orientation)
        {
            case EGirdOrientation.Horizontal:
            default:
                gridMain.ColumnDefinitions = new ColumnDefinitions("2.2*,10,1*");
                gridMain.RowDefinitions = new RowDefinitions("1*");
                Grid.SetColumn(tabProfiles, 0);
                Grid.SetColumn(gridSplitter, 1);
                Grid.SetColumn(tabDockPanelHost, 2);
                Grid.SetRow(tabProfiles, 0);
                Grid.SetRow(gridSplitter, 0);
                Grid.SetRow(tabDockPanelHost, 0);
                tabStripH.IsVisible = true;
                tabStripV.IsVisible = false;
                gridSplitter.IsVisible = true;
                gridSplitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                tabContent.Content = null;
                tabProfiles.Content = _profilesView;
                this.WhenAnyValue(x => x.ViewModel!.ShowClashUI)
                    .ObserveOn(RxSchedulers.MainThreadScheduler)
                    .Subscribe(show =>
                    {
                        if (show)
                        {
                            tabStripH.ItemsSource = new List<string>
                            {
                                ResUI.MsgInformationTitle,
                                ResUI.TbProxies,
                                ResUI.TbConnections,
                            };
                            tabStripH.IsVisible = true;
                        }
                        else
                        {
                            tabStripH.IsVisible = false;
                            tabStripH.ItemsSource = new List<string>();
                        }
                    }).DisposeWith(currentLayoutDisposables);
                tabStripH.GetObservable(SelectingItemsControl.SelectedIndexProperty)
                    .Subscribe(index =>
                    {
                        tabContent.Content = index switch
                        {
                            1 => _clashProxiesView,
                            2 => _clashConnectionsView,
                            _ => _msgView,
                        };
                    }).DisposeWith(currentLayoutDisposables);
                break;

            case EGirdOrientation.Vertical:
                gridMain.RowDefinitions = new RowDefinitions("1*,10,1*");
                gridMain.ColumnDefinitions = new ColumnDefinitions("1*");
                Grid.SetRow(tabProfiles, 0);
                Grid.SetRow(gridSplitter, 1);
                Grid.SetRow(tabDockPanelHost, 2);
                Grid.SetColumn(tabProfiles, 0);
                Grid.SetColumn(gridSplitter, 0);
                Grid.SetColumn(tabDockPanelHost, 0);
                tabStripV.IsVisible = true;
                tabStripH.IsVisible = false;
                gridSplitter.IsVisible = true;
                gridSplitter.VerticalAlignment = VerticalAlignment.Stretch;
                tabContent.Content = null;
                tabProfiles.Content = _profilesView;
                this.WhenAnyValue(x => x.ViewModel!.ShowClashUI)
                    .ObserveOn(RxSchedulers.MainThreadScheduler)
                    .Subscribe(show =>
                    {
                        if (show)
                        {
                            tabStripV.ItemsSource = new List<string>
                            {
                                ResUI.MsgInformationTitle,
                                ResUI.TbProxies,
                                ResUI.TbConnections,
                            };
                            tabStripV.IsVisible = true;
                        }
                        else
                        {
                            tabStripV.IsVisible = false;
                            tabStripV.ItemsSource = new List<string>();
                        }
                    }).DisposeWith(currentLayoutDisposables);
                tabStripV.GetObservable(SelectingItemsControl.SelectedIndexProperty)
                    .Subscribe(index =>
                    {
                        tabContent.Content = index switch
                        {
                            1 => _clashProxiesView,
                            2 => _clashConnectionsView,
                            _ => _msgView,
                        };
                    }).DisposeWith(currentLayoutDisposables);
                break;

            case EGirdOrientation.Tab:
                gridMain.RowDefinitions = new RowDefinitions("1*");
                gridMain.ColumnDefinitions = new ColumnDefinitions("1*");
                Grid.SetColumn(tabProfiles, 0);
                Grid.SetColumn(gridSplitter, 0);
                Grid.SetColumn(tabDockPanelHost, 0);
                Grid.SetRow(tabProfiles, 0);
                Grid.SetRow(gridSplitter, 0);
                Grid.SetRow(tabDockPanelHost, 0);
                tabStripV.IsVisible = true;
                tabStripH.IsVisible = false;
                gridSplitter.IsVisible = false;
                tabContent.Content = null;
                tabProfiles.Content = null;
                this.WhenAnyValue(x => x.ViewModel!.ShowClashUI)
                    .ObserveOn(RxSchedulers.MainThreadScheduler)
                    .Subscribe(show =>
                    {
                        if (show)
                        {
                            tabStripV.ItemsSource = new List<string>
                            {
                                ResUI.menuServers,
                                ResUI.MsgInformationTitle,
                                ResUI.TbProxies,
                                ResUI.TbConnections,
                            };
                            tabStripV.IsVisible = true;
                        }
                        else
                        {
                            tabStripV.IsVisible = false;
                            tabStripV.ItemsSource = new List<string>();
                        }
                    }).DisposeWith(currentLayoutDisposables);
                tabStripV.GetObservable(SelectingItemsControl.SelectedIndexProperty)
                    .Subscribe(index =>
                    {
                        tabContent.Content = index switch
                        {
                            1 => _msgView,
                            2 => _clashProxiesView,
                            3 => _clashConnectionsView,
                            _ => _profilesView,
                        };
                    }).DisposeWith(currentLayoutDisposables);
                break;

        }

        RestoreUI();
    }

    private void AddHelpMenuItem()
    {
        var coreInfo = CoreInfoManager.Instance.GetCoreInfo();
        foreach (var it in coreInfo
            .Where(t => t.CoreType is not ECoreType.v2fly
                        and not ECoreType.hysteria))
        {
            var item = new MenuItem()
            {
                Tag = it.Url?.Replace(@"/releases", ""),
                Header = string.Format(ResUI.menuWebsiteItem, it.CoreType.ToString().Replace("_", " ")).UpperFirstChar()
            };
            item.Click += MenuItem_Click;
            menuHelp.Items.Add(item);
        }
    }

    private void MenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item)
        {
            ProcUtils.ProcessStart(item.Tag?.ToString());
        }
    }

    #endregion UI
}
