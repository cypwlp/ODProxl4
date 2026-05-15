using ODProxl.ClientDtos;

namespace ODProxl.Global.Services
{
    public interface IConfigManager
    {
        string? GetValue(string key);
        event Action? ConfigChanged;
        void SetConfigs(List<ConfigDto> configs);
    }
}
