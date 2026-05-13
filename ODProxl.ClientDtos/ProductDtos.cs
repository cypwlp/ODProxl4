namespace ODProxl.ClientDtos
{
    public record ProductDto
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }

    public record CreateProductDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public record UpdateProductDto
    {
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }
}
