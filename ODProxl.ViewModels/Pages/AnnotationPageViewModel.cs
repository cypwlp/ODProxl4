using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.ML.OnnxRuntime;
using ODProxl.ClientDtos;
using ODProxl.ClientServices.Impls;
using ODProxl.Global.Services;
using ODProxl.Utils.HttpService;
using RestSharp;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;

namespace ODProxl.ViewModels.Pages
{
    public class AnnotationPageViewModel : BindableBase, INavigationAware, IDisposable
    {
        #region 服務注入
        private readonly IDialogService _dialogService;
        private readonly HttpClient _httpClient;
        private readonly IHttpRestClient _httpRestClient;
        private readonly IConfigManager _configManager;
        private readonly IEventAggregator _eventAggregator;
        private string _imagesBaseUrl;
        private string _labelsBaseUrl;
        #endregion

        #region 繪圖狀態（欄位）
        private Point _startPoint;
        private bool _isDragging;
        private Point _currentRectEnd;
        private List<Point> _currentPolygonPoints = new();
        private Point? _tempMovePoint;
        #endregion

        #region 圖像控制相關
        private Image? _imageControl;
        private Canvas? _canvas;
        private SKBitmap? _currentSkBitmap;
        #endregion

        #region 標註元素映射（增量重繪）
        private readonly Dictionary<Annotation, Shape> _annotationElements = new();
        private Shape? _tempShape;
        #endregion

        #region 公開事件
        public event Action? RequestResetZoom;
        #endregion

        #region 屬性（Bindable）

        private string _currentModelFolder = "default";
        public string CurrentModelFolder
        {
            get => _currentModelFolder;
            set => SetProperty(ref _currentModelFolder, value);
        }
        private bool _isPolygonMode;
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

        private double _imagePixelWidth;
        public double ImagePixelWidth { get => _imagePixelWidth; set => SetProperty(ref _imagePixelWidth, value); }

        private double _imagePixelHeight;
        public double ImagePixelHeight { get => _imagePixelHeight; set => SetProperty(ref _imagePixelHeight, value); }

        private int _currentImageIndex = -1;
        public int CurrentImageIndex { get => _currentImageIndex; set => SetProperty(ref _currentImageIndex, value); }

        private double _zoomLevel = 1.0;
        public double ZoomLevel { get => _zoomLevel; set => SetProperty(ref _zoomLevel, value); }

        private int _polygonPointCount;
        public int PolygonPointCount { get => _polygonPointCount; set => SetProperty(ref _polygonPointCount, value); }

        private Bitmap? _currentImage;
        public Bitmap? CurrentImage { get => _currentImage; set => SetProperty(ref _currentImage, value); }

        private string _statusText = "準備就緒";
        public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

        private string _mousePositionText = "X: --- Y: ---";
        public string MousePositionText { get => _mousePositionText; set => SetProperty(ref _mousePositionText, value); }

        public string ModeText => IsPolygonMode ? "多邊形模式" : "矩形模式";

        public ObservableCollection<string> ExpectedImagePaths { get; } = new();

        private ObservableCollection<TinyRuleClassDto> _ruleCLass;
        public ObservableCollection<TinyRuleClassDto> RuleClass
        {
            get => _ruleCLass;
            set => SetProperty(ref _ruleCLass, value);
        }
        #endregion

        #region 集合與選取項目
        public ObservableCollection<string> ImagePaths { get; } = new();
        public ObservableCollection<LocalClass> Classes { get; } = new();
        public ObservableCollection<Annotation> Annotations { get; } = new();
        public LocalClass? SelectedClass { get; set; }
        #endregion

        #region 命令
        public AsyncDelegateCommand OpenImagesCommand { get; }
        public DelegateCommand SetRectModeCommand { get; }
        public DelegateCommand SetPolygonModeCommand { get; }
        public DelegateCommand ResetZoomCommand { get; }
        public DelegateCommand CancelPolygonCommand { get; }
        public AsyncDelegateCommand PrevImageCommand { get; }
        public AsyncDelegateCommand NextImageCommand { get; }
        public DelegateCommand<Annotation> DeleteAnnotationCommand { get; }
        public AsyncDelegateCommand AutoAnnotateCommand { get; }
        public DelegateCommand AddNewClassCommand { get; }

        public DelegateCommand OpenFileCommand { get; set; }
        #endregion

        #region 建構子
        public AnnotationPageViewModel(IDialogService dialogService, IHttpRestClient httpRestClient, HttpClient httpClient, IConfigManager configManager, IEventAggregator eventAggregator)
        {
            _dialogService = dialogService;
            _httpClient = httpClient;
            _httpRestClient = httpRestClient;
            _configManager = configManager;
            _eventAggregator = eventAggregator;
            _configManager.ConfigChanged += () =>
            {
                _imagesBaseUrl = _configManager.GetValue("annotation_image_base_url") ?? string.Empty;
                _labelsBaseUrl = _configManager.GetValue("source_pdf_base_path") ?? string.Empty;
            };
            // 初始化立即读取
            _imagesBaseUrl = _configManager.GetValue("annotation_image_base_url") ?? string.Empty;
            _labelsBaseUrl = _configManager.GetValue("source_pdf_base_path") ?? string.Empty;
            OpenImagesCommand = new AsyncDelegateCommand(OpenImagesAsync);
            SetRectModeCommand = new DelegateCommand(() => IsPolygonMode = false);
            SetPolygonModeCommand = new DelegateCommand(() => IsPolygonMode = true);
            ResetZoomCommand = new DelegateCommand(() => RequestResetZoom?.Invoke());
            CancelPolygonCommand = new DelegateCommand(CancelCurrentPolygon);
            PrevImageCommand = new AsyncDelegateCommand(async () =>
            {
                if (CurrentImageIndex > 0) await LoadImageAsync(CurrentImageIndex - 1);
            });
            NextImageCommand = new AsyncDelegateCommand(async () =>
            {
                if (CurrentImageIndex < ImagePaths.Count - 1) await LoadImageAsync(CurrentImageIndex + 1);
            });
            DeleteAnnotationCommand = new DelegateCommand<Annotation>(ann =>
            {
                if (ann != null && Annotations.Contains(ann))
                {
                    Annotations.Remove(ann);
                    RedrawAllAnnotations();
                }
            });
            AutoAnnotateCommand = new AsyncDelegateCommand(RunAutoAnnotationAsync);
            AddNewClassCommand = new DelegateCommand(async () => await AddNewClassAsync());
        }
        #endregion

        #region 初始化類別列表
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
        #endregion

        #region 文件操作命令

        private async Task EnsureLabelFolderExistsAsync()
        {
            string folderUrl = $"{_labelsBaseUrl}{CurrentModelFolder}/";
            var headRequest = new HttpRequestMessage(HttpMethod.Head, folderUrl);
            var headResponse = await _httpClient.SendAsync(headRequest);
            if (headResponse.IsSuccessStatusCode)
                return;
            var mkcolMethod = new HttpMethod("MKCOL");
            var mkcolRequest = new HttpRequestMessage(mkcolMethod, folderUrl);
            var mkcolResponse = await _httpClient.SendAsync(mkcolRequest);

            if (mkcolResponse.IsSuccessStatusCode)
            {
                StatusText = $"已建立标注文件夹：{CurrentModelFolder}";
                return;
            }
            var placeholderContent = new StringContent("[]", Encoding.UTF8, "application/json");
            var putResponse = await _httpClient.PutAsync(folderUrl + ".placeholder", placeholderContent);

            if (putResponse.IsSuccessStatusCode)
            {
                StatusText = $"已通过占位文件建立文件夹：{CurrentModelFolder}";
            }
            else
            {
                StatusText = $"警告：无法创建文件夹 {CurrentModelFolder}，HTTP {putResponse.StatusCode}。后续保存可能失败。";
            }
        }
        public async Task ProcessPdfFolderAsync(string folderPath)
        {
            var pdfFiles = Directory.GetFiles(folderPath, "*.pdf", SearchOption.TopDirectoryOnly);
            await ProcessPdfFilesAsync(pdfFiles);
        }

        public async Task ProcessPdfFileAsync(string filePath)
        {
            await ProcessPdfFilesAsync(new[] { filePath });
        }

        private async Task ProcessPdfFilesAsync(IEnumerable<string> pdfPaths)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ExpectedImagePaths.Clear();
                Annotations.Clear();
                CurrentImage = null;
                CurrentImageIndex = -1;
                StatusText = "正在處理 PDF 文件...";
            });

            await EnsureLabelFolderExistsAsync();

            int totalProcessed = 0;
            foreach (var pdfPath in pdfPaths)
            {
                var pdfFileName = System.IO.Path.GetFileNameWithoutExtension(pdfPath);
                int pageCount = 0;
                try
                {
                    using var docReader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(2480, 3508));
                    pageCount = docReader.GetPageCount();
                }
                catch (Exception ex)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => StatusText = $"無法讀取 PDF：{System.IO.Path.GetFileName(pdfPath)} - {ex.Message}");
                    continue;
                }

                for (int page = 0; page < pageCount; page++)
                {
                    string imageName = $"{pdfFileName}_p{(page + 1):D3}.png";
                    string imageHttpUrl = _imagesBaseUrl + imageName;
                    bool existsOnServer = await ImageExistsOnServerAsync(imageHttpUrl);

                    if (existsOnServer)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            ExpectedImagePaths.Add(imageHttpUrl);
                            totalProcessed++;
                            StatusText = $"已從伺服器載入圖片 {imageName} ({totalProcessed} 張)";
                            if (ExpectedImagePaths.Count == 1)
                            {
                                CurrentImageIndex = 0;
                                _ = LoadImageAsync(0);
                            }
                        });
                    }
                    else
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => StatusText = $"正在轉換 300 DPI 圖片: {imageName}");
                        byte[] pngBytes = await RenderPdfPageToPngAsync(pdfPath, page);
                        await UploadImageToServerAsync(imageHttpUrl, pngBytes);
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            ExpectedImagePaths.Add(imageHttpUrl);
                            totalProcessed++;
                            StatusText = $"已轉換並上傳圖片 {imageName} ({totalProcessed} 張)";
                            if (ExpectedImagePaths.Count == 1)
                            {
                                CurrentImageIndex = 0;
                                _ = LoadImageAsync(0);
                            }
                        });
                    }
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() => StatusText = $"處理完成，共 {totalProcessed} 張圖片（已自動同步至伺服器）");
        }

        private async Task<bool> ImageExistsOnServerAsync(string imageHttpUrl)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, imageHttpUrl);
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<byte[]> RenderPdfPageToPngAsync(string pdfPath, int pageIndex)
        {
            return await Task.Run(() =>
            {
                using var docReader = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(2480, 3508));
                using var pageReader = docReader.GetPageReader(pageIndex);
                var rawBytes = pageReader.GetImage();
                int width = pageReader.GetPageWidth();
                int height = pageReader.GetPageHeight();
                var info = new SKImageInfo(width, height, SKColorType.Bgra8888);
                using var skData = SKData.CreateCopy(rawBytes);
                using var skImage = SKImage.FromPixels(info, skData);
                using var encoded = skImage.Encode(SKEncodedImageFormat.Png, 100);
                using var ms = new MemoryStream();
                encoded.SaveTo(ms);
                return ms.ToArray();
            });
        }

        private async Task UploadImageToServerAsync(string imageHttpUrl, byte[] pngBytes)
        {
            var content = new ByteArrayContent(pngBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            var response = await _httpClient.PutAsync(imageHttpUrl, content);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"上傳圖片失敗: HTTP {(int)response.StatusCode}");
        }

        #endregion

        #region 增量重繪方法
        public void RequestRedraw() => RedrawAllAnnotations();

        public void RedrawAllAnnotations()
        {
            if (_canvas == null) return;
            var currentAnnotations = new HashSet<Annotation>(Annotations);

            foreach (var kv in _annotationElements.ToList())
            {
                if (!currentAnnotations.Contains(kv.Key))
                {
                    _canvas.Children.Remove(kv.Value);
                    _annotationElements.Remove(kv.Key);
                }
            }

            foreach (var ann in Annotations)
            {
                if (!_annotationElements.TryGetValue(ann, out var shape))
                {
                    shape = CreateShapeForAnnotation(ann);
                    _annotationElements[ann] = shape;
                    _canvas.Children.Add(shape);
                }
                else
                {
                    UpdateShape(shape, ann);
                }
            }

            if (_tempShape != null)
                _canvas.Children.Remove(_tempShape);
            _tempShape = CreateTempShape();
            if (_tempShape != null)
                _canvas.Children.Add(_tempShape);
        }

        private Shape CreateShapeForAnnotation(Annotation ann)
        {
            if (ann.IsPolygon && ann.Points.Count >= 3)
            {
                return new Polygon
                {
                    Points = new Points(ann.Points),
                    Stroke = Brushes.Red,
                    StrokeThickness = 3.5,
                    Fill = new SolidColorBrush(Colors.Red, 0.06),
                    StrokeJoin = PenLineJoin.Round
                };
            }
            if (!ann.IsPolygon && ann.Points.Count == 2)
            {
                var p1 = ann.Points[0];
                var p2 = ann.Points[1];
                var rect = new Rectangle
                {
                    Width = Math.Abs(p2.X - p1.X),
                    Height = Math.Abs(p2.Y - p1.Y),
                    Stroke = Brushes.Blue,
                    StrokeThickness = 3.5,
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(rect, Math.Min(p1.X, p2.X));
                Canvas.SetTop(rect, Math.Min(p1.Y, p2.Y));
                return rect;
            }
            return new Rectangle();
        }

        private void UpdateShape(Shape shape, Annotation ann)
        {
            if (shape is Polygon poly && ann.IsPolygon)
                poly.Points = new Points(ann.Points);
        }

        private Shape? CreateTempShape()
        {
            if (IsPolygonMode && _currentPolygonPoints.Count > 0)
            {
                var points = new List<Point>(_currentPolygonPoints);
                if (_tempMovePoint.HasValue) points.Add(_tempMovePoint.Value);
                if (points.Count >= 2)
                    return new Polygon
                    {
                        Points = new Points(points),
                        Stroke = Brushes.Orange,
                        StrokeThickness = 3.5,
                        Fill = new SolidColorBrush(Colors.Orange, 0.08),
                        StrokeDashArray = new AvaloniaList<double> { 5, 3 }
                    };
            }

            if (!IsPolygonMode && _isDragging && _currentRectEnd != default)
            {
                var p1 = _startPoint;
                var p2 = _currentRectEnd;
                var rect = new Rectangle
                {
                    Width = Math.Abs(p2.X - p1.X),
                    Height = Math.Abs(p2.Y - p1.Y),
                    Stroke = Brushes.Lime,
                    StrokeThickness = 3.5,
                    StrokeDashArray = new AvaloniaList<double> { 4, 2 },
                    Fill = Brushes.Transparent
                };
                Canvas.SetLeft(rect, Math.Min(p1.X, p2.X));
                Canvas.SetTop(rect, Math.Min(p1.Y, p2.Y));
                return rect;
            }
            return null;
        }
        #endregion

        #region 滑鼠 / 觸控事件處理
        public void OnPointerPressed(Point imagePixelPos)
        {
            if (!IsPolygonMode)
            {
                _startPoint = _currentRectEnd = imagePixelPos;
                _isDragging = true;
            }
            else
            {
                _currentPolygonPoints.Add(imagePixelPos);
                PolygonPointCount = _currentPolygonPoints.Count;
                _tempMovePoint = imagePixelPos;
            }
            RedrawAllAnnotations();
        }

        public void OnPointerPressedRight(Point imagePixelPos)
        {
            if (IsPolygonMode)
            {
                if (_currentPolygonPoints.Count >= 3)
                    FinishCurrentPolygon();
                else if (_currentPolygonPoints.Count > 0)
                {
                    _currentPolygonPoints.RemoveAt(_currentPolygonPoints.Count - 1);
                    PolygonPointCount = _currentPolygonPoints.Count;
                }
            }
            else
            {
                _isDragging = false;
            }
            RedrawAllAnnotations();
        }

        public void OnPointerMoved(Point imagePixelPos)
        {
            MousePositionText = $"X: {imagePixelPos.X:F1} Y: {imagePixelPos.Y:F1}";
            if (!IsPolygonMode && _isDragging)
                _currentRectEnd = imagePixelPos;
            else if (IsPolygonMode && _currentPolygonPoints.Count > 0)
                _tempMovePoint = imagePixelPos;
        }

        public void OnPointerReleased(Point imagePixelPos)
        {
            if (!IsPolygonMode && _isDragging)
            {
                _currentRectEnd = imagePixelPos;
                AddRectangleAnnotation();
                _isDragging = false;
            }
            RedrawAllAnnotations();
        }
        #endregion

        #region 標註建立與取消
        private void AddRectangleAnnotation()
        {
            if (_startPoint == default || _currentRectEnd == default || SelectedClass == null) return;
            var ann = new Annotation
            {
                Points = new List<Point> { _startPoint, _currentRectEnd },
                IsPolygon = false,
                ClassId = SelectedClass.Id,
                ClassName = SelectedClass.Name
            };
            Annotations.Add(ann);
        }

        private void FinishCurrentPolygon()
        {
            if (_currentPolygonPoints.Count < 3 || SelectedClass == null) return;
            var ann = new Annotation
            {
                Points = new List<Point>(_currentPolygonPoints),
                IsPolygon = true,
                ClassId = SelectedClass.Id,
                ClassName = SelectedClass.Name
            };
            Annotations.Add(ann);
            _currentPolygonPoints.Clear();
            PolygonPointCount = 0;
            _tempMovePoint = null;
        }

        public void FinishPolygonIfPossible()
        {
            if (IsPolygonMode && _currentPolygonPoints.Count >= 3 && SelectedClass != null)
                FinishCurrentPolygon();
        }

        private void CancelCurrentPolygon()
        {
            _currentPolygonPoints.Clear();
            PolygonPointCount = 0;
            _tempMovePoint = null;
            RedrawAllAnnotations();
        }
        #endregion

        #region 控制項設置
        public void SetControls(Image? image, Canvas? canvas)
        {
            _imageControl = image;
            _canvas = canvas;
        }
        #endregion

        #region 圖像加載與瀏覽
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
            if (index < 0 || index >= ImagePaths.Count) return;
            CurrentImageIndex = index;
            var localPath = ImagePaths[index];
            try
            {
                using var stream = File.OpenRead(localPath);
                CurrentImage = new Bitmap(stream);
                _currentSkBitmap?.Dispose();
                _currentSkBitmap = SKBitmap.Decode(localPath);
                ImagePixelWidth = CurrentImage.PixelSize.Width;
                ImagePixelHeight = CurrentImage.PixelSize.Height;
                Annotations.Clear();
                _currentPolygonPoints.Clear();
                PolygonPointCount = 0;
                _isDragging = false;
                _tempMovePoint = null;
                RedrawAllAnnotations();
                StatusText = $"已載入第 {index + 1} 張圖片（共 {ImagePaths.Count} 張）";
            }
            catch (Exception ex)
            {
                StatusText = $"載入失敗: {ex.Message}";
            }
        }
        #endregion

        #region 類別管理（本地添加）
        private int _nextClassId = 1;

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
        #endregion

        #region AI 自動標註
        private async Task RunAutoAnnotationAsync()
        {
            if (CurrentImage == null || _currentSkBitmap == null)
            {
                StatusText = "請先載入圖片";
                return;
            }

            string modelPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Models", "yolov8.onnx");
            if (!File.Exists(modelPath))
            {
                StatusText = "未找到模型文件，請將 ONNX 模型放置於 Models 文件夾";
                return;
            }

            try
            {
                using var session = new InferenceSession(modelPath);
                var preprocessor = YoloPreprocessor.FromSession(session);
                var classNames = new[] { "object" };
                var postprocessor = new YoloPostprocessor(
                    confThreshold: 0.30f,
                    iouThreshold: 0.45f,
                    classNames: classNames,
                    inputWidth: preprocessor.TargetWidth,
                    inputHeight: preprocessor.TargetHeight,
                    originalWidth: (int)ImagePixelWidth,
                    originalHeight: (int)ImagePixelHeight);
                postprocessor.UpdateLetterboxParams((int)ImagePixelWidth, (int)ImagePixelHeight);
                using var inferenceService = new OnnxInferenceService(modelPath, preprocessor, postprocessor);

                StatusText = "🤖 正在進行 AI 自動標註...";
                var result = await inferenceService.PredictAsync(_currentSkBitmap);
                int added = 0;
                foreach (var box in result.Boxes)
                {
                    if (box.Confidence < 0.25f) continue;
                    var ann = new Annotation
                    {
                        IsPolygon = false,
                        ClassId = -1,
                        ClassName = box.Label,
                        Points = new List<Point>
                        {
                            new Point(box.X, box.Y),
                            new Point(box.X + box.Width, box.Y + box.Height)
                        }
                    };
                    Annotations.Add(ann);
                    added++;
                }
                RedrawAllAnnotations();
                StatusText = $"✅ AI 自動標註完成！新增 {added} 個矩形框";
            }
            catch (Exception ex)
            {
                StatusText = $"自動標註失敗: {ex.Message}";
            }
        }
        #endregion

        #region INavigationAware 實作
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            await InitializeRuleClassAsync();
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            _currentSkBitmap?.Dispose();
            _httpClient.Dispose();
        }
        #endregion
    }

    #region 輔助類型
    public class LocalClass
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Annotation
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public bool IsPolygon { get; set; }
        public List<Point> Points { get; set; } = new();
        public string DisplayText => IsPolygon ? $"多邊形 [{ClassName}]" : $"矩形 [{ClassName}]";
    }
    #endregion
}