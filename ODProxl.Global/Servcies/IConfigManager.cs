using ODProxl.ClientDtos;

namespace ODProxl.Global.Services
{
    public interface IConfigManager
    {
        /// <summary>依 key 取得設定值 (即時讀取最新字典)</summary>
        string? GetValue(string key);

        /// <summary>所有設定的唯讀清單 (供 DataGrid 等直接綁定)</summary>
        IReadOnlyList<ConfigDto> AllConfigs { get; }

        /// <summary>設定變更時觸發 (用於即時更新 UI)</summary>
        event Action? ConfigChanged;

        /// <summary>外部寫入設定集合 (通常由登入或 API 回傳時呼叫)</summary>
        void SetConfigs(List<ConfigDto> configs);

        /// <summary>手動從伺服器重新拉取最新設定</summary>
        Task RefreshAsync();
    }
}
