using ODProxl.ClientDtos;
using ODProxl.ClientServices;
using ODProxl.Utils.HttpService;
using System.Collections.ObjectModel;

namespace ODProxl.ViewModels.Pages
{
    public class ProductGroupPageViewModel : BindableBase, INavigationAware
    {
        #region INavigationAware implementation

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            await InitializeProductGroupsAsync();
        }

        #endregion

        #region 字段與構造函數

        private readonly IAuthService _authService;
        private readonly IHttpRestClient _httpRestClient;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogService _dialogService;
        private ProductGroupDto _selectedProductGroup;
        private ObservableCollection<ProductGroupDto> _productGroups;

        public ProductGroupPageViewModel(IEventAggregator eventAggregator, IAuthService authService, IHttpRestClient httpRestClient, IDialogService dialogService)
        {
            _eventAggregator = eventAggregator;
            _authService = authService;
            _httpRestClient = httpRestClient;
            _dialogService = dialogService;
            _productGroups = new ObservableCollection<ProductGroupDto>();

            AddProductGroupCommand = new DelegateCommand(
                async () => await ShowReviseProductGroupDialogAsync(null)
            );
            EditProductGroupCommand = new DelegateCommand<ProductGroupDto?>(
                async (group) => await ShowReviseProductGroupDialogAsync(group)
            );
        }

        #endregion

        #region 屬性

        public DelegateCommand AddProductGroupCommand { get; }
        public DelegateCommand<ProductGroupDto?> EditProductGroupCommand { get; }

        public ProductGroupDto SelectedProductGroup
        {
            get => _selectedProductGroup;
            set => SetProperty(ref _selectedProductGroup, value);
        }

        public ObservableCollection<ProductGroupDto> ProductGroups
        {
            get => _productGroups;
            set => SetProperty(ref _productGroups, value);
        }

        #endregion

        #region 方法

        private async Task ShowReviseProductGroupDialogAsync(ProductGroupDto? group = null)
        {
            try
            {
                IDialogResult result;
                if (group == null)
                {
                    result = await _dialogService.ShowDialogAsync("ReviseProductGroupDialog");
                }
                else
                {
                    var parameters = new DialogParameters
                    {
                        { "GroupId", group.GroupId },
                        { "GroupName", group.GroupName },
                        { "IsActive", group.IsActive }
                    };
                    result = await _dialogService.ShowDialogAsync("ReviseProductGroupDialog", parameters);
                }

                if (result.Result != ButtonResult.OK || result.Parameters == null)
                    return;

                var groupName = result.Parameters.GetValue<string>("GroupName");
                var isActive = result.Parameters.GetValue<bool>("IsActive");

                if (string.IsNullOrWhiteSpace(groupName))
                    return;

                if (group == null)
                {
                    var createDto = new CreateProductGroupDto
                    {
                        GroupName = groupName,
                        IsActive = isActive
                    };
                    var request = new ClientRequest
                    {
                        Url = "ProductGroup",
                        Method = RestSharp.Method.Post,
                        Parameters = createDto,
                        ContentType = "application/json"
                    };
                    var response = await _httpRestClient.ExecuteAsync<ProductGroupDto>(request);
                    if (response.IsSuccess && response.Data != null)
                    {
                        ProductGroups.Add(response.Data);
                    }
                }
                else
                {
                    var updateDto = new UpdateProductGroupDto
                    {
                        GroupId = group.GroupId,
                        GroupName = groupName,
                        IsActive = isActive
                    };
                    var request = new ClientRequest
                    {
                        Url = $"ProductGroup/{group.GroupId}",
                        Method = RestSharp.Method.Put,
                        Parameters = updateDto,
                        ContentType = "application/json"
                    };
                    var response = await _httpRestClient.ExecuteAsync<ProductGroupDto>(request);
                    if (response.IsSuccess && response.Data != null)
                    {
                        var index = ProductGroups.IndexOf(group);
                        if (index >= 0)
                        {
                            ProductGroups[index] = response.Data;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProductGroup operation error: {ex}");
            }
        }

        private async Task InitializeProductGroupsAsync()
        {
            var request = new ClientRequest
            {
                Url = "ProductGroup",
                Method = RestSharp.Method.Get,
                ContentType = "application/json"
            };
            var response = await _httpRestClient.ExecuteAsync<List<ProductGroupDto>>(request);
            if (response.IsSuccess && response.Data != null)
            {
                ProductGroups = new ObservableCollection<ProductGroupDto>(response.Data);
            }
        }

        #endregion
    }
}