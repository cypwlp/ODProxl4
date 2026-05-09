using ODProxl.ClientDtos;

namespace ODProxl.ClientServices;

public interface IAuthService
{
    /// <summary>當前登錄用戶（未登錄時為 null）</summary>
    LoginRequestDto? CurrentUser { get; }

    /// <summary>是否已登錄</summary>
    bool IsAuthenticated { get; }

    /// <summary>登錄狀態變更事件</summary>
    event EventHandler? AuthStateChanged;

    /// <summary>設置當前用戶（登錄成功時調用）</summary>
    void SignIn(LoginRequestDto user);

    /// <summary>清除當前用戶（退出登錄時調用）</summary>
    void SignOut();
}
