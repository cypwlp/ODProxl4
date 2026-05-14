namespace ODProxl.ViewModels.Dialogs
{
    public class RevisionDetailsDialogViewModel : BindableBase, IDialogAware
    {
        public string Title { get; set; } = "新增或修訂明細";
        public DialogCloseListener RequestClose { get; set; }

        private int _detailId;
        private string _attrName = string.Empty;
        private string _attrValue = string.Empty;
        private string _attrUnit = string.Empty;
        private int _classId;

        public string AttrName
        {
            get => _attrName;
            set => SetProperty(ref _attrName, value);
        }

        public string AttrValue
        {
            get => _attrValue;
            set => SetProperty(ref _attrValue, value);
        }

        public string AttrUnit
        {
            get => _attrUnit;
            set => SetProperty(ref _attrUnit, value);
        }

        public int ClassId
        {
            get => _classId;
            set => SetProperty(ref _classId, value);
        }

        public DelegateCommand<string?> CloseCommand { get; }

        public RevisionDetailsDialogViewModel()
        {
            CloseCommand = new DelegateCommand<string?>(OnClose);
        }

        private void OnClose(string? parameter)
        {
            if (parameter == "true")
            {
                var parameters = new DialogParameters
                {
                    { "DetailId", _detailId },
                    { "AttrName", AttrName },
                    { "AttrValue", AttrValue },
                    { "AttrUnit", AttrUnit },
                    { "ClassId", ClassId }
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
            if (parameters.ContainsKey("DetailId"))
            {
                _detailId = parameters.GetValue<int>("DetailId");
                Title = "修訂明細";
            }
            else
            {
                _detailId = 0;
                Title = "新增明細";
            }

            if (parameters.ContainsKey("AttrName"))
                AttrName = parameters.GetValue<string>("AttrName");

            if (parameters.ContainsKey("AttrValue"))
                AttrValue = parameters.GetValue<string>("AttrValue");

            if (parameters.ContainsKey("AttrUnit"))
                AttrUnit = parameters.GetValue<string>("AttrUnit");

            if (parameters.ContainsKey("ClassId"))
                ClassId = parameters.GetValue<int>("ClassId");
        }
    }
}