namespace ODProxl.ClientDtos
{
    public record ProductRuleDto
    {
        public int RuleId { get; set; }
        public string? ProductCode { get; set; }
        public string? RuleName { get; set; }
        public bool IsActive { get; set; }
    }

    public record CreateProductRuleDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string? RuleName { get; set; }
        public bool? IsActive { get; set; } = true;
    }

    public record UpdateProductRuleDto
    {
        public string? ProductCode { get; set; }
        public string? RuleName { get; set; }
        public bool? IsActive { get; set; }
    }
}