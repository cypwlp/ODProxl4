namespace ODProxl.ClientDtos
{
    public record FileDto
    {
        public int FileId { get; set; }
        public int? ParentFileId { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public string? FileExtension { get; set; }
        public string? FileUrl { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedTime { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
    }

    public record FileNameUrlDto
    {
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
    public record CreateFileDto
    {
        public int? ParentFileId { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public string? FileExtension { get; set; }
        public string? FileUrl { get; set; }
    }

    public record UpdateFileDto
    {
        public int? ParentFileId { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public string? FileExtension { get; set; }
        public string? FileUrl { get; set; }
    }
}
