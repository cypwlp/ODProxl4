using System.Net;

namespace ODProxl.Utils.HttpService;

public class ClientResponse<T>
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public int? StatusCode { get; set; }
    public T? Data { get; set; }

    public ClientResponse() { }

    public ClientResponse(bool isSuccess, string message, int statusCode, T? data = default)
    {
        IsSuccess = isSuccess;
        Message = message;
        StatusCode = statusCode;
        Data = data;
    }

    // 成功（无数据）
    public static ClientResponse<T> Success(
        string message = "OK",
        int statusCode = (int)HttpStatusCode.OK
    )
    {
        return new ClientResponse<T>(true, message, statusCode);
    }

    // 成功（带数据）- 参数类型改为 T
    public static ClientResponse<T> Success(
        T data,
        string message = "OK",
        int statusCode = (int)HttpStatusCode.OK
    )
    {
        return new ClientResponse<T>(true, message, statusCode, data);
    }

    // 错误
    public static ClientResponse<T> Error(
        string message,
        int statusCode = (int)HttpStatusCode.BadRequest
    )
    {
        return new ClientResponse<T>(false, message, statusCode);
    }

    // 服务器错误
    public static ClientResponse<T> InternalError(
        string message = "Server_Error",
        int statusCode = (int)HttpStatusCode.InternalServerError
    )
    {
        return new ClientResponse<T>(false, message, statusCode);
    }
}
