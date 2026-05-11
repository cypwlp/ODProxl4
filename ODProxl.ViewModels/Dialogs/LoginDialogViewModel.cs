using Avalonia.Threading;
using Material.Styles.Controls;
using Material.Styles.Models;
using ODProxl.ClientDtos;
using ODProxl.Utils.HttpService;
using RestSharp;

namespace ODProxl.ViewModels.Dialogs;

public class LoginDialogViewModel : BindableBase, IDialogAware
{
    #region IDialogAware 成員
    public bool CanCloseDialog()
    {
        return true;
    }

    public void OnDialogClosed() { }

    public void OnDialogOpened(IDialogParameters parameters) { }

    public DialogCloseListener RequestClose { get; private set; } = new();
    #endregion

    #region  字段
    private string _userName;
    private string _password;
    private IDialogService _dialogService;
    private LoginRequestDto _loginRequestDto;
    private readonly IHttpRestClient _httpClient;
    public DelegateCommand LoginCommand { get; }

    public LoginDialogViewModel(IDialogService dialogService, IHttpRestClient httpClient)
    {
        _dialogService = dialogService;
        _httpClient = httpClient;
        LoginCommand = new DelegateCommand(async () => await LoginAsync());
    }

    #endregion

    #region 屬性
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
    public LoginRequestDto LoginInfo
    {
        get => _loginRequestDto;
        set => SetProperty(ref _loginRequestDto, value);
    }
    #endregion

    #region 登錄方法

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(UserName) || string.IsNullOrWhiteSpace(Password))
            return;
        var request = new ClientRequest
        {
            Url = "Account/login",
            Method = Method.Post,
            ContentType = "application/json",
            Parameters = new AccountDto { Username = UserName, Password = Password },
        };
        var response = await _httpClient.ExecuteAsync<LoginRequestDto>(request);
        await Task.Delay(200);
        try
        {
            if (response.IsSuccess)
            {
                LoginInfo = response.Data;
                DialogParameters paras = new DialogParameters();
                paras.Add("LoginInfo", LoginInfo);
                RequestClose.Invoke(paras, ButtonResult.OK);
            }
            else
            {
                string content = "用户名或密码错误，请重试。";
                SnackbarModel snackbar = new SnackbarModel(content, TimeSpan.FromSeconds(5));
                SnackbarHost.Post(snackbar, "LoginPageSnackbarHost", DispatcherPriority.Normal);
            }
        }
        catch (Exception ex)
        {
            string content = $"登入過程中發生錯誤: {ex.Message}";
            SnackbarModel snackbar = new SnackbarModel(content, TimeSpan.FromSeconds(5));
            SnackbarHost.Post(snackbar, "LoginPageSnackbarHost", DispatcherPriority.Normal);
        }
    }

    #endregion
}
