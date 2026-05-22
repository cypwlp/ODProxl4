namespace ODProxl.Global.Servcies
{
    public interface IFileManager
    {
        Task<string> UploadFileAsync(string localFilePath, string baseUrl, string customPath);
        Task UploadFilesAsync(IEnumerable<string> localFilePaths, string baseUrl, string customPath);
        Task SaveFileAsync(string fileType);
    }
}
