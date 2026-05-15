namespace ODProxl.ClientDtos
{
    public record ProductGroupDto
    {
        public int GroupId { get; init; }
        public string GroupName { get; init; } = string.Empty;
        public bool? IsActive { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
        public string UpdatedBy { get; init; } = string.Empty;
        public DateTime CreatedTime { get; init; }
        public DateTime UpdatedTime { get; init; }
    }

    public record CreateProductGroupDto
    {
        public string GroupName { get; init; } = string.Empty;
        public bool? IsActive { get; init; } = true;
        public string CreatedBy { get; init; } = string.Empty;
    }

    public record UpdateProductGroupDto
    {
        public int GroupId { get; init; }
        public string? GroupName { get; init; }
        public bool? IsActive { get; init; }
        public string UpdatedBy { get; init; } = string.Empty;
    }
}
