using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Docnet.Core;
using Docnet.Core.Models;
using SkiaSharp;

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

            int totalProcessed = 0;
            foreach (var pdfPath in pdfPaths)
            {
                // 上传原始 PDF（使用 IFileManager，不再检查重复）
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
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        StatusText = $"無法讀取 PDF：{System.IO.Path.GetFileName(pdfPath)} - {ex.Message}");
                    continue;
                }

                for (int page = 0; page < pageCount; page++)
                {
                    string imageName = $"{pdfFileName}_p{(page + 1):D3}.png";
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        StatusText = $"正在處理圖片: {imageName}");

                    // 直接渲染并上传，获取实际 URL（GUID 前缀）
                    string uploadedUrl = await UploadImageAsync(pdfPath, page);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ExpectedImagePaths.Add(uploadedUrl);
                        totalProcessed++;
                        StatusText = $"已上傳圖片 {imageName} ({totalProcessed} 張)";
                        if (ExpectedImagePaths.Count == 1)
                        {
                            CurrentImageIndex = 0;
                            _ = LoadImageAsync(0);
                        }
                    });
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
                StatusText = $"處理完成，共 {totalProcessed} 張圖片（已自動同步至伺服器）");
        }

        private async Task UploadOriginalPdfAsync(string pdfPath)
        {
            if (string.IsNullOrEmpty(source_pdf_base_url))
            {
                StatusText = "未設定 PDF 上傳位址 (source_pdf_base_path)，略過上傳原始 PDF";
                return;
            }
            if (string.IsNullOrEmpty(credentials_l) || string.IsNullOrEmpty(credentials_p))
            {
                StatusText = "缺少上傳憑證，無法上傳 PDF";
                return;
            }

            string pdfFileName = Path.GetFileName(pdfPath);
            try
            {
                string uploadedUrl = await UploadPdfAsync(pdfPath);
                StatusText = $"已上傳原始 PDF：{pdfFileName} → {uploadedUrl}";
                // 注意：IFileManager 内部已保存文件元数据，此处无需再调用 SaveFileMetadataAsync
            }
            catch (Exception ex)
            {
                StatusText = $"上傳 PDF 失敗：{pdfFileName} - {ex.Message}";
            }
        }

        //private async Task<string> UploadPdfAsync(string localPdfPath)
        //{
        //    return await _fileManager.UploadSingleFileAsync(
        //        localPdfPath,
        //        source_pdf_base_url,
        //        "annotation/pdfs",
        //        credentials_l,
        //        credentials_p,
        //        "標註原PDF文件");
        //}

        private async Task<string> UploadPdfAsync(string localPdfPath)
        {
            string customUrl = "annotation/pdfs";
            string hash = await _fileManager.ComputeFileSHA256Async(localPdfPath);
            string targetFileName = $"{hash}.pdf";
            string fullUrl = $"{source_pdf_base_url.TrimEnd('/')}/{customUrl}/{targetFileName}";

            // 检查服务器是否已存在
            if (await _fileManager.FileExistsOnServerAsync(fullUrl, credentials_l, credentials_p))
            {
                StatusText = $"PDF 已存在於伺服器，略過上傳：{Path.GetFileName(localPdfPath)}";
                return fullUrl;
            }

            // 上传
            return await _fileManager.UploadSingleFileWithFileNameAsync(
                localPdfPath,
                source_pdf_base_url.TrimEnd('/'),
                customUrl,
                targetFileName,
                credentials_l,
                credentials_p,
                "標註原PDF文件");
        }
        private async Task<string> UploadImageAsync(string pdfPath, int pageIndex)
        {
            byte[] pngBytes = await RenderPdfPageToPngAsync(pdfPath, pageIndex);

            // 计算哈希
            string hash = _fileManager.ComputeBytesSHA256(pngBytes);
            string targetFileName = $"{hash}.png";

            var imageBaseUri = new Uri(annotation_image_base_url);
            string baseUrl = imageBaseUri.GetLeftPart(UriPartial.Authority);
            string customUrl = imageBaseUri.AbsolutePath.Trim('/');
            string fullUrl = $"{baseUrl}/{customUrl}/{targetFileName}";

            // 检查是否存在
            if (await _fileManager.FileExistsOnServerAsync(fullUrl, credentials_l, credentials_p))
            {
                StatusText = $"圖片已存在於伺服器，略過上傳：{Path.GetFileNameWithoutExtension(pdfPath)} 第 {pageIndex + 1} 頁";
                return fullUrl;
            }

            // 写入临时文件并上传
            string tempFile = Path.GetTempFileName() + ".png";
            await File.WriteAllBytesAsync(tempFile, pngBytes);
            try
            {
                return await _fileManager.UploadSingleFileWithFileNameAsync(
                    tempFile,
                    baseUrl,
                    customUrl,
                    targetFileName,
                    credentials_l,
                    credentials_p,
                    "image");
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
        //private async Task<string> UploadImageAsync(string pdfPath, int pageIndex)
        //{
        //    byte[] pngBytes = await RenderPdfPageToPngAsync(pdfPath, pageIndex);

        //    string tempFile = Path.GetTempFileName() + ".png";
        //    await File.WriteAllBytesAsync(tempFile, pngBytes);
        //    try
        //    {
        //        var imageBaseUri = new Uri(annotation_image_base_url);
        //        string baseUrl = imageBaseUri.GetLeftPart(UriPartial.Authority);
        //        string customUrl = imageBaseUri.AbsolutePath.TrimEnd('/');

        //        return await _fileManager.UploadSingleFileAsync(
        //            tempFile,
        //            baseUrl,
        //            customUrl,
        //            credentials_l,
        //            credentials_p,
        //            "image");
        //    }
        //    finally
        //    {
        //        if (File.Exists(tempFile))
        //            File.Delete(tempFile);
        //    }
        //}

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
    }
}