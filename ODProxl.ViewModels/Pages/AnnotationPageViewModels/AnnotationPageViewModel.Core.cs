using Avalonia.Controls;
using Avalonia.Media.Imaging;
using ODProxl.ClientDtos;
using ODProxl.Global.Servcies;
using ODProxl.Global.Services;
using ODProxl.Utils.HttpService;
using SkiaSharp;
using System.Collections.ObjectModel;

namespace ODProxl.ViewModels.Pages.AnnotationPageViewModels;

public partial class AnnotationPageViewModel : BindableBase, INavigationAware, IDisposable
{
    private readonly IDialogService _dialogService;
    private readonly HttpClient _httpClient;
    private readonly IHttpRestClient _httpRestClient;
    private readonly IConfigManager _configManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly IFileManager _fileManager;

    private string source_pdf_base_url;
    private string credentials_l;
    private string credentials_p;
    private string annotation_image_base_url;
    //private string annotation_label_base_url;

    private Image? _imageControl;
    private Canvas? _canvas;
    private SKBitmap? _currentSkBitmap;
    private TopLevel? _topLevel;

    private string _currentModelFolder = "default";
    private bool _isPolygonMode;
    private double _imagePixelWidth;
    private double _imagePixelHeight;
    private int _currentImageIndex = -1;
    private double _zoomLevel = 1.0;
    private int _polygonPointCount;
    private Bitmap? _currentImage;
    private string _statusText = "準備就緒";
    private string _mousePositionText = "X: --- Y: ---";
    private ObservableCollection<TinyRuleClassDto> _ruleCLass;
    private ObservableCollection<string> _imageFiles = new();

    public event Action? RequestResetZoom;

    public string CurrentModelFolder
    {
        get => _currentModelFolder;
        set => SetProperty(ref _currentModelFolder, value);
    }
    public bool IsPolygonMode
    {
        get => _isPolygonMode;
        set
        {
            if (SetProperty(ref _isPolygonMode, value))
                RaisePropertyChanged(nameof(NotIsPolygonMode));
        }
    }
    public bool NotIsPolygonMode => !IsPolygonMode;
    public double ImagePixelWidth { get => _imagePixelWidth; set => SetProperty(ref _imagePixelWidth, value); }
    public double ImagePixelHeight { get => _imagePixelHeight; set => SetProperty(ref _imagePixelHeight, value); }
    public int CurrentImageIndex { get => _currentImageIndex; set => SetProperty(ref _currentImageIndex, value); }
    public double ZoomLevel { get => _zoomLevel; set => SetProperty(ref _zoomLevel, value); }
    public int PolygonPointCount { get => _polygonPointCount; set => SetProperty(ref _polygonPointCount, value); }
    public Bitmap? CurrentImage { get => _currentImage; set => SetProperty(ref _currentImage, value); }
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }
    public string MousePositionText { get => _mousePositionText; set => SetProperty(ref _mousePositionText, value); }
    public string ModeText => IsPolygonMode ? "多邊形模式" : "矩形模式";

    public ObservableCollection<string> ExpectedImagePaths { get; } = new();
    public ObservableCollection<TinyRuleClassDto> RuleClass
    {
        get => _ruleCLass;
        set => SetProperty(ref _ruleCLass, value);
    }
    public ObservableCollection<string> ImagePaths { get; } = new();
    public ObservableCollection<LocalClass> Classes { get; } = new();
    public ObservableCollection<Annotation> Annotations { get; } = new();
    public LocalClass? SelectedClass { get; set; }
    public ObservableCollection<string> ImageFiles
    {
        get => _imageFiles;
        set => SetProperty(ref _imageFiles, value);
    }

    public AnnotationPageViewModel(IDialogService dialogService, IHttpRestClient httpRestClient, HttpClient httpClient, IConfigManager configManager, IEventAggregator eventAggregator, IFileManager fileManager)
    {
        _dialogService = dialogService;
        _httpClient = httpClient;
        _httpRestClient = httpRestClient;
        _configManager = configManager;
        _eventAggregator = eventAggregator;
        _fileManager = fileManager;

        _configManager.ConfigChanged += () =>
        {

            source_pdf_base_url = _configManager.GetValue("source_pdf_base_path") ?? string.Empty;
            credentials_l = _configManager.GetValue("credentials_l") ?? string.Empty;
            credentials_p = _configManager.GetValue("credentials_p") ?? string.Empty;
            annotation_image_base_url = _configManager.GetValue("annotation_image_base_url") ?? string.Empty;
            //annotation_label_base_url = _configManager.GetValue("annotation_image_base_url") ?? string.Empty;
        };
        source_pdf_base_url = _configManager.GetValue("source_pdf_base_path") ?? string.Empty;
        credentials_l = _configManager.GetValue("credentials_l") ?? string.Empty;
        credentials_p = _configManager.GetValue("credentials_p") ?? string.Empty;
        annotation_image_base_url = _configManager.GetValue("annotation_image_base_url") ?? string.Empty;
        //annotation_label_base_url = _configManager.GetValue("annotation_image_base_url") ?? string.Empty;

        InitializeCommands();
    }



    public void SetTopLevel(TopLevel? topLevel) => _topLevel = topLevel;
    public void SetControls(Image? image, Canvas? canvas)
    {
        _imageControl = image;
        _canvas = canvas;
    }
    public void RequestRedraw() => RedrawAllAnnotations();

    public void Dispose()
    {
        _currentSkBitmap?.Dispose();
    }
}