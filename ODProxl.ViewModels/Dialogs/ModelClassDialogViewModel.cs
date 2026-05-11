using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ODProxl.ClientDtos;
using ODProxl.Utils.HttpService;
using Prism.Commands;
using Prism.Mvvm;
using RestSharp;

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

    #endregion

    #region 字段 & 构造函数
    private int _modelId;
    private readonly IHttpRestClient _httpRestClient;
    private ObservableCollection<ModelClassDto> _baseClasses;
    private ObservableCollection<ModelClassDto> _subClasses;
    private ModelClassDto _selectedBaseClass;

    public ModelClassDialogViewModel(IHttpRestClient httpRestClient)
    {
        _httpRestClient = httpRestClient;
        SearchText = string.Empty;
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
                    SubClasses?.Clear();
            }
        }
    }
    public ObservableCollection<ModelClassDto> BaseClasses
    {
        get => _baseClasses;
        set => SetProperty(ref _baseClasses, value);
    }

    public ObservableCollection<ModelClassDto> SubClasses
    {
        get => _subClasses;
        set => SetProperty(ref _subClasses, value);
    }
    private string _searchText;
    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }
    public DelegateCommand SearchCommand { get; }
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
            BaseClasses = response.Data;
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
            SubClasses = response.Data;
        }
    }

    private void ExecuteSearch(string searchText) { }
    #endregion
}
