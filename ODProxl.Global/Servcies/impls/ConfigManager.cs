// Global/Services/impls/ConfigManager.cs
using ODProxl.ClientDtos;
using ODProxl.Utils.HttpService;
using RestSharp;

namespace ODProxl.Global.Services.impls
{
    public class ConfigManager : IConfigManager, IDisposable
    {
        private readonly IHttpRestClient _httpRestClient;
        private readonly IEventAggregator _eventAggregator;
        private readonly object _lock = new();

        private Dictionary<string, string> _configValues = new(StringComparer.OrdinalIgnoreCase);
        private List<ConfigDto> _configs = new();

        public event Action? ConfigChanged;

        public ConfigManager(IHttpRestClient httpRestClient, IEventAggregator eventAggregator)
        {
            _httpRestClient = httpRestClient;
            _eventAggregator = eventAggregator;

            // 訂閱 SignalR 傳來的設定變更通知，自動從伺服器拉取最新設定
            _eventAggregator.GetEvent<PubSubEvent<string>>()
                .Subscribe(async _ => await RefreshAsync());
        }

        public IReadOnlyList<ConfigDto> AllConfigs
        {
            get
            {
                lock (_lock)
                    return _configs.AsReadOnly();
            }
        }

        public string? GetValue(string key)
        {
            lock (_lock)
                return _configValues.TryGetValue(key, out var value) ? value : null;
        }

        public void SetConfigs(List<ConfigDto> configs)
        {
            lock (_lock)
            {
                _configs = new List<ConfigDto>(configs);
                _configValues = configs.ToDictionary(
                    c => c.CgKey,
                    c => c.CgValue,
                    StringComparer.OrdinalIgnoreCase);
            }
            ConfigChanged?.Invoke();
        }

        public async Task RefreshAsync()
        {
            try
            {
                var request = new ClientRequest
                {
                    Url = "Config",
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

        public void Dispose() { }
    }
}