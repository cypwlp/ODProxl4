using ODProxl.ClientDtos;
using ODProxl.Utils.HttpService;
using RestSharp;
using System.Collections.ObjectModel;

namespace ODProxl.ViewModels.Dialogs;

public class ModelClassDialogViewModel : BindableBase, IDialogAware
{
    #region IDialogAware implementation
    public bool CanCloseDialog() => true;

    public void OnDialogClosed() { }

    public async void OnDialogOpened(IDialogParameters parameters)
    {
        if (parameters.ContainsKey("ModelId"))
            ModelId = parameters.GetValue<int>("ModelId");
        await InitializeBaseClassesAsync(ModelId);
    }

    public DialogCloseListener RequestClose { get; }

    public string Title => "模型類別管理";
    #endregion

    #region 字段 & 构造函数
    private int _modelId;
    private readonly IHttpRestClient _httpRestClient;

    // 完整原始資料
    private ObservableCollection<ModelClassDto> _originalBaseClasses;
    private ObservableCollection<ModelClassDto> _originalSubClasses;

    // 顯示用過濾集合
    private ObservableCollection<ModelClassDto> _filteredBaseClasses;
    private ObservableCollection<ModelClassDto> _filteredSubClasses;

    private ModelClassDto _selectedBaseClass;
    private string _searchText;

    public ModelClassDialogViewModel(IHttpRestClient httpRestClient)
    {
        _httpRestClient = httpRestClient;
        SearchText = string.Empty;

        // 搜尋命令（點擊按鈕執行，也可在 SearchText setter 中即時觸發）
        SearchCommand = new DelegateCommand<string>(ExecuteSearch);

        // 初始化集合，避免 null
        FilteredBaseClasses = new ObservableCollection<ModelClassDto>();
        FilteredSubClasses = new ObservableCollection<ModelClassDto>();
    }
    #endregion

    #region 属性
    public int ModelId
    {
        get => _modelId;
        set => SetProperty(ref _modelId, value);
    }

    public ModelClassDto SelectedBaseClass
    {
        get => _selectedBaseClass;
        set
        {
            if (SetProperty(ref _selectedBaseClass, value))
            {
                if (value != null)
                    _ = InitializeSubClassesAsync(value.ClassId);
                else
                {
                    _originalSubClasses = null;
                    FilteredSubClasses = new ObservableCollection<ModelClassDto>();
                }
            }
        }
    }

    public ObservableCollection<ModelClassDto> FilteredBaseClasses
    {
        get => _filteredBaseClasses;
        set => SetProperty(ref _filteredBaseClasses, value);
    }

    public ObservableCollection<ModelClassDto> FilteredSubClasses
    {
        get => _filteredSubClasses;
        set => SetProperty(ref _filteredSubClasses, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                // 即時過濾（可選）
                ApplyFilterToBaseClasses();
                // 若子類別也有搜尋需求，可一併呼叫
                ApplyFilterToSubClasses();
            }
        }
    }

    public DelegateCommand<string> SearchCommand { get; }
    #endregion

    #region 方法

    private async Task InitializeBaseClassesAsync(int modelId)
    {
        var request = new ClientRequest
        {
            Url = $"ModelClass/getBaseClasses/{modelId}",
            Method = Method.Get,
            ContentType = "application/json",
        };
        var response = await _httpRestClient.ExecuteAsync<ObservableCollection<ModelClassDto>>(
            request
        );
        if (response.IsSuccess)
        {
            _originalBaseClasses = response.Data;
            ApplyFilterToBaseClasses();
        }
    }

    private async Task InitializeSubClassesAsync(int classId)
    {
        var request = new ClientRequest
        {
            Url = $"ModelClass/getSubClasses/{classId}",
            Method = Method.Get,
            ContentType = "application/json",
        };
        var response = await _httpRestClient.ExecuteAsync<ObservableCollection<ModelClassDto>>(
            request
        );
        if (response.IsSuccess)
        {
            _originalSubClasses = response.Data;
            ApplyFilterToSubClasses();
        }
    }

    private void ExecuteSearch(string _)
    {
        // 按鈕點擊時手動觸發過濾（與即時過濾重複，可依需求保留或移除）
        ApplyFilterToBaseClasses();
        ApplyFilterToSubClasses();
    }

    private void ApplyFilterToBaseClasses()
    {
        if (_originalBaseClasses == null)
            return;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredBaseClasses = new ObservableCollection<ModelClassDto>(_originalBaseClasses);
        }
        else
        {
            var filtered = _originalBaseClasses
                .Where(x => x.ClassName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            FilteredBaseClasses = new ObservableCollection<ModelClassDto>(filtered);
        }
    }

    private void ApplyFilterToSubClasses()
    {
        if (_originalSubClasses == null)
            return;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredSubClasses = new ObservableCollection<ModelClassDto>(_originalSubClasses);
        }
        else
        {
            var filtered = _originalSubClasses
                .Where(x => x.ClassName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            FilteredSubClasses = new ObservableCollection<ModelClassDto>(filtered);
        }
    }
    #endregion
}
