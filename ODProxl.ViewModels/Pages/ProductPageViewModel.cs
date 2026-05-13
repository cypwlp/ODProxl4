using ODProxl.ClientDtos;
using ODProxl.ClientServices;
using ODProxl.Utils.HttpService;
using RestSharp;
using System.Collections.ObjectModel;

namespace ODProxl.ViewModels.Pages
{
    public class ProductPageViewModel : BindableBase, INavigationAware
    {
        private readonly IHttpRestClient _httpRestClient;
        private readonly IAuthService _authService;
        private readonly IDialogService _dialogService;

        public ProductPageViewModel(IHttpRestClient httpRestClient,
                                    IAuthService authService,
                                    IDialogService dialogService)
        {
            _httpRestClient = httpRestClient;
            _authService = authService;
            _dialogService = dialogService;

            AddProductCommand = new DelegateCommand(OpenAddDialog);
            EditProductCommand = new DelegateCommand<ProductDto>(OpenEditDialog);
            DeleteProductCommand = new DelegateCommand<ProductDto>(async (p) => await DeleteProductAsync(p));

            Products = new ObservableCollection<ProductDto>();
        }

        #region 屬性
        private ObservableCollection<ProductDto> _products;
        public ObservableCollection<ProductDto> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        private ProductDto _currentProduct = new ProductDto();
        public ProductDto CurrentProduct
        {
            get => _currentProduct;
            set => SetProperty(ref _currentProduct, value);
        }

        private string _dialogTitle = "新增產品";
        public string DialogTitle
        {
            get => _dialogTitle;
            set => SetProperty(ref _dialogTitle, value);
        }

        private bool _isNewProduct;
        #endregion

        #region 命令
        public DelegateCommand AddProductCommand { get; }
        public DelegateCommand<ProductDto> EditProductCommand { get; }
        public DelegateCommand<ProductDto> DeleteProductCommand { get; }
        #endregion

        #region INavigationAware
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            await LoadProductsAsync();
        }
        #endregion

        #region 加載產品列表
        private async Task LoadProductsAsync()
        {
            var request = new ClientRequest
            {
                Url = "Product",
                Method = Method.Get,
                ContentType = "application/json"
            };

            var response = await _httpRestClient.ExecuteAsync<List<ProductDto>>(request);

            if (response.IsSuccess && response.Data != null)
            {
                Products = new ObservableCollection<ProductDto>(response.Data);
            }
        }
        #endregion

        #region 對話框操作
        private void OpenAddDialog()
        {
            CurrentProduct = new ProductDto { IsActive = true };
            _isNewProduct = true;
            DialogTitle = "新增產品";

            var parameters = new DialogParameters
            {
                { "CurrentProduct", CurrentProduct },
                { "IsNewProduct", true },
                { "Title", DialogTitle }
            };

            _dialogService.ShowDialog("AddOrEditProductDialog", parameters, OnDialogClosed);
        }

        private void OpenEditDialog(ProductDto product)
        {
            if (product == null) return;

            CurrentProduct = new ProductDto
            {
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Description = product.Description,
                IsActive = product.IsActive
            };

            _isNewProduct = false;
            DialogTitle = "編輯產品";

            var parameters = new DialogParameters
            {
                { "CurrentProduct", CurrentProduct },
                { "IsNewProduct", false },
                { "Title", DialogTitle }
            };

            _dialogService.ShowDialog("AddOrEditProductDialog", parameters, OnDialogClosed);
        }

        private void OnDialogClosed(IDialogResult result)
        {
            if (result.Result == ButtonResult.OK)
            {
                LoadProductsAsync(); // 重新載入列表
            }
        }
        #endregion

        #region 刪除產品
        private async Task DeleteProductAsync(ProductDto product)
        {
            if (product == null) return;

            var request = new ClientRequest
            {
                Url = $"Product/{product.ProductId}",
                Method = Method.Delete,
                ContentType = "application/json"
            };

            var response = await _httpRestClient.ExecuteAsync<object>(request);

            if (response.IsSuccess)
            {
                Products.Remove(product);
            }
        }
        #endregion
    }
}