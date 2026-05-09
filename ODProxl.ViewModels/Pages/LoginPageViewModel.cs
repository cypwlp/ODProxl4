using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using Material.Styles.Controls;
using Material.Styles.Models;
using ODProxl.ClientDtos;
using ODProxl.ClientServices;
using ODProxl.Utils.HttpService;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using RestSharp;

namespace ODProxl.ViewModels.Pages;

public class LoginPageViewModel : BindableBase, INavigationAware
{
    private readonly IAuthService _authService;
    private readonly IRegionManager _regionManager;

    private string _userName = "";
    private string _password = "";
    private bool _isBusy;

    public LoginPageViewModel(IAuthService authService, IRegionManager regionManager)
    {
        _authService = authService;
        _regionManager = regionManager;
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
            var httpClient = new HttpRestClient(new RestClient());
            var request = new ClientRequest
            {
                Url = "Account/login",
                Method = Method.Post,
                ContentType = "application/json",
                Parameters = new AccountDto { Username = UserName, Password = Password },
            };

            var response = await httpClient.ExecuteAsync<LoginRequestDto>(request);

            if (response.IsSuccess && response.Data != null)
            {
                // 1. 將登錄信息寫入全局認證服務
                _authService.SignIn(response.Data);

                // 2. 導航到主頁
                _regionManager.RequestNavigate("MainRegion", "HomePage");
            }
            else
            {
                ShowSnackbar("用戶名或密碼錯誤，請重試");
            }
        }
        catch (Exception ex)
        {
            ShowSnackbar($"登錄過程中發生錯誤: {ex.Message}");
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

    #region INavigationAware
    public void OnNavigatedTo(NavigationContext navigationContext) { }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext) { }
    #endregion
}
