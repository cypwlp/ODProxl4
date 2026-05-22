using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Docnet.Core;
using Docnet.Core.Models;
using ODProxl.Utils.HttpService;
using RestSharp;
using SkiaSharp;
using System.Text;

namespace ODProxl.ViewModels.Pages.AnnotationPageViewModels
{
    public partial class AnnotationPageViewModel
    {
        private async Task OpenPdfAsync()
        {
            if (_topLevel == null)
            {
                StatusText = "無法開啟檔案對話框（未取得視窗控制代碼）";
                return;
            }
            var folders = await _topLevel.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = "選擇包含 PDF 的資料夾",
                    AllowMultiple = false
                });
            if (folders.Count > 0)
            {
                string folderPath = folders[0].Path.LocalPath;
                await ProcessPdfFolderAsync(folderPath);
                return;
            }
            var files = await _topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = "選擇 PDF 檔案",
                    AllowMultiple = true,
                    FileTypeFilter = new[]
                    {
                    new FilePickerFileType("PDF 文件") { Patterns = new[] { "*.pdf" } }
                    }
                });
            if (files.Count > 0)
            {
                var pdfPaths = files.Select(f => f.Path.LocalPath).ToArray();
                await ProcessPdfFilesAsync(pdfPaths);
            }
        }

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
            var pdfFiles = await Task.Run(() =>
                Directory.GetFiles(folderPath, "*.pdf", SearchOption.AllDirectories));
            if (pdfFiles.Length == 0)
            {
                StatusText = "所選資料夾中沒有 PDF 檔案。";
                return;
            }
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
                await UploadOriginalPdfAsync(pdfPath);

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
                        string uploadedUrl = await UploadImageWithFileManagerAsync(pngBytes, imageHttpUrl, "images");
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            ExpectedImagePaths.Add(uploadedUrl);
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

        private async Task UploadOriginalPdfAsync(string pdfPath)
        {
            if (string.IsNullOrEmpty(_pdfBaseUrl))
            {
                StatusText = "未設定 PDF 上傳位址 (pdf_base_url)，略過上傳原始 PDF";
                return;
            }
            string pdfFileName = System.IO.Path.GetFileName(pdfPath);
            string targetPdfUrl = _pdfBaseUrl + pdfFileName;
            bool exists = await PdfExistsOnServerAsync(targetPdfUrl);
            if (exists)
            {
                StatusText = $"PDF 已存在伺服器：{pdfFileName}";
                return;
            }
            try
            {
                string uploadedUrl = await UploadPdfWithFileManagerAsync(pdfPath, targetPdfUrl, "pdfs");
                StatusText = $"已上傳原始 PDF：{pdfFileName} → {uploadedUrl}";
            }
            catch (Exception ex)
            {
                StatusText = $"上傳 PDF 失敗：{pdfFileName} - {ex.Message}";
            }
        }

        private async Task<bool> PdfExistsOnServerAsync(string pdfUrl)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, pdfUrl);
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> UploadPdfWithFileManagerAsync(string localPdfPath, string expectedUrl, string customPath)
        {
            var uri = new Uri(expectedUrl);
            string baseUrl = uri.GetLeftPart(UriPartial.Authority);
            string uploadedUrl = await _fileManager.UploadFileAsync(localPdfPath, baseUrl, customPath);
            await SaveFileMetadataAsync(uploadedUrl, "pdf");
            return uploadedUrl;
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

        private async Task<string> UploadImageWithFileManagerAsync(byte[] pngBytes, string expectedUrl, string customPath)
        {
            string tempFilePath = Path.GetTempFileName() + ".png";
            try
            {
                await File.WriteAllBytesAsync(tempFilePath, pngBytes);
                var uri = new Uri(expectedUrl);
                string baseUrl = uri.GetLeftPart(UriPartial.Authority);
                string uploadedUrl = await _fileManager.UploadFileAsync(tempFilePath, baseUrl, customPath);
                await SaveFileMetadataAsync(uploadedUrl, "image");
                return uploadedUrl;
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        private async Task SaveFileMetadataAsync(string fileUrl, string fileType)
        {
            var uri = new Uri(fileUrl);
            string fileName = uri.Segments[^1];
            string fileExtension = Path.GetExtension(fileName)?.TrimStart('.');
            var request = new ClientRequest
            {
                Url = "File",
                Method = Method.Post,
                ContentType = "application/json",
                Parameters = new ClientDtos.CreateFileDto
                {
                    FileUrl = fileUrl,
                    FileName = fileName,
                    FileExtension = fileExtension,
                    FileType = fileType
                }
            };
            await _httpRestClient.ExecuteAsync<ClientDtos.FileDto>(request);
        }
    }
}
