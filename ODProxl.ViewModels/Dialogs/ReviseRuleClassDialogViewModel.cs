using ODProxl.ClientDtos;
using ODProxl.Global.Services;
using ODProxl.Utils.HttpService;
using RestSharp;
using System.Net.Http.Headers;
using System.Text;

namespace ODProxl.ViewModels.Dialogs
{
    public class ReviseRuleClassDialogViewModel : BindableBase, IDialogAware
    {
        private string? _fileUrl;
        private string _ruleClassKey = string.Empty;
        private string _ruleClassName = string.Empty;
        private bool _isUploading;
        private IHttpRestClient? _httpRestClient;
        private IConfigManager _configManager;
        private string credentials_l;
        private string credentials_p;
        private string symbol_icon_url;
        private int _fileId;
        private readonly HttpClient _httpClient;

        public string? Title { get; set; }
        public DialogCloseListener RequestClose { get; set; }

        public string RuleClassKey
        {
            get => _ruleClassKey;
            set => SetProperty(ref _ruleClassKey, value);
        }

        public int FileId
        {
            get => _fileId;
            set => SetProperty(ref _fileId, value);
        }

        public string RuleClassName
        {
            get => _ruleClassName;
            set => SetProperty(ref _ruleClassName, value);
        }

        public string? FileUrl
        {
            get => _fileUrl;
            set
            {
                SetProperty(ref _fileUrl, value);
                RaisePropertyChanged(nameof(HasFile));
                RaisePropertyChanged(nameof(NotHasFile));
            }
        }

        public bool HasFile => !string.IsNullOrEmpty(FileUrl);
        public bool NotHasFile => !HasFile;

        public bool IsUploading
        {
            get => _isUploading;
            set => SetProperty(ref _isUploading, value);
        }

        public DelegateCommand ConfirmCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public ReviseRuleClassDialogViewModel(IHttpRestClient httpRestClient, IConfigManager configManager, HttpClient httpClient)
        {
            _httpRestClient = httpRestClient;
            _configManager = configManager;
            _httpClient = httpClient;
            _configManager.ConfigChanged += () =>
            {
                symbol_icon_url = _configManager.GetValue("symbol_icon_url") ?? string.Empty;
                credentials_l = _configManager.GetValue("credentials_l") ?? string.Empty;
                credentials_p = _configManager.GetValue("credentials_p") ?? string.Empty;
            };
            symbol_icon_url = _configManager.GetValue("symbol_icon_url") ?? string.Empty;
            credentials_l = _configManager.GetValue("credentials_l") ?? string.Empty;
            credentials_p = _configManager.GetValue("credentials_p") ?? string.Empty;

            ConfirmCommand = new DelegateCommand(OnConfirm);
            CancelCommand = new DelegateCommand(OnCancel);
        }

        private void OnConfirm()
        {
            var parameters = new DialogParameters
            {
                { "RuleClassName", RuleClassName },
                { "RuleClassKey", RuleClassKey },
                { "FileId", FileId }
            };
            RequestClose.Invoke(new DialogResult(ButtonResult.OK) { Parameters = parameters });
        }

        private void OnCancel()
        {
            RequestClose.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        public async Task UploadFileAsync(string localFilePath)
        {
            if (string.IsNullOrEmpty(localFilePath) || !File.Exists(localFilePath))
                return;

            try
            {
                IsUploading = true;
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(localFilePath)}";
                var requestUrl = $"{symbol_icon_url}uploads/{fileName}";

                using var fileStream = File.OpenRead(localFilePath);
                using var content = new StreamContent(fileStream);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var request = new HttpRequestMessage(HttpMethod.Put, requestUrl)
                {
                    Content = content
                };
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(Encoding.ASCII.GetBytes($"{credentials_l}:{credentials_p}"))
                );

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                FileUrl = symbol_icon_url + $"uploads/{fileName}";
                await SaveFileAsync();
            }
            finally
            {
                IsUploading = false;
            }
        }

        private async Task SaveFileAsync()
        {
            var fileName = Path.GetFileName(FileUrl);
            var fileExtension = Path.GetExtension(FileUrl)?.TrimStart('.');
            var createFileDto = new CreateFileDto
            {
                FileName = fileName,
                FileType = fileExtension,
                FileExtension = fileExtension,
                FileUrl = FileUrl
            };

            var request = new ClientRequest
            {
                Url = "File",
                Method = Method.Post,
                ContentType = "application/json",
                Parameters = createFileDto
            };
            var response = await _httpRestClient.ExecuteAsync<FileDto>(request);
            if (response.IsSuccess && response.Data != null)
            {
                FileId = response.Data.FileId;
            }
        }

        public bool CanCloseDialog() => true;
        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            Title = parameters.GetValue<string>("Title");
            if (parameters.ContainsKey("RuleClassName"))
                RuleClassName = parameters.GetValue<string>("RuleClassName");
            if (parameters.ContainsKey("RuleClassKey"))
                RuleClassKey = parameters.GetValue<string>("RuleClassKey");
            if (parameters.ContainsKey("FileId"))
                FileId = parameters.GetValue<int>("FileId");
            if (parameters.ContainsKey("FileUrl"))
                FileUrl = parameters.GetValue<string>("FileUrl");
        }
    }
}