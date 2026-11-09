using Avalonia.Controls.DataGridDragDrop;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia;
using DialogHostAvalonia;
using v2rayN.Desktop.Common;

namespace v2rayN.Desktop.Views;

public partial class ProfilesView : ReactiveUserControl<ProfilesViewModel>
{
    private static Config _config;
    private static readonly string _tag = "ProfilesView";
    private bool _isDndRegistered = false;

    public ProfilesView()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;

        menuSelectAll.Click += menuSelectAll_Click;
        btnAutofitColumnWidth.Click += BtnAutofitColumnWidth_Click;
        txtServerFilter.KeyDown += TxtServerFilter_KeyDown;
        lstProfiles.KeyDown += LstProfiles_KeyDown;
        lstProfiles.SelectionChanged += lstProfiles_SelectionChanged;
        lstProfiles.DoubleTapped += LstProfiles_DoubleTapped;
        lstProfiles.Sorting += LstProfiles_Sorting;
        txtServerFilter.TextChanged += TxtServerFilter_TextChanged;
        AddHandler(InputElement.KeyDownEvent, ProfilesView_KeyDown, RoutingStrategies.Tunnel);

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.DisplayItems, v => v.lstProfiles.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedProfile, v => v.lstProfiles.SelectedItem).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.ServerFilter, v => v.txtServerFilter.Text).DisposeWith(disposables);

            //servers delete
            this.BindCommand(ViewModel, vm => vm.EditServerCmd, v => v.menuEditServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RemoveServerCmd, v => v.menuRemoveServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RemoveDuplicateServerCmd, v => v.menuRemoveDuplicateServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.CopyServerCmd, v => v.menuCopyServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SetDefaultServerCmd, v => v.menuSetDefaultServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ShareServerCmd, v => v.menuShareServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.GenGroupAllServerCmd, v => v.menuGenGroupAllServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.GenGroupRegionServerCmd, v => v.menuGenGroupRegionServer).DisposeWith(disposables);

            //servers move
            //this.OneWayBind(ViewModel, vm => vm.SubItems, v => v.cmbMoveToGroup.ItemsSource).DisposeWith(disposables);
            //this.Bind(ViewModel, vm => vm.SelectedMoveToGroup, v => v.cmbMoveToGroup.SelectedItem).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.MoveTopCmd, v => v.menuMoveTop).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.MoveUpCmd, v => v.menuMoveUp).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.MoveDownCmd, v => v.menuMoveDown).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.MoveBottomCmd, v => v.menuMoveBottom).DisposeWith(disposables);

            //servers ping
            this.BindCommand(ViewModel, vm => vm.MixedTestServerCmd, v => v.menuMixedTestServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.TcpingServerCmd, v => v.menuTcpingServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RealPingServerCmd, v => v.menuRealPingServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.UdpTestServerCmd, v => v.menuUdpTestServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SpeedServerCmd, v => v.menuSpeedServer).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SortServerResultCmd, v => v.menuSortServerResult).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RemoveInvalidServerResultCmd, v => v.menuRemoveInvalidServerResult).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.FastRealPingCmd, v => v.btnFastRealPing).DisposeWith(disposables);


            //servers export
            this.BindCommand(ViewModel, vm => vm.Export2ClientConfigCmd, v => v.menuExport2ClientConfig).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.Export2ClientConfigClipboardCmd, v => v.menuExport2ClientConfigClipboard).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.Export2ShareUrlCmd, v => v.menuExport2ShareUrl).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.Export2ShareUrlBase64Cmd, v => v.menuExport2ShareUrlBase64).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.Export2InnerUriCmd, v => v.menuExport2InnerUri).DisposeWith(disposables);

            if (_config.UiItem.EnableDragDropSort && !_isDndRegistered)
            {
                lstProfiles.CanUserReorderRows = true;
                lstProfiles.RowDragHandle = DataGridRowDragHandle.Row;
                lstProfiles.RowDragHandleVisible = true;
                var rowDragDropOptions = new DataGridRowDragDropOptions()
                {
                    AllowedEffects = DragDropEffects.Move,
                    DragSelectedRows = true,
                    SuppressSelectionDragFromDragHandle = true,
                };
                lstProfiles.RowDragDropOptions = rowDragDropOptions;
                lstProfiles.RowDropHandler = new ProfileRowDropHandler(ViewModel);
                _isDndRegistered = true;
            }

            ViewModel.ShowYesNoInteraction.RegisterHandler(async interaction =>
            {
                var message = interaction.Input;
                var result = await UI.ShowYesNo(message);
                interaction.SetOutput(result == ButtonResult.Yes);
            }).DisposeWith(disposables);

            ViewModel.SaveFileDialogInteraction.RegisterHandler(async interaction =>
            {
                var viewModel = ViewModel;
                if (viewModel is null)
                {
                    interaction.SetOutput(false);
                    return;
                }
                var profileItem = interaction.Input;
                var fileName = await UI.SaveFileDialog("");
                if (fileName.IsNullOrEmpty())
                {
                    interaction.SetOutput(false);
                    return;
                }
                await viewModel.Export2ClientConfigResult(fileName, profileItem);
                interaction.SetOutput(true);
            }).DisposeWith(disposables);

            ViewModel.SetClipboardDataInteraction.RegisterHandler(async interaction =>
            {
                var strData = interaction.Input;
                await AvaUtils.SetClipboardData(this, strData);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            ViewModel.ProfilesFocusInteraction.RegisterHandler(interaction =>
            {
                lstProfiles.Focus();
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            ViewModel.ShareServerInteraction.RegisterHandler(async interaction =>
            {
                var url = interaction.Input;
                if (url.IsNullOrEmpty())
                {
                    interaction.SetOutput(RxVoid.Default);
                    return;
                }
                await ShareServer(url);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            ViewModel.DispatcherRefreshServersBizInteraction.RegisterHandler(interaction =>
            {
                Dispatcher.UIThread.Post(RefreshServersBiz, DispatcherPriority.Default);
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            ViewModel.AdjustMainLvColWidthInteraction.RegisterHandler(interaction =>
            {
                //AutofitColumnWidth();
                interaction.SetOutput(RxVoid.Default);
            }).DisposeWith(disposables);

            AppEvents.AppExitRequested
              .AsObservable()
              .ObserveOn(RxSchedulers.MainThreadScheduler)
              .Subscribe(_ => StorageUI())
              .DisposeWith(disposables);
        });

        RestoreUI();

        // 首帧先给个“加载中”，等 RefreshServersBiz 回来后 UpdateEmptyState 会替换掉
        txtEmptyState.Text = ResUI.TipLoadingServers;
        txtEmptyState.IsVisible = true;

        // 窄屏自动隐藏低优先级列，宽屏再放回来；拖窗口时防抖，避免每帧重算
        lstProfiles.SizeChanged += (_, _) => ScheduleAdjustColumns();
        lstProfiles.Loaded += (_, _) =>
            Dispatcher.UIThread.Post(() => AdjustColumnsForWidth(), DispatcherPriority.Background);
    }

    private async void LstProfiles_Sorting(object? sender, DataGridColumnEventArgs e)
    {
        e.Handled = true;

        if (ViewModel != null && e.Column?.Tag?.ToString() != null)
        {
            await ViewModel.SortServer(e.Column.Tag.ToString());
        }

        e.Handled = false;
    }

    /// <summary>
    /// 行内快捷操作：编辑 / 测速 / 复制 / 删除。
    /// 这些命令的 CanExecute 依赖 ViewModel.SelectedProfile，所以先把选中项同步给 VM，
    /// 再延后一帧执行（给 ReactiveUI 的 CanExecute 一次刷新机会）。
    /// </summary>
    private void RowAction_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ProfileItemModel item } btn || ViewModel is null)
        {
            return;
        }

        if (ProfilesViewModel.IsGroupHeader(item))
        {
            return;
        }

        ViewModel.SelectedProfile = item;
        lstProfiles.SelectedItem = item;

        var tag = btn.Tag as string;
        Dispatcher.UIThread.Post(() => DoRowAction(tag), DispatcherPriority.Background);
    }

    private void DoRowAction(string? tag)
    {
        try
        {
            Logging.SaveLog($"row action: {tag}");
            switch (tag)
            {
                case "edit":
                    ViewModel?.EditServerAsync();
                    break;
                case "ping":
                    ViewModel?.ServerSpeedtest(ESpeedActionType.Realping);
                    break;
                case "copy":
                    if (ViewModel?.CopyServerCmd is { } copyCmd)
                    {
                        ((System.Windows.Input.ICommand)copyCmd).Execute(null);
                    }
                    break;
                case "delete":
                    ViewModel?.RemoveServerAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
        }
    }
    #region Event

    public async Task ShareServer(string url)
    {
        if (url.IsNullOrEmpty())
        {
            return;
        }

        var dialog = new QrcodeView(url);
        await DialogHost.Show(dialog);
    }

    public void RefreshServersBiz()
    {
        if (lstProfiles.SelectedIndex >= 0)
        {
            lstProfiles.ScrollIntoView(lstProfiles.SelectedItem, null);
        }

        UpdateEmptyState();
    }

    private bool _handlingHeaderClick;

    private void lstProfiles_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is null || _handlingHeaderClick)
        {
            return;
        }

        var selected = lstProfiles.SelectedItems?.Cast<ProfileItemModel>().ToList() ?? [];

        // 点到分组标题行：折叠 / 展开，然后把选中还原回去
        var header = selected.FirstOrDefault(ProfilesViewModel.IsGroupHeader);
        if (header is not null)
        {
            _handlingHeaderClick = true;
            try
            {
                var subid = header.Subid ?? string.Empty;
                ViewModel.ToggleGroup(subid);
                Dispatcher.UIThread.Post(() =>
                {
                    lstProfiles.SelectedItem = ViewModel.SelectedProfile;
                    _handlingHeaderClick = false;
                }, DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                _handlingHeaderClick = false;
                Logging.SaveLog(_tag, ex);
            }
            return;
        }

        ViewModel.SelectedProfiles = selected;
    }

    private void LstProfiles_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel?.SelectedProfile is { } cur && ProfilesViewModel.IsGroupHeader(cur))
        {
            return;
        }

        var source = e.Source as Border;
        if (source?.Name == "HeaderBackground")
        {
            return;
        }

        if (_config.UiItem.DoubleClick2Activate)
        {
            ViewModel?.SetDefaultServer();
        }
        else
        {
            ViewModel?.EditServerAsync();
        }
    }

    //private void LstProfiles_ColumnHeader_Click(object? sender, RoutedEventArgs e)
    //{
    //    var colHeader = sender as DataGridColumnHeader;
    //    if (colHeader == null || colHeader.TabIndex < 0 || colHeader.Column == null)
    //    {
    //        return;
    //    }

    //    var colName = ((MyDGTextColumn)colHeader.Column).ExName;
    //    ViewModel?.SortServer(colName);
    //}

    private void menuSelectAll_Click(object? sender, RoutedEventArgs e)
    {
        lstProfiles.SelectAll();
    }

    private void LstProfiles_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers is KeyModifiers.Control or KeyModifiers.Meta)
        {
            switch (e.Key)
            {
                case Key.A:
                    menuSelectAll_Click(null, null);
                    break;

                case Key.C:
                    ViewModel?.Export2ShareUrlAsync(false);
                    break;

                case Key.D:
                    ViewModel?.EditServerAsync();
                    break;

                case Key.F:
                    ViewModel?.ShareServerAsync();
                    break;

                case Key.O:
                    ViewModel?.ServerSpeedtest(ESpeedActionType.Tcping);
                    break;

                case Key.R:
                    ViewModel?.ServerSpeedtest(ESpeedActionType.Realping);
                    break;

                case Key.T:
                    ViewModel?.ServerSpeedtest(ESpeedActionType.Speedtest);
                    break;

                case Key.E:
                    ViewModel?.ServerSpeedtest(ESpeedActionType.Mixedtest);
                    break;
            }
        }
        else
        {
            switch (e.Key)
            {
                case Key.Enter:
                    //case Key.Return:
                    ViewModel?.SetDefaultServer();
                    break;

                case Key.Delete:
                case Key.Back:
                    ViewModel?.RemoveServerAsync();
                    break;

                case Key.T:
                    ViewModel?.MoveServer(EMove.Top);
                    break;

                case Key.U:
                    ViewModel?.MoveServer(EMove.Up);
                    break;

                case Key.D:
                    ViewModel?.MoveServer(EMove.Down);
                    break;

                case Key.B:
                    ViewModel?.MoveServer(EMove.Bottom);
                    break;

                case Key.Escape:
                    ViewModel?.ServerSpeedtestStop();
                    break;
            }
        }
    }

    #endregion Event

    #region UI

    private async void CopyIpInfo_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is MenuItem { CommandParameter: string text } && !text.IsNullOrEmpty())
            {
                await AvaUtils.SetClipboardData(this, text);
            }
        }
        catch
        {
            // Ignore
        }
    }

    #endregion UI

    private sealed class ProfileRowDropHandler(ProfilesViewModel viewModel) : IDataGridRowDropHandler
    {
        private readonly DataGridRowReorderHandler _reorderHandler = new();

        public bool Validate(DataGridRowDropEventArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);
            return _reorderHandler.Validate(args);
        }

        public bool Execute(DataGridRowDropEventArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            if (!_reorderHandler.Execute(args))
            {
                return false;
            }

            if (viewModel?.ProfileItems != null)
            {
                for (var index = 0; index < viewModel.ProfileItems.Count; index++)
                {
                    var item = viewModel.ProfileItems[index];
                    var sort = (index + 1) * 10;
                    item.Sort = sort;
                    ProfileExManager.Instance.SetSort(item.IndexId, sort);
                }
            }

            return true;
        }
    }
}
