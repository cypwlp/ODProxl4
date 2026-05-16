// UserConfigPageViewModel.cs
using ODProxl.ClientDtos;
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

        private ObservableCollection<ConfigDto> _allConfigs = new();
        private ObservableCollection<ConfigDto> _developerConfigs = new();
        private ConfigDto? _selectedDeveloperConfig;
        private bool _hasChanges;

        public UserConfigPageViewModel(
            IConfigManager configManager,
            IDialogService dialogService,
            IHttpRestClient httpRestClient)
        {
            _configManager = configManager;
            _dialogService = dialogService;
            _httpRestClient = httpRestClient;

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
            foreach (var config in _allConfigs)
            {
                var dto = new CreateUserConfigDto
                {
                    ConfigKey = config.CgKey,
                    ConfigValue = config.CgValue,
                    ConfigUserAccount = config.CgUserAccount
                };

                ClientRequest request;
                if (config.CgId > 0)
                {
                    request = new ClientRequest
                    {
                        Url = $"Config/{config.CgId}",
                        Method = Method.Put,
                        ContentType = "application/json",
                        Parameters = dto
                    };
                }
                else
                {
                    request = new ClientRequest
                    {
                        Url = "Config",
                        Method = Method.Post,
                        ContentType = "application/json",
                        Parameters = dto
                    };
                }

                var response = await _httpRestClient.ExecuteAsync<ConfigDto>(request);
                if (!response.IsSuccess)
                {
                    break;
                }
            }

            await _configManager.RefreshAsync();
            HasChanges = false;
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            LoadConfigsFromManager();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}