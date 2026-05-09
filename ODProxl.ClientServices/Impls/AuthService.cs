using ODProxl.ClientDtos;

namespace ODProxl.ClientServices.Impls;

public class AuthService : IAuthService
{
    public LoginRequestDto? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser != null;

    public event EventHandler? AuthStateChanged;

    public void SignIn(LoginRequestDto user)
    {
        CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut()
    {
        CurrentUser = null;
        AuthStateChanged?.Invoke(this, EventArgs.Empty);
    }
}
