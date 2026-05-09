using System.Net;
using Newtonsoft.Json;
using RestSharp;

namespace ODProxl.Utils.HttpService;

public class HttpRestClient
{
    private readonly RestClient _client;
    private readonly string baseUrl = "https://localhost:44364/api/";

    public HttpRestClient(RestClient client)
    {
        _client = client;
    }

    /// <summary>
    /// 异步执行请求
    /// </summary>
    /// <typeparam name="T">响应数据类型</typeparam>
    /// <param name="clientRequest">请求信息</param>
    /// <returns>返回响应结果</returns>
    public async Task<ClientResponse<T>> ExecuteAsync<T>(ClientRequest clientRequest)
    {
        // 拼接完整请求地址（不再依赖 RestClient 的 BaseUrl）
        string fullUrl = baseUrl + clientRequest.Url;
        var request = new RestRequest(fullUrl, clientRequest.Method);

        request.AddHeader("Content-Type", clientRequest.ContentType);

        if (clientRequest.Parameters != null)
        {
            var jsonBody = JsonConvert.SerializeObject(clientRequest.Parameters);
            request.AddStringBody(jsonBody, DataFormat.Json);
        }

        var response = await _client.ExecuteAsync(request);

        if (response.StatusCode == HttpStatusCode.OK && !string.IsNullOrEmpty(response.Content))
        {
            var result = JsonConvert.DeserializeObject<ClientResponse<T>>(response.Content);
            return result ?? ClientResponse<T>.Error("响应反序列化失败");
        }
        else
        {
            // StatusCode 是非可空类型，直接转换为 int
            return ClientResponse<T>.Error("服务器繁忙，请稍后", (int)response.StatusCode);
        }
    }
}
