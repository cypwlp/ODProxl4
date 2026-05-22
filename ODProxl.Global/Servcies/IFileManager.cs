namespace ODProxl.Global.Servcies
{
    public interface IFileManager
    {
        Task<string> UploadSingleFileAsync(string localFilePath, string baseUrl, string customUrl,
                                            string credentials_l, string credentials_p, string fileType);
        Task<IEnumerable<string>> UploadFilesAsync(IEnumerable<string> localFilePaths, string baseUrl,
                                                   string customUrl, string credentials_l, string credentials_p, string fileType);
    }
}
