using Avalonia.Threading;
using Material.Styles.Controls;
using Material.Styles.Models;
using ODProxl.ClientDtos;
using ODProxl.ClientServices;
using ODProxl.Utils.Events;
using ODProxl.Utils.HttpService;
using RestSharp;

namespace ODProxl.ViewModels.Pages;

public class LoginPageViewModel : BindableBase, INavigationAware
{
    private readonly IAuthService _authService;
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly IHttpRestClient _httpRestClient;
    private readonly ISignalRService _signalRService;
    private readonly Global.Services.IConfigManager _configManager;

    private string _userName = "";
    private string _password = "";
    private bool _isBusy;

    public LoginPageViewModel(
        IAuthService authService,
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        IHttpRestClient httpRestClient,
        ISignalRService signalRService,
        Global.Services.IConfigManager configManager)
    {
        _authService = authService;
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;
        _httpRestClient = httpRestClient;
        _signalRService = signalRService;
        _configManager = configManager;
        LoginCommand = new DelegateCommand(
            async () => await LoginAsync(),
            () => !IsBusy
        ).ObservesProperty(() => IsBusy);
    }

    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public DelegateCommand LoginCommand { get; }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
        {
            ShowSnackbar("請輸入用戶名和密碼");
            return;
        }

        IsBusy = true;
        try
        {
            var loginRequest = new ClientRequest
            {
                Url = "Account/login",
                Method = Method.Post,
                ContentType = "application/json",
                Parameters = new AccountDto { Username = UserName, Password = Password },
            };

            var loginResponse = await _httpRestClient.ExecuteAsync<LoginRequestDto>(loginRequest);

            if (loginResponse.IsSuccess && loginResponse.Data != null)
            {
                _authService.SignIn(loginResponse.Data);

                await _signalRService.StartAsync();

                var configRequest = new ClientRequest
                {
                    Url = "Config/getUserConfig",
                    Method = Method.Get,
                    ContentType = "application/json"
                };
                var configResponse = await _httpRestClient.ExecuteAsync<List<ConfigDto>>(configRequest);
                if (configResponse.IsSuccess && configResponse.Data != null)
                {
                    _configManager.SetConfigs(configResponse.Data);
                }

                _regionManager.RequestNavigate("MainRegion", "HomePage");
            }
            else
            {
                _eventAggregator
                    .GetEvent<PubSubEvent<NotificationMessage>>()
                    .Publish(new NotificationMessage("用戶名或密碼錯誤，請重試", NotificationType.Warning));
            }
        }
        catch (Exception ex)
        {
            _eventAggregator
                .GetEvent<PubSubEvent<NotificationMessage>>()
                .Publish(new NotificationMessage($"錯誤: {ex.Message}", NotificationType.Warning));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static void ShowSnackbar(string message)
    {
        var snackbar = new SnackbarModel(message, TimeSpan.FromSeconds(5));
        SnackbarHost.Post(snackbar, "LoginPageSnackbarHost", DispatcherPriority.Normal);
    }

    public void OnNavigatedTo(NavigationContext navigationContext) { }
    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}