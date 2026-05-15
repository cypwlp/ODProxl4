namespace ODProxl.ClientServices
{
    public interface ISignalRService
    {
        Task StartAsync();
        Task StopAsync();
    }
}
