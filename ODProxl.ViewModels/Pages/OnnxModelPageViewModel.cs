using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ODProxl.ClientDtos;
using ODProxl.ClientServices;
using ODProxl.Utils.HttpService;
using RestSharp;

namespace ODProxl.ViewModels.Pages;

public class OnnxModelPageViewModel : BindableBase, INavigationAware
{
    #region INavigationAware implementation

    private INavigationAware _navigationAwareImplementation;

    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        await InitializeModellistAsync();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext) { }

    #endregion

    #region  字段與構造函數
    private readonly IAuthService _authService;
    private readonly IHttpRestClient _httpRestClient;
    private readonly IEventAggregator _eventAggregator;
    private readonly IDialogService _dialogService;
    private ObservableCollection<ModelDto> _userModels;
    private ModelDto _selectedModel;
    private bool _isLoading;
    public DelegateCommand<ModelDto> OpenSubClassPageCommand { get; private set; }

    public OnnxModelPageViewModel(
        IAuthService authService,
        IHttpRestClient httpRestClient,
        IEventAggregator eventAggregator,
        IDialogService dialogService
    )
    {
        _httpRestClient = httpRestClient;
        _authService = authService;
        _eventAggregator = eventAggregator;
        _dialogService = dialogService;
        OpenSubClassPageCommand = new DelegateCommand<ModelDto>(
            async (model) => await ShowClassDialogAsync(model)
        );
    }

    #endregion

    #region  屬性

    public ModelDto SelectedModel
    {
        get { return _selectedModel; }
        set { SetProperty(ref _selectedModel, value); }
    }
    public bool IsLoading
    {
        get { return _isLoading; }
        set { SetProperty(ref _isLoading, value); }
    }
    public ObservableCollection<ModelDto> UserModels
    {
        get => _userModels;
        set => SetProperty(ref _userModels, value);
    }

    #endregion

    #region  加載模型列表

    private async Task ShowClassDialogAsync(ModelDto model)
    {
        if (model == null)
            return;

        var parameters = new DialogParameters { { "ModelId", model.ModelId } };
        await _dialogService.ShowDialogAsync("ModelClassDialog", parameters);
    }

    private async Task InitializeModellistAsync()
    {
        var request = new ClientRequest
        {
            Url = "OnnxModel/getModel",
            Method = Method.Get,
            ContentType = "application/json",
        };

        var response = await _httpRestClient.ExecuteAsync<ObservableCollection<ModelDto>>(request);
        if (response.IsSuccess)
        {
            var list = response.Data;
            for (int i = 0; i < list.Count; i++)
                list[i].RowIndex = i + 1;
            UserModels = list;
        }
        IsLoading = false;
    }
    #endregion
}
