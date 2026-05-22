using Avalonia;
using Microsoft.ML.OnnxRuntime;
using ODProxl.ClientServices.Impls;
using ODProxl.Utils.HttpService;
using RestSharp;

namespace ODProxl.ViewModels.Pages.AnnotationPageViewModels
{
    public partial class AnnotationPageViewModel
    {
        private async Task SaveAnnotationsToServerAsync()
        {
            if (string.IsNullOrEmpty(CurrentModelFolder))
            {
                StatusText = "請先選擇模型文件夾";
                return;
            }
            if (ExpectedImagePaths.Count == 0 || CurrentImageIndex < 0 || CurrentImageIndex >= ExpectedImagePaths.Count)
            {
                StatusText = "沒有可保存的圖片";
                return;
            }
            string currentImageUrl = ExpectedImagePaths[CurrentImageIndex];
            var annotationsForImage = Annotations.ToList();
            var dto = new
            {
                ImageUrl = currentImageUrl,
                ModelFolder = CurrentModelFolder,
                Annotations = annotationsForImage.Select(a => new
                {
                    a.ClassId,
                    a.ClassName,
                    a.IsPolygon,
                    Points = a.Points.Select(p => new { X = p.X, Y = p.Y }).ToList()
                })
            };
            var request = new ClientRequest
            {
                Url = "Annotations/save",
                Method = Method.Post,
                ContentType = "application/json",
                Parameters = dto
            };
            var response = await _httpRestClient.ExecuteAsync<object>(request);
            if (response.IsSuccess)
                StatusText = "標註已保存";
            else
                StatusText = $"保存失敗";
        }

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
    }
}
