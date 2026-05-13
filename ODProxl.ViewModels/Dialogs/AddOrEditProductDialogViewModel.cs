using ODProxl.ClientDtos;
using ODProxl.Utils.HttpService;
using RestSharp;

namespace ODProxl.ViewModels.Dialogs
{
    public class AddOrEditProductDialogViewModel : BindableBase, IDialogAware
    {
        private readonly IHttpRestClient _httpRestClient;

        public string Title { get; private set; } = "新增產品";

        private ProductDto _currentProduct = new();
        public ProductDto CurrentProduct
        {
            get => _currentProduct;
            set => SetProperty(ref _currentProduct, value);
        }

        public bool IsNewProduct { get; private set; }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public DialogCloseListener RequestClose { get; set; }

        // 建構式注入 HttpRestClient
        public AddOrEditProductDialogViewModel(IHttpRestClient httpRestClient)
        {
            _httpRestClient = httpRestClient;

            SaveCommand = new DelegateCommand(async () => await SaveAsync());
            CancelCommand = new DelegateCommand(Cancel);
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("CurrentProduct"))
            {
                CurrentProduct = parameters.GetValue<ProductDto>("CurrentProduct");
            }

            IsNewProduct = parameters.ContainsKey("IsNewProduct")
                && parameters.GetValue<bool>("IsNewProduct");

            if (parameters.ContainsKey("Title"))
            {
                Title = parameters.GetValue<string>("Title")!;
            }
            else
            {
                Title = IsNewProduct ? "新增產品" : "編輯產品";
            }
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentProduct.ProductCode) ||
                string.IsNullOrWhiteSpace(CurrentProduct.ProductName))
            {
                // TODO: 之後可加入錯誤提示對話框
                return;
            }

            if (IsNewProduct)
            {
                // 新增
                var createDto = new CreateProductDto
                {
                    ProductCode = CurrentProduct.ProductCode,
                    ProductName = CurrentProduct.ProductName,
                    Description = CurrentProduct.Description ?? "",
                    IsActive = CurrentProduct.IsActive
                };

                var request = new ClientRequest
                {
                    Url = "Product",
                    Method = Method.Post,
                    ContentType = "application/json",
                    Parameters = createDto
                };

                var response = await _httpRestClient.ExecuteAsync<ProductDto>(request);

                if (response.IsSuccess)
                {
                    RequestClose.Invoke(new DialogResult(ButtonResult.OK));
                }
                // TODO: 失敗時可顯示錯誤訊息
            }
            else
            {
                // 編輯（待你後續補齊）
                var updateDto = new UpdateProductDto
                {
                    ProductCode = CurrentProduct.ProductCode,
                    ProductName = CurrentProduct.ProductName,
                    Description = CurrentProduct.Description ?? "",
                    IsActive = CurrentProduct.IsActive
                };

                var request = new ClientRequest
                {
                    Url = $"Product/{CurrentProduct.ProductId}",
                    Method = Method.Put,
                    ContentType = "application/json",
                    Parameters = updateDto
                };

                var response = await _httpRestClient.ExecuteAsync<ProductDto>(request);

                if (response.IsSuccess)
                {
                    RequestClose.Invoke(new DialogResult(ButtonResult.OK));
                }
            }
        }

        private void Cancel()
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        public bool CanCloseDialog() => true;
        public void OnDialogClosed() { }
    }
}