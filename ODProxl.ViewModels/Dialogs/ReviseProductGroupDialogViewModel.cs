namespace ODProxl.ViewModels.Dialogs
{
    public class ReviseProductGroupDialogViewModel : BindableBase, IDialogAware
    {
        #region IDialogAware implementation

        public string? Title { get; set; }
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
            if (parameters.ContainsKey("GroupName"))
            {
                GroupName = parameters.GetValue<string>("GroupName");
                Title = "修訂群組";
            }
            else
            {
                GroupName = string.Empty;
                Title = "新增群組";
            }
            if (parameters.ContainsKey("IsActive"))
                IsActive = parameters.GetValue<bool>("IsActive");
        }

        #endregion

        #region 字段與構造函數

        private string? _groupName;
        private bool _isActive;

        public DelegateCommand<object> CloseCommand { get; }

        public ReviseProductGroupDialogViewModel()
        {
            _groupName = string.Empty;
            _isActive = true;
            CloseCommand = new DelegateCommand<object>(ExecuteClose);
        }

        #endregion

        #region 方法

        private void ExecuteClose(object parameter)
        {
            bool isOk = parameter?.ToString() == "true";
            if (isOk)
            {
                if (string.IsNullOrWhiteSpace(GroupName))
                {
                    RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
                    return;
                }
                var parameters = new DialogParameters
                {
                    { "GroupName", GroupName },
                    { "IsActive", IsActive }
                };
                RequestClose.Invoke(new DialogResult(ButtonResult.OK) { Parameters = parameters });
            }
            else
            {
                RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
            }
        }

        #endregion

        #region 屬性

        public string? GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        #endregion
    }
}