using ODProxl.ClientDtos;
using RestSharp;


namespace ODProxl.Global.Services.impls
{
    public class ConfigManager : IConfigManager, IDisposable
    {
        private readonly Utils.HttpService.IHttpRestClient _httpRestClient;
        private readonly IEventAggregator _eventAggregator;
        private Dictionary<string, string> _configs = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        public event Action? ConfigChanged;

        public ConfigManager(Utils.HttpService.IHttpRestClient httpRestClient, IEventAggregator eventAggregator)
        {
            _httpRestClient = httpRestClient;
            _eventAggregator = eventAggregator;

            _eventAggregator.GetEvent<PubSubEvent<List<ConfigDto>>>()
                .Subscribe(configs => SetConfigs(configs));

            _eventAggregator.GetEvent<PubSubEvent<string>>()
                .Subscribe(async _ => await RefreshAsync());
        }

        public string? GetValue(string key)
        {
            lock (_lock)
            {
                return _configs.TryGetValue(key, out var value) ? value : null;
            }
        }

        public void SetConfigs(List<ConfigDto> configs)
        {
            lock (_lock)
            {
                _configs = configs.ToDictionary(c => c.CgKey, c => c.CgValue, StringComparer.OrdinalIgnoreCase);
            }
            ConfigChanged?.Invoke();
        }

        public async Task RefreshAsync()
        {
            try
            {
                var request = new Utils.HttpService.ClientRequest
                {
                    Url = "Config/getUserConfig",
                    Method = Method.Get,
                    ContentType = "application/json"
                };
                var response = await _httpRestClient.ExecuteAsync<List<ConfigDto>>(request);
                if (response.IsSuccess && response.Data != null)
                {
                    SetConfigs(response.Data);
                }
            }
            catch
            {
            }
        }

        public void Dispose()
        {
        }
    }
}
