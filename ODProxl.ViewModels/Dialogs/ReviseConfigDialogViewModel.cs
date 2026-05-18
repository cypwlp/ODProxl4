using ODProxl.ClientDtos;

namespace ODProxl.ViewModels.Dialogs
{
    public class ReviseConfigDialogViewModel : BindableBase, IDialogAware
    {
        private ConfigDto? _originalConfig;
        private string _configName = string.Empty;
        private string _configKey = string.Empty;
        private string _configValue = string.Empty;
        private bool _isGlobal;
        private string _confirmButtonText = "確定新增";
        private string _title = "設定";
        private DialogCloseListener _requestClose;

        public ReviseConfigDialogViewModel()
        {
            OkCommand = new DelegateCommand(Submit, () =>
                !string.IsNullOrWhiteSpace(ConfigName) &&
                !string.IsNullOrWhiteSpace(ConfigKey))
                .ObservesProperty(() => ConfigName)
                .ObservesProperty(() => ConfigKey);

            CancelCommand = new DelegateCommand(() =>
                RequestClose.Invoke(new DialogResult(ButtonResult.Cancel)));
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

        public string ConfirmButtonText
        {
            get => _confirmButtonText;
            set => SetProperty(ref _confirmButtonText, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public DelegateCommand OkCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public DialogCloseListener RequestClose
        {
            get => _requestClose;
            set => _requestClose = value;
        }

        public bool CanCloseDialog() => true;

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters != null && parameters.TryGetValue<ConfigDto>("config", out var config))
            {
                Title = "編輯設定";
                ConfirmButtonText = "確定修改";
                _originalConfig = config;
                ConfigName = config.CgModuleName ?? "";
                ConfigKey = config.CgKey;
                ConfigValue = config.CgValue;
                IsGlobal = config.CgUserAccount == "AllUser";
            }
            else
            {
                Title = "新增設定";
                ConfirmButtonText = "確定新增";
                _originalConfig = null;
                ConfigName = "";
                ConfigKey = "";
                ConfigValue = "";
                IsGlobal = false;
            }
        }

        private void Submit()
        {
            var resultConfig = new ConfigDto
            {
                CgId = _originalConfig?.CgId ?? 0,
                CgUserAccount = IsGlobal ? "AllUser" : null,
                CgType = _originalConfig?.CgType ?? "開發者設定",
                CgModuleName = ConfigName,
                CgKey = ConfigKey,
                CgValue = ConfigValue
            };

            var parameters = new DialogParameters
            {
                { "resultConfig", resultConfig }
            };
            RequestClose.Invoke(new DialogResult(ButtonResult.OK) { Parameters = parameters });
        }
    }
}