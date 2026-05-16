namespace ODProxl.ViewModels.Dialogs
{
    public class ReviseConfigDialogViewModel : BindableBase, IDialogAware
    {
        #region IDialogAware 成員
        public string? Title;
        public DialogCloseListener RequestClose { get; set; }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {

        }

        public void OnDialogOpened(IDialogParameters parameters)
        {

        }
        #endregion

        #region 字段與構造函數
        private string _configName;
        private string _configKey;
        private string _configValue;

        public ReviseConfigDialogViewModel()
        {

        }

        #endregion

        #region 屬性
        public string ConfigName
        {
            get { return _configName; }
            set { SetProperty(ref _configName, value); }
        }

        public string ConfigKey
        {
            get { return _configKey; }
            set { SetProperty(ref _configKey, value); }
        }

        public string ConfigValue
        {
            get { return _configValue; }
            set { SetProperty(ref _configValue, value); }
        }
        #endregion

    }
}
