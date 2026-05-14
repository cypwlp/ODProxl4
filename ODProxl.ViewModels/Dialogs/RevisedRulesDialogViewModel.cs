namespace ODProxl.ViewModels.Dialogs
{
    public class RevisedRulesDialogViewModel : BindableBase, IDialogAware
    {
        public string Title { get; set; } = "新增或修訂規則";
        public DialogCloseListener RequestClose { get; set; }

        private int _ruleId;
        private string _ruleName = string.Empty;
        private bool _isActive = true;

        public string RuleName
        {
            get => _ruleName;
            set => SetProperty(ref _ruleName, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public DelegateCommand<string?> CloseCommand { get; }

        public RevisedRulesDialogViewModel()
        {
            CloseCommand = new DelegateCommand<string?>(OnClose);
        }

        private void OnClose(string? parameter)
        {
            if (parameter == "true")
            {
                var parameters = new DialogParameters
                {
                    { "RuleId", _ruleId },
                    { "RuleName", RuleName },
                    { "IsActive", IsActive }
                };
                RequestClose.Invoke(parameters);
            }
            else
            {
                RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
            }
        }

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("RuleId"))
            {
                _ruleId = parameters.GetValue<int>("RuleId");
                Title = "修訂規則";
            }
            else
            {
                _ruleId = 0;
                Title = "新增規則";
            }

            if (parameters.ContainsKey("RuleName"))
                RuleName = parameters.GetValue<string>("RuleName");

            if (parameters.ContainsKey("IsActive"))
                IsActive = parameters.GetValue<bool>("IsActive");
        }
    }
}