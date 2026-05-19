using Avalonia.Controls;
using Material.Icons;
using Material.Icons.Avalonia;
using ODProxl.ClientCommonModels;
using ODProxl.ClientServices;
using ODProxl.Utils.Events;
using System.Collections.ObjectModel;

namespace ODProxl.ViewModels.Pages
{
    public class MainWinViewModel : BindableBase
    {
        #region 字段

        private readonly IRegionManager? _regionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly IAuthService _authService;
        private IRegionNavigationJournal? _journal;

        private bool _isMenuExpanded;
        private LeftMenuItem? _selectedMenuItem;
        private LeftMenuItem? _selectedFlyoutItem;

        // 保留原始的全量菜單，過濾時從這裡重新算
        private ObservableCollection<LeftMenuItem>? _allMenuItems;
        private ObservableCollection<LeftMenuItem>? _allFlyoutItems;

        #endregion

        #region 屬性

        public ObservableCollection<LeftMenuItem>? MenuItems { get; private set; }
        public ObservableCollection<MenuItem> FlyoutMenuItems { get; } = new();
        public NotificationService Notifications { get; } = new();

        /// <summary>
        /// 認證狀態（給 XAML 綁定，控制菜單和標題欄的顯示）
        /// </summary>
        public bool IsAuthenticated => _authService.IsAuthenticated;

        /// <summary>
        /// 當前登錄用戶名（給標題欄綁定）
        /// </summary>
        public string Username => _authService.CurrentUser?.Username ?? "";

        public bool IsMenuExpanded
        {
            get => _isMenuExpanded;
            set => SetProperty(ref _isMenuExpanded, value);
        }

        public LeftMenuItem? SelectedMenuItem
        {
            get => _selectedMenuItem;
            set
            {
                if (
                    SetProperty(ref _selectedMenuItem, value)
                    && value != null
                    && !string.IsNullOrEmpty(value.ViewName)
                )
                {
                    _ = NavigateAsync(value.ViewName);
                }
            }
        }

        public LeftMenuItem? SelectedFlyoutItem
        {
            get => _selectedFlyoutItem;
            set => SetProperty(ref _selectedFlyoutItem, value);
        }

        #endregion

        #region 命令

        public DelegateCommand BackCommand { get; }
        public DelegateCommand ForwardCommand { get; }

        #endregion

        #region 構造函數

        public MainWinViewModel(
            IRegionManager? regionManager,
            IEventAggregator eventAggregator,
            IAuthService authService
        )
        {
            _regionManager = regionManager;
            _eventAggregator = eventAggregator;
            _authService = authService;

            // 訂閱通知事件
            _eventAggregator
                .GetEvent<PubSubEvent<NotificationMessage>>()
                .Subscribe(OnNotificationReceived, ThreadOption.UIThread);

            // 訂閱認證狀態變更：登錄/登出時刷新 UI
            _authService.AuthStateChanged += OnAuthStateChanged;

            BackCommand = new DelegateCommand(OnBack);
            ForwardCommand = new DelegateCommand(OnForward);

            BuildMenus();
            BuildFlyoutMenus();

            // 初始狀態下根據認證情況過濾一次
            ApplyMenuPermissions();
            ApplyFlyoutPermissions();
        }

        #endregion

        #region 認證狀態回調

        private void OnAuthStateChanged(object? sender, EventArgs e)
        {
            // 認證狀態變化時刷新所有相關綁定
            RaisePropertyChanged(nameof(IsAuthenticated));
            RaisePropertyChanged(nameof(Username));

            // 根據新的用戶權限重算菜單
            ApplyMenuPermissions();
            ApplyFlyoutPermissions();
        }

        #endregion

        #region 通知

        private void OnNotificationReceived(NotificationMessage msg)
        {
            Notifications.Show(msg.Message, msg.Type, msg.ActionText, msg.ActionCommand);
        }

        #endregion

        #region 菜單構建

        private void BuildMenus()
        {
            _allMenuItems = new ObservableCollection<LeftMenuItem>
            {
                new LeftMenuItem
                {
                    Icon = MaterialIconKind.Home,
                    Title = "首頁",
                    ViewName = "HomePage",
                    LimitUserName = new ObservableCollection<string> { "AllUser" },
                },
                new LeftMenuItem
                {
                    Icon = MaterialIconKind.Database,
                    Title = "檢測管理",
                    LimitUserName = new ObservableCollection<string> { "AllUser" },
                    SubItems = new ObservableCollection<LeftMenuItem>
                    {
                        new LeftMenuItem
                        {
                            Icon = MaterialIconKind.SmokeDetector,
                            Title = "數據標註",
                            ViewName = "AnnotationPage",
                            LimitUserName = new ObservableCollection<string> { "AllUser" },
                        },
                        new LeftMenuItem
                        {
                            Icon = MaterialIconKind.GlobeModel,
                            Title = "模型管理",
                            ViewName = "OnnxModelPage",
                            LimitUserName = new ObservableCollection<string> { "AllUser" },
                        },
                        new LeftMenuItem
                        {
                            Icon = MaterialIconKind.Magnify,
                            Title = "實時檢測",
                            ViewName = "Detect",
                            LimitUserName = new ObservableCollection<string> { "AllUser" },
                        },
                        new LeftMenuItem
                        {
                            Icon = MaterialIconKind.Magnify,
                            Title = "類別對照庫",
                            ViewName = "Detect",
                            LimitUserName = new ObservableCollection<string> { "AllUser" },
                        },
                    },
                },
                new LeftMenuItem
                {
                    Icon = MaterialIconKind.CogOutline,
                    Title = "設置",
                    LimitUserName = new ObservableCollection<string> { "AllUser" },
                    SubItems = new ObservableCollection<LeftMenuItem>
                    {
                        new LeftMenuItem{
                            Icon=MaterialIconKind.Group,
                            Title="類別管理",
                            ViewName="RuleClassPage",
                            LimitUserName=new ObservableCollection<string>{"AllUser"}
                        },
                        new LeftMenuItem{
                            Icon=MaterialIconKind.Group,
                            Title="群組管理",
                            ViewName="ProductGroupPage",
                            LimitUserName=new ObservableCollection<string>{"AllUser"}
                        },
                        new LeftMenuItem{
                            Icon=MaterialIconKind.ChartProductionPossibilityFrontier,
                            Title="產品管理",
                            ViewName="ProductPage",
                            LimitUserName=new ObservableCollection<string>{"AllUser"}
                        },
                        new LeftMenuItem
                        {
                            Icon=MaterialIconKind.Ruler,
                            Title="規則設定",
                            ViewName="RuleMakingPage",
                            LimitUserName=new ObservableCollection<string>{"AllUser"}
                        },
                        new LeftMenuItem
                        {
                            Icon = MaterialIconKind.Cog,
                            Title = "檢測設置",
                            ViewName = "Settings",
                            LimitUserName = new ObservableCollection<string> { "AllUser" },
                        },
                        new LeftMenuItem
                        {
                            Icon = MaterialIconKind.Account,
                            Title = "用戶中心",
                            ViewName = "UserConfigPage",
                            LimitUserName = new ObservableCollection<string> { "AllUser" },
                        },
                    },
                },
            };
        }

        private void BuildFlyoutMenus()
        {
            _allFlyoutItems = new ObservableCollection<LeftMenuItem>
            {
                new LeftMenuItem
                {
                    Icon = MaterialIconKind.Account,
                    Title = "個性設置",
                    ViewName = "UserPreferencePage",
                    LimitUserName = new ObservableCollection<string> { "AllUser" },
                    Command = new DelegateCommand(async () =>
                        await NavigateAsync("UserPreferencePage")
                    ),
                },
                new LeftMenuItem
                {
                    Icon = MaterialIconKind.Cog,
                    Title = "檢測設置",
                    ViewName = "Settings",
                    LimitUserName = new ObservableCollection<string> { "AllUser" },
                    Command = new DelegateCommand(async () => await NavigateAsync("Settings")),
                },
                new LeftMenuItem
                {
                    Icon = MaterialIconKind.Information,
                    Title = "關於",
                    LimitUserName = new ObservableCollection<string> { "AllUser" },
                    Command = new DelegateCommand(OnShowAbout),
                },
                new LeftMenuItem
                {
                    Icon = MaterialIconKind.LogoutVariant,
                    Title = "退出登錄",
                    LimitUserName = new ObservableCollection<string> { "AllUser" },
                    Command = new DelegateCommand(OnLogout),
                },
                new LeftMenuItem
                {
                    Icon = MaterialIconKind.CodeGreaterThanOrEqual,
                    Title = "程序信息",
                    LimitUserName = new ObservableCollection<string> { "L5940", "L5126", "1817" },
                    Command = new DelegateCommand(OnShowProgramInfo),
                },
            };
        }

        #endregion

        #region 權限過濾

        private void ApplyMenuPermissions()
        {
            if (_allMenuItems == null)
                return;

            string userName = _authService.CurrentUser?.Username ?? "";
            var filtered = new ObservableCollection<LeftMenuItem>();

            foreach (var item in _allMenuItems)
            {
                var filteredItem = FilterMenuItem(item, userName);
                if (filteredItem != null)
                    filtered.Add(filteredItem);
            }

            MenuItems = filtered;
            RaisePropertyChanged(nameof(MenuItems));
        }

        private LeftMenuItem? FilterMenuItem(LeftMenuItem item, string userName)
        {
            // 構造一個淺拷貝，避免修改原始數據
            var clone = new LeftMenuItem
            {
                Icon = item.Icon,
                Title = item.Title,
                ViewName = item.ViewName,
                LimitUserName = item.LimitUserName,
                CommandName = item.CommandName,
                Command = item.Command,
                SubItems = new ObservableCollection<LeftMenuItem>(),
            };

            // 遞歸過濾子項
            if (item.SubItems != null && item.SubItems.Any())
            {
                foreach (var sub in item.SubItems)
                {
                    var filteredSub = FilterMenuItem(sub, userName);
                    if (filteredSub != null)
                        clone.SubItems.Add(filteredSub);
                }
            }

            // 自身有權限，或有可見的子項，才保留
            bool selfHasPermission = HasPermission(item, userName);
            if (!selfHasPermission && clone.SubItems.Count == 0)
                return null;

            return clone;
        }

        private void ApplyFlyoutPermissions()
        {
            FlyoutMenuItems.Clear();
            if (_allFlyoutItems == null)
                return;

            string userName = _authService.CurrentUser?.Username ?? "";
            var filtered = _allFlyoutItems.Where(item => HasPermission(item, userName));

            foreach (var item in filtered)
            {
                var menuItem = new MenuItem
                {
                    Header = item.Title,
                    Command = item.Command,
                    Icon = new MaterialIcon
                    {
                        Kind = item.Icon,
                        Width = 20,
                        Height = 20,
                    },
                };
                FlyoutMenuItems.Add(menuItem);
            }
        }

        private bool HasPermission(LeftMenuItem item, string userName)
        {
            if (item.LimitUserName == null || item.LimitUserName.Count == 0)
                return true;
            if (item.LimitUserName.Contains("AllUser"))
                return true;
            return item.LimitUserName.Contains(userName);
        }

        #endregion

        #region 導航
        public void RequestInitialNavigation()
        {
            // 首次打開窗口時，若未登錄則導航到登錄頁
            if (!_authService.IsAuthenticated)
                _ = NavigateAsync("LoginPage");
        }

        // public Task NavigateAsync(string viewName, NavigationParameters? paras = null)
        // {
        //     if (string.IsNullOrEmpty(viewName) || _regionManager == null)
        //         return Task.CompletedTask;
        //
        //     var parameters = paras ?? new NavigationParameters();
        //
        //     _regionManager
        //         .Regions["MainRegion"]
        //         .RequestNavigate(
        //             viewName,
        //             callback =>
        //             {
        //                 if (callback.Success)
        //                     _journal = callback.Context.NavigationService.Journal;
        //                 else
        //                     System.Diagnostics.Debug.WriteLine(
        //                         $"導航至 {viewName} 失敗: {callback.Exception?.Message}"
        //                     );
        //             },
        //             parameters
        //         );
        //
        //     return Task.CompletedTask;
        // }
        public Task NavigateAsync(string viewName, NavigationParameters? paras = null)
        {
            if (string.IsNullOrEmpty(viewName) || _regionManager == null)
                return Task.CompletedTask;

            var parameters = paras ?? new NavigationParameters();

            // ✅ 用這個，不用 Regions["MainRegion"]，避免 Region 未就緒的問題
            _regionManager.RequestNavigate(
                "MainRegion",
                viewName,
                callback =>
                {
                    if (callback.Success)
                        _journal = callback.Context.NavigationService.Journal;
                    else
                        System.Diagnostics.Debug.WriteLine(
                            $"導航至 {viewName} 失敗: {callback.Exception?.Message}"
                        );
                },
                parameters
            );

            return Task.CompletedTask;
        }

        private void OnBack()
        {
            if (_journal?.CanGoBack == true)
                _journal.GoBack();
        }

        private void OnForward()
        {
            if (_journal?.CanGoForward == true)
                _journal.GoForward();
        }

        #endregion

        #region 退出登錄 / 對話框

        private void OnLogout()
        {
            _authService.SignOut();
            // 認證狀態改變後 ApplyMenuPermissions 會自動重算菜單
            // 然後手動導航回 LoginPage
            _ = NavigateAsync("LoginPage");
        }

        private async void OnShowAbout()
        {
            await ShowDialogAsync("AboutDialog");
        }

        private async void OnShowProgramInfo()
        {
            await ShowDialogAsync("UploadDialog");
        }

        private async Task ShowDialogAsync(
            string dialogName,
            Prism.Dialogs.IDialogParameters? paras = null
        )
        {
            var dialogService =
                Prism.Ioc.ContainerLocator.Container.Resolve<Prism.Dialogs.IDialogService>();

            var tcs = new TaskCompletionSource<Prism.Dialogs.IDialogResult>();
            dialogService.ShowDialog(
                dialogName,
                paras ?? new Prism.Dialogs.DialogParameters(),
                result => tcs.SetResult(result)
            );
            await tcs.Task;
        }

        #endregion
    }
}
