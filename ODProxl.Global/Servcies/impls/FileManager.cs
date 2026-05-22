using ODProxl.ClientDtos;
using ODProxl.Global.Services;
using ODProxl.Utils.HttpService;
using RestSharp;
using System.Net.Http.Headers;
using System.Text;

namespace ODProxl.Global.Servcies.impls
{
    public class FileManager : IFileManager, IDisposable
    {
        private readonly IConfigManager _configManager;
        private readonly IHttpRestClient _httpRestClient;
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _uploadSemaphore = new(5);
        private readonly HashSet<string> _createdDirectories = new();
        private readonly object _dirLock = new();

        private string _credentialsL;
        private string _credentialsP;

        public FileManager(IConfigManager configManager, IHttpRestClient httpRestClient, HttpClient httpClient)
        {
            _configManager = configManager;
            _httpRestClient = httpRestClient;
            _httpClient = httpClient;

            _configManager.ConfigChanged += () =>
            {
                _credentialsL = _configManager.GetValue("credentials_l") ?? string.Empty;
                _credentialsP = _configManager.GetValue("credentials_p") ?? string.Empty;
            };
            _credentialsL = _configManager.GetValue("credentials_l") ?? string.Empty;
            _credentialsP = _configManager.GetValue("credentials_p") ?? string.Empty;
        }

        public async Task<string> UploadSingleFileAsync(string localFilePath, string baseUrl, string customUrl,
            string credentials_l, string credentials_p, string fileType)
        {
            string fullDirUrl = $"{baseUrl.TrimEnd('/')}/{customUrl.Trim('/')}/";
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(localFilePath)}";
            var requestUrl = $"{fullDirUrl}{fileName}";
            bool success = await TryPutFileAsync(requestUrl, localFilePath, credentials_l, credentials_p);
            if (!success)
            {
                await EnsureDirectoryExistsRecursiveAsync(fullDirUrl, credentials_l, credentials_p);
                success = await TryPutFileAsync(requestUrl, localFilePath, credentials_l, credentials_p);
                if (!success)
                    throw new HttpRequestException($"无法上传文件到 {requestUrl}，请检查服务器权限或路径是否正确。");
            }
            string singleFileUrl = baseUrl + $"{customUrl}/{fileName}";
            await SaveSingleFileAsync(singleFileUrl, fileType);
            return singleFileUrl;
        }

        private async Task<bool> TryPutFileAsync(string requestUrl, string localFilePath,
            string credentials_l, string credentials_p)
        {
            try
            {
                using var fileStream = File.OpenRead(localFilePath);
                using var content = new StreamContent(fileStream);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var request = new HttpRequestMessage(HttpMethod.Put, requestUrl) { Content = content };
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{credentials_l}:{credentials_p}")));

                var response = await _httpClient.SendAsync(request);
                System.Diagnostics.Debug.WriteLine($"[FileManager] PUT {requestUrl} -> {(int)response.StatusCode} {response.ReasonPhrase}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FileManager] PUT {requestUrl} 异常: {ex.Message}");
                return false;
            }
        }

        public async Task<IEnumerable<string>> UploadFilesAsync(
            IEnumerable<string> localFilePaths, string baseUrl, string customUrl,
            string credentials_l, string credentials_p, string fileType)
        {
            var tasks = localFilePaths.Select(async path =>
            {
                await _uploadSemaphore.WaitAsync();
                try
                {
                    return await UploadSingleFileAsync(path, baseUrl, customUrl,
                        credentials_l, credentials_p, fileType);
                }
                finally
                {
                    _uploadSemaphore.Release();
                }
            });
            return await Task.WhenAll(tasks);
        }

        private async Task EnsureDirectoryExistsRecursiveAsync(string directoryUrl, string credentials_l, string credentials_p)
        {
            if (!directoryUrl.EndsWith("/"))
                directoryUrl += "/";
            lock (_dirLock)
            {
                if (_createdDirectories.Contains(directoryUrl))
                    return;
            }
            if (await HeadDirectoryAsync(directoryUrl, credentials_l, credentials_p))
            {
                lock (_dirLock) _createdDirectories.Add(directoryUrl);
                return;
            }
            string? parentDir = GetParentDirectoryUrl(directoryUrl);
            if (parentDir != null && parentDir != directoryUrl)
            {
                await EnsureDirectoryExistsRecursiveAsync(parentDir, credentials_l, credentials_p);
            }
            if (await MkcolDirectoryAsync(directoryUrl, credentials_l, credentials_p))
            {
                lock (_dirLock) _createdDirectories.Add(directoryUrl);
                return;
            }
            if (await CreateWithPlaceholderAsync(directoryUrl, credentials_l, credentials_p))
            {
                lock (_dirLock) _createdDirectories.Add(directoryUrl);
                return;
            }
            System.Diagnostics.Debug.WriteLine($"[FileManager] 无法自动创建目录 {directoryUrl}，将依赖上传时服务器自动创建。");
        }

        private async Task<bool> HeadDirectoryAsync(string url, string user, string pass)
        {
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            AddBasicAuth(request, user, pass);
            var response = await _httpClient.SendAsync(request);
            System.Diagnostics.Debug.WriteLine($"[FileManager] HEAD {url} -> {(int)response.StatusCode}");
            return response.IsSuccessStatusCode;
        }

        private async Task<bool> MkcolDirectoryAsync(string url, string user, string pass)
        {
            var request = new HttpRequestMessage(new HttpMethod("MKCOL"), url);
            AddBasicAuth(request, user, pass);
            var response = await _httpClient.SendAsync(request);
            System.Diagnostics.Debug.WriteLine($"[FileManager] MKCOL {url} -> {(int)response.StatusCode}");
            return response.IsSuccessStatusCode;
        }

        private async Task<bool> CreateWithPlaceholderAsync(string url, string user, string pass)
        {
            var placeholderUrl = $"{url}.odproxl_placeholder";
            var content = new StringContent("", Encoding.UTF8);
            var request = new HttpRequestMessage(HttpMethod.Put, placeholderUrl);
            AddBasicAuth(request, user, pass);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            System.Diagnostics.Debug.WriteLine($"[FileManager] PLACEHOLDER PUT {placeholderUrl} -> {(int)response.StatusCode}");
            if (response.IsSuccessStatusCode)
            {
                var delRequest = new HttpRequestMessage(HttpMethod.Delete, placeholderUrl);
                AddBasicAuth(delRequest, user, pass);
                await _httpClient.SendAsync(delRequest);
                return true;
            }
            return false;
        }

        private string? GetParentDirectoryUrl(string url)
        {
            string trimmed = url.TrimEnd('/');
            int lastSlash = trimmed.LastIndexOf('/');
            if (lastSlash <= 0) return null;
            return trimmed.Substring(0, lastSlash + 1);
        }

        private void AddBasicAuth(HttpRequestMessage request, string username, string password)
        {
            var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
        }

        private async Task SaveSingleFileAsync(string SingleFileUrl, string fileType)
        {
            var fileName = Path.GetFileName(SingleFileUrl);
            var fileExtension = Path.GetExtension(SingleFileUrl)?.TrimStart('.');
            var createFileDto = new CreateFileDto
            {
                FileName = fileName,
                FileType = fileType,
                FileExtension = fileExtension,
                FileUrl = SingleFileUrl
            };

            var request = new ClientRequest
            {
                Url = "File",
                Method = Method.Post,
                ContentType = "application/json",
                Parameters = createFileDto
            };
            await _httpRestClient.ExecuteAsync<FileDto>(request);
        }

        public async Task SaveFileAsync(IEnumerable<string> fileUrls, string fileType)
        {
            foreach (var fileUrl in fileUrls)
                await SaveSingleFileAsync(fileUrl, fileType);
        }

        public void Dispose()
        {
            _uploadSemaphore?.Dispose();
        }
    }
}