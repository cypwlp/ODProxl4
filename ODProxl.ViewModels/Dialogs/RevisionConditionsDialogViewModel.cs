using System.Collections.ObjectModel;

namespace ODProxl.ViewModels.Dialogs
{
    public class RevisionConditionsDialogViewModel : BindableBase, IDialogAware
    {
        public string Title { get; set; } = "新增或修訂條件";
        public DialogCloseListener RequestClose { get; set; }

        private int _conditionId;
        private string _conditionName = string.Empty;
        private string _selectedOperator = "<=";
        private decimal _value;
        private string _unit = string.Empty;

        public string ConditionName
        {
            get => _conditionName;
            set => SetProperty(ref _conditionName, value);
        }

        public string SelectedOperator
        {
            get => _selectedOperator;
            set => SetProperty(ref _selectedOperator, value);
        }

        public decimal Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        public ObservableCollection<string> Operators { get; } = new ObservableCollection<string> { "<=", ">", "==", "!=", "<", ">=" };

        public DelegateCommand<string?> CloseCommand { get; }

        public RevisionConditionsDialogViewModel()
        {
            CloseCommand = new DelegateCommand<string?>(OnClose);
        }

        private void OnClose(string? parameter)
        {
            if (parameter == "true")
            {
                var parameters = new DialogParameters
                {
                    { "ConditionId", _conditionId },
                    { "ConditionName", ConditionName },
                    { "Operator", SelectedOperator },
                    { "Value", Value },
                    { "Unit", Unit }
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
            if (parameters.ContainsKey("ConditionId"))
            {
                _conditionId = parameters.GetValue<int>("ConditionId");
                Title = "修訂條件";
            }
            else
            {
                _conditionId = 0;
                Title = "新增條件";
            }

            if (parameters.ContainsKey("ConditionName"))
                ConditionName = parameters.GetValue<string>("ConditionName");

            if (parameters.ContainsKey("Operator"))
                SelectedOperator = parameters.GetValue<string>("Operator");

            if (parameters.ContainsKey("Value"))
                Value = parameters.GetValue<decimal>("Value");

            if (parameters.ContainsKey("Unit"))
                Unit = parameters.GetValue<string>("Unit");
        }
    }
}