namespace ODProxl.Utils.HttpService;

public interface IHttpRestClient
{
    Task<ClientResponse<T>> ExecuteAsync<T>(ClientRequest request);
}
