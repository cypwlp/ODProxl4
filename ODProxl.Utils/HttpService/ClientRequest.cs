using RestSharp;

namespace ODProxl.Utils.HttpService;

/// <summary>
/// 请求对象
/// </summary>
public class ClientRequest
{
    /// <summary>
    /// 请求地址/api/xxx
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 请求方法(GET,POST,PUT,DELETE)
    /// </summary>
    public Method Method { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    public object? Parameters { get; set; }

    /// <summary>
    /// 请求数据类型，默认为application/json
    /// </summary>
    public string ContentType { get; set; } = "application/json";
}
