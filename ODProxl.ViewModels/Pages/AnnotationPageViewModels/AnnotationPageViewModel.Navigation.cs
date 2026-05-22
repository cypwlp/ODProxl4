using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ODProxl.ClientDtos;
using ODProxl.Utils.HttpService;
using RestSharp;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace ODProxl.ViewModels.Pages.AnnotationPageViewModels;

public partial class AnnotationPageViewModel
{
    private int _nextClassId = 1;

    private async Task InitializeRuleClassAsync()
    {
        var request = new ClientRequest
        {
            Url = "RuleClass/none-parent-class",
            Method = Method.Get,
            ContentType = "application/json"
        };
        var response = await _httpRestClient.ExecuteAsync<List<TinyRuleClassDto>>(request);
        if (response.IsSuccess && response.Data != null)
        {
            RuleClass = new ObservableCollection<TinyRuleClassDto>(response.Data);
        }
    }

    private async Task OpenImagesAsync()
    {
        var parameters = new DialogParameters
        {
            { "Title", "選擇圖片文件" },
            { "AllowMultiple", true },
            { "Filter", "圖片文件|*.jpg;*.jpeg;*.png;*.bmp" }
        };
        var result = await _dialogService.ShowDialogAsync("OpenFileDialog", parameters);
        if (result.Result != ButtonResult.OK) return;
        var files = result.Parameters.GetValue<string[]>("Files");
        if (files == null || files.Length == 0) return;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ImagePaths.Clear();
            foreach (var file in files)
                ImagePaths.Add(file);
            Annotations.Clear();
            CurrentImageIndex = 0;
            _ = LoadImageAsync(0);
        });
    }

    public async Task LoadImageAsync(int index)
    {
        if (index < 0 || index >= ExpectedImagePaths.Count) return;
        CurrentImageIndex = index;
        var url = ExpectedImagePaths[index];
        try
        {
            using var httpResponse = await _httpClient.GetAsync(url);
            httpResponse.EnsureSuccessStatusCode();
            var stream = await httpResponse.Content.ReadAsStreamAsync();
            CurrentImage = new Bitmap(stream);
            _currentSkBitmap?.Dispose();
            _currentSkBitmap = SKBitmap.Decode(stream);
            ImagePixelWidth = CurrentImage.PixelSize.Width;
            ImagePixelHeight = CurrentImage.PixelSize.Height;
            Annotations.Clear();
            _currentPolygonPoints.Clear();
            PolygonPointCount = 0;
            _isDragging = false;
            _tempMovePoint = null;
            RedrawAllAnnotations();
            StatusText = $"已載入第 {index + 1} 張圖片（共 {ExpectedImagePaths.Count} 張）";
        }
        catch (Exception ex)
        {
            StatusText = $"載入失敗: {ex.Message}";
        }
    }

    private async Task AddNewClassAsync()
    {
        var newName = await ShowInputDialogAsync("新增類別", "請輸入類別名稱", "");
        if (string.IsNullOrWhiteSpace(newName)) return;
        newName = newName.Trim();
        if (Classes.Any(c => c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
        {
            StatusText = "⚠️ 此類別名稱已存在";
            return;
        }
        var newClass = new LocalClass { Id = _nextClassId++, Name = newName };
        Classes.Add(newClass);
        SelectedClass ??= newClass;
        StatusText = $"✅ 已添加類別：「{newName}」";
    }

    private async Task<string> ShowInputDialogAsync(string title, string message, string defaultText)
    {
        var parameters = new DialogParameters
        {
            { "Title", title },
            { "Message", message },
            { "DefaultText", defaultText }
        };
        var result = await _dialogService.ShowDialogAsync("InputDialog", parameters);
        return result.Result == ButtonResult.OK ? result.Parameters.GetValue<string>("Result") ?? "" : "";
    }

    public void ToRuleClassPage()
    {
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
    public async void OnNavigatedTo(NavigationContext navigationContext)
    {
        await InitializeRuleClassAsync();
    }
}