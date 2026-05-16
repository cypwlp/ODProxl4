// ODProxl/ViewModels/Pages/UserConfigPageViewModel.cs
using ODProxl.ClientDtos;
using ODProxl.ClientServices;
using ODProxl.Global.Services;
using ODProxl.Utils.HttpService;
using RestSharp;
using System.Collections.ObjectModel;

namespace ODProxl.ViewModels.Pages
{
    public class UserConfigPageViewModel : BindableBase, INavigationAware
    {
        private readonly IConfigManager _configManager;
        private readonly IDialogService _dialogService;
        private readonly IHttpRestClient _httpRestClient;
        private readonly IAuthService _authService;

        private ObservableCollection<ConfigDto> _allConfigs;
        private ObservableCollection<ConfigDto> _developerConfigs;
        private ConfigDto? _selectedDeveloperConfig;
        private bool _hasChanges;
        private bool _isDeveloperMode;

        public UserConfigPageViewModel(
            IConfigManager configManager,
            IDialogService dialogService,
            IHttpRestClient httpRestClient,
            IAuthService authService)
        {
            _configManager = configManager;
            _dialogService = dialogService;
            _httpRestClient = httpRestClient;
            _authService = authService;

            _configManager.ConfigChanged += LoadConfigsFromManager;

            AddDeveloperSettingCommand = new DelegateCommand(async () => await OpenAddDialogAsync());
            EditDeveloperSettingCommand = new DelegateCommand(async () =>
            {
                if (SelectedDeveloperConfig != null)
                    await OpenEditDialogAsync(SelectedDeveloperConfig);
            });
            SaveCommand = new AsyncDelegateCommand(SaveAllChangesAsync, () => HasChanges)
                .ObservesProperty(() => HasChanges);
            CancelCommand = new DelegateCommand(ReloadFromManager);
        }

        public ObservableCollection<ConfigDto> AllConfigs
        {
            get => _allConfigs;
            set => SetProperty(ref _allConfigs, value);
        }

        public ObservableCollection<ConfigDto> DeveloperConfigs
        {
            get => _developerConfigs;
            set => SetProperty(ref _developerConfigs, value);
        }

        public ConfigDto? SelectedDeveloperConfig
        {
            get => _selectedDeveloperConfig;
            set => SetProperty(ref _selectedDeveloperConfig, value);
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set => SetProperty(ref _hasChanges, value);
        }

        public bool IsDeveloperMode
        {
            get => _isDeveloperMode;
            set => SetProperty(ref _isDeveloperMode, value);
        }

        public DelegateCommand AddDeveloperSettingCommand { get; }
        public DelegateCommand EditDeveloperSettingCommand { get; }
        public AsyncDelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        private void LoadConfigsFromManager()
        {
            var configs = _configManager.AllConfigs.ToList();
            AllConfigs = new ObservableCollection<ConfigDto>(configs);
            DeveloperConfigs = new ObservableCollection<ConfigDto>(
                configs.Where(c => c.CgType == "開發者設定" || c.CgModuleName == "允許開發者設定"));

            var developerModeConfig = configs.FirstOrDefault(c => c.CgModuleName == "允許開發者設定");
            if (developerModeConfig != null && !string.IsNullOrWhiteSpace(developerModeConfig.CgType))
            {
                var allowedUsers = developerModeConfig.CgType
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(u => u.Trim())
                    .ToList();

                var currentUser = _authService.CurrentUser?.Username;
                IsDeveloperMode = allowedUsers.Contains(currentUser, System.StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                IsDeveloperMode = false;
            }

            HasChanges = false;
        }

        private void ReloadFromManager()
        {
            LoadConfigsFromManager();
        }

        private async Task OpenAddDialogAsync()
        {
            var parameters = new DialogParameters();
            _dialogService.ShowDialog("ReviseConfigDialog", parameters, async result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    var newConfig = result.Parameters.GetValue<ConfigDto>("resultConfig");
                    AddOrUpdateLocalConfig(newConfig);
                }
            });
        }

        private async Task OpenEditDialogAsync(ConfigDto config)
        {
            var parameters = new DialogParameters { { "config", config } };
            _dialogService.ShowDialog("ReviseConfigDialog", parameters, async result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    var updatedConfig = result.Parameters.GetValue<ConfigDto>("resultConfig");
                    AddOrUpdateLocalConfig(updatedConfig);
                }
            });
        }

        private void AddOrUpdateLocalConfig(ConfigDto config)
        {
            if (config.CgId == 0)
            {
                _allConfigs.Add(config);
            }
            else
            {
                var existing = _allConfigs.FirstOrDefault(c => c.CgId == config.CgId);
                if (existing != null)
                {
                    var index = _allConfigs.IndexOf(existing);
                    _allConfigs[index] = config;
                }
                else
                {
                    _allConfigs.Add(config);
                }
            }

            RefreshDeveloperConfigs();
            HasChanges = true;
        }

        private void RefreshDeveloperConfigs()
        {
            DeveloperConfigs = new ObservableCollection<ConfigDto>(
                _allConfigs.Where(c => c.CgType == "開發者設定" || c.CgModuleName == "允許開發者設定"));
        }

        private async Task SaveAllChangesAsync()
        {
            var batchItems = _allConfigs.Select(c => new
            {
                cgId = c.CgId,
                configKey = c.CgKey,
                configValue = c.CgValue,
                configUserAccount = c.CgUserAccount,
                cgModuleName = c.CgModuleName
            }).ToList();

            var request = new ClientRequest
            {
                Url = "Config/batch",
                Method = Method.Post,
                ContentType = "application/json",
                Parameters = batchItems
            };

            var response = await _httpRestClient.ExecuteAsync<List<ConfigDto>>(request);
            if (response.IsSuccess)
            {
                await _configManager.RefreshAsync();
                HasChanges = false;
            }
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            LoadConfigsFromManager();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}