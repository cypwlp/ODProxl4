
using ODProxl.Global.Services;
using ODProxl.Utils.HttpService;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;

namespace ODProxl.Global.Servcies.impls
{
    public class FileManager : IFileManager, IDisposable
    {
        private readonly IConfigManager _configManager;
        private readonly IHttpRestClient _httpRestClient;
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _uploadSemaphore = new(5); // 控制并发上传数量

        // 使用 volatile 确保凭证更新的可见性
        private volatile string _credentialsL;
        private volatile string _credentialsP;

        // 保留 FileUrl 以兼容旧调用（SaveFileAsync 依赖），但标注为 Obsolete
        [Obsolete("此属性将被移除，请通过上传返回值直接传递文件URL")]
        public string FileUrl { get; private set; }

        public FileManager(IConfigManager configManager, IHttpRestClient httpRestClient, HttpClient httpClient)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _httpRestClient = httpRestClient ?? throw new ArgumentNullException(nameof(httpRestClient));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

            // 初始化凭证
            (_credentialsL, _credentialsP) = LoadCredentials();

            // 订阅配置变更事件
            _configManager.ConfigChanged += OnConfigChanged;
        }

        /// <summary>
        /// 上传单个文件，返回可访问的文件URL。
        /// 方法结束时会自动更新 FileUrl 属性（已过时）。
        /// </summary>
        public async Task<string> UploadFileAsync(string localFilePath, string baseUrl, string customPath)
        {
            var fileUrl = await UploadFileInternalAsync(localFilePath, baseUrl, customPath).ConfigureAwait(false);
            FileUrl = fileUrl; // 保持向后兼容
            return fileUrl;
        }

        /// <summary>
        /// 并发上传多个文件，通过信号量控制并发数。
        /// 所有文件上传成功后才会返回；任意失败则抛出 AggregateException。
        /// </summary>
        public async Task UploadFilesAsync(IEnumerable<string> localFilePaths, string baseUrl, string customPath)
        {
            var paths = localFilePaths?.ToList() ?? throw new ArgumentNullException(nameof(localFilePaths));
            if (paths.Count == 0) return;

            var exceptions = new ConcurrentQueue<Exception>();
            string lastFileUrl = null;

            var tasks = paths.Select(async path =>
            {
                await _uploadSemaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    var url = await UploadFileInternalAsync(path, baseUrl, customPath).ConfigureAwait(false);
                    lastFileUrl = url; // 简单记录最后一个（不保证顺序，仅用于兼容）
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
                finally
                {
                    _uploadSemaphore.Release();
                }
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);

            if (!exceptions.IsEmpty)
                throw new AggregateException("一个或多个文件上传失败。", exceptions);

            // 更新 FileUrl 为最后一个成功上传的URL（保持向后兼容）
            if (lastFileUrl != null)
                FileUrl = lastFileUrl;
        }

        /// <summary>
        /// 将文件元数据保存到后端服务。依赖 FileUrl 属性（即将弃用）。
        /// </summary>
        public async Task SaveFileAsync(string fileType)
        {
            if (string.IsNullOrWhiteSpace(FileUrl))
                throw new InvalidOperationException("FileUrl 尚未设置，请先上传文件。");

            // 安全解析URL，避免 Path 方法异常
            var uri = new Uri(FileUrl);
            string fileName = uri.Segments[^1];
            string fileExtension = null;
            int dotIndex = fileName.LastIndexOf('.');
            if (dotIndex >= 0 && dotIndex < fileName.Length - 1)
                fileExtension = fileName[(dotIndex + 1)..];

            var request = new ClientRequest
            {
                Url = "File",
                Method = RestSharp.Method.Post,
                ContentType = "application/json",
                Parameters = new ClientDtos.CreateFileDto
                {
                    FileUrl = FileUrl,
                    FileName = fileName,
                    FileExtension = fileExtension,
                    FileType = fileType
                }
            };

            await _httpRestClient.ExecuteAsync<ClientDtos.FileDto>(request).ConfigureAwait(false);
        }

        /// <summary>
        /// 核心上传逻辑，返回生成的文件URL，不修改外部状态。
        /// </summary>
        private async Task<string> UploadFileInternalAsync(string localFilePath, string baseUrl, string customPath)
        {
            if (!File.Exists(localFilePath))
                throw new FileNotFoundException($"本地文件不存在: {localFilePath}");

            // 安全的URL拼接
            var baseUri = new Uri(baseUrl.TrimEnd('/') + "/");
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(localFilePath)}";
            var requestUri = new Uri(baseUri, $"{customPath.Trim('/')}/{fileName}");

            using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            using var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
            {
                Content = content
            };

            // 使用UTF8编码避免非ASCII字符问题
            var credentials = $"{_credentialsL}:{_credentialsP}";
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials))
            );

            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return requestUri.ToString();
        }

        private (string l, string p) LoadCredentials()
        {
            return (
                _configManager.GetValue("credentials_l"),
                _configManager.GetValue("credentials_p")
            );
        }

        private void OnConfigChanged()
        {
            (_credentialsL, _credentialsP) = LoadCredentials();
        }

        public void Dispose()
        {
            _configManager.ConfigChanged -= OnConfigChanged;
            _uploadSemaphore?.Dispose();
        }
    }
}
