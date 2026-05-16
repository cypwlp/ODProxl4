// ReviseConfigDialogViewModel.cs
using ODProxl.ClientDtos;

namespace ODProxl.ViewModels.Dialogs
{
    public class ReviseConfigDialogViewModel : BindableBase, IDialogAware
    {
        private string _configName = string.Empty;
        private string _configKey = string.Empty;
        private string _configValue = string.Empty;
        private bool _isGlobal;

        public ReviseConfigDialogViewModel()
        {
            OkCommand = new DelegateCommand(Submit, () =>
                !string.IsNullOrWhiteSpace(ConfigName) &&
                !string.IsNullOrWhiteSpace(ConfigKey))
                .ObservesProperty(() => ConfigName)
                .ObservesProperty(() => ConfigKey);

            CancelCommand = new DelegateCommand(() =>
                RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel)));
        }

        public string ConfigName
        {
            get => _configName;
            set => SetProperty(ref _configName, value);
        }

        public string ConfigKey
        {
            get => _configKey;
            set => SetProperty(ref _configKey, value);
        }

        public string ConfigValue
        {
            get => _configValue;
            set => SetProperty(ref _configValue, value);
        }

        public bool IsGlobal
        {
            get => _isGlobal;
            set => SetProperty(ref _isGlobal, value);
        }

        public DelegateCommand OkCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public string Title => "設定";

        DialogCloseListener IDialogAware.RequestClose { get; }

        public event Action<IDialogResult>? RequestClose;

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.TryGetValue<ConfigDto>("config", out var config))
            {
                ConfigName = config.CgModuleName ?? "";
                ConfigKey = config.CgKey;
                ConfigValue = config.CgValue;
                IsGlobal = config.CgUserAccount == "AllUser";
            }
        }

        private void Submit()
        {
            var resultConfig = new ConfigDto
            {
                CgId = 0,
                CgUserAccount = IsGlobal ? "AllUser" : null,
                CgModuleName = ConfigName,
                CgKey = ConfigKey,
                CgValue = ConfigValue
            };

            if (RequestClose != null)
            {
                var parameters = new DialogParameters
                {
                    { "resultConfig", resultConfig }
                };
                RequestClose.Invoke(new DialogResult(ButtonResult.OK) { Parameters = parameters });
            }
        }
    }
}