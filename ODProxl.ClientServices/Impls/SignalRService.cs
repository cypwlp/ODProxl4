

using Microsoft.AspNetCore.SignalR.Client;

namespace ODProxl.ClientServices.Impls
{
    public class SignalRService : ISignalRService, IDisposable
    {
        private HubConnection _connection;
        private readonly IAuthService _authService;
        private readonly IEventAggregator _eventAggregator;

        public SignalRService(IAuthService authService, IEventAggregator eventAggregator)
        {
            _authService = authService;
            _eventAggregator = eventAggregator;
        }

        public async Task StartAsync()
        {
            _connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:44364/hubs/config", options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult(_authService.CurrentUser.Token);
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<string>("ConfigUpdated", message =>
            {
                _eventAggregator.GetEvent<PubSubEvent<string>>().Publish(message);
            });

            await _connection.StartAsync();
        }

        public async Task StopAsync()
        {
            if (_connection != null)
                await _connection.DisposeAsync();
        }

        public void Dispose()
        {
            _connection?.DisposeAsync().AsTask().Wait();
        }
    }
}

