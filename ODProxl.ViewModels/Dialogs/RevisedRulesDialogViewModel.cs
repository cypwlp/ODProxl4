using ODProxl.ClientServices;
using ODProxl.Utils.HttpService;
using RestSharp;
using System.Collections.ObjectModel;

namespace ODProxl.ViewModels.Dialogs
{
    public class RevisedRulesDialogViewModel : BindableBase, IDialogAware
    {
        public string Title { get; set; }
        public DialogCloseListener RequestClose { get; set; }

        private int _ruleId;
        private string _ruleName = string.Empty;
        private bool _isActive = true;
        private ObservableCollection<string>? _productCodes;
        private string? _selectedProductCode;
        private readonly IHttpRestClient _httpRestClient;
        private readonly IEventAggregator _eventAggregator;
        private readonly IAuthService _authService;

        public RevisedRulesDialogViewModel(IHttpRestClient httpRestClient, IEventAggregator eventAggregator, IAuthService authService)
        {
            _httpRestClient = httpRestClient;
            _eventAggregator = eventAggregator;
            _authService = authService;
            ProductCodes = new ObservableCollection<string>();
        }
        public ObservableCollection<string>? ProductCodes
        {
            get => _productCodes;
            set => SetProperty(ref _productCodes, value);
        }

        public string? SelectedProductCode
        {
            get => _selectedProductCode;
            set => SetProperty(ref _selectedProductCode, value);
        }

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

        public async void OnDialogOpened(IDialogParameters parameters)
        {
            await LoadProductCodesAsync();
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

            if (parameters.ContainsKey("ProductCode"))
            {
                SelectedProductCode = parameters.GetValue<string>("ProductCode");
            }
        }


        private async Task LoadProductCodesAsync()
        {
            try
            {
                var request = new ClientRequest
                {
                    Url = "Product/product_codes",
                    Method = Method.Get,
                    ContentType = "application/json",
                };

                var response = await _httpRestClient.ExecuteAsync<List<string>>(request);
                if (response.IsSuccess && response.Data != null)
                {
                    ProductCodes = new ObservableCollection<string>(response.Data);
                }
                else
                {

                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}