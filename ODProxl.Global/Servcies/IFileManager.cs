namespace ODProxl.Global.Servcies
{
    public interface IFileManager
    {
        Task<string> UploadSingleFileAsync(string localFilePath, string baseUrl, string customUrl,
                                            string credentials_l, string credentials_p, string fileType);
        Task<IEnumerable<string>> UploadFilesAsync(IEnumerable<string> localFilePaths, string baseUrl,
                                                   string customUrl, string credentials_l, string credentials_p, string fileType);

        // 使用自定义文件名上传
        Task<string> UploadSingleFileWithFileNameAsync(string localFilePath, string baseUrl, string customUrl,
                                                       string fileName, string credentials_l, string credentials_p, string fileType);

        // 计算文件 SHA256 哈希
        Task<string> ComputeFileSHA256Async(string filePath);

        // 计算字节数组 SHA256 哈希
        string ComputeBytesSHA256(byte[] data);

        // 检查服务器文件是否存在
        Task<bool> FileExistsOnServerAsync(string url, string credentials_l, string credentials_p);
    }
}