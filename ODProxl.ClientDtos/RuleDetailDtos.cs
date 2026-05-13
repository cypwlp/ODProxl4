namespace ODProxl.ClientDtos
{
    public record RuleDetailDto
    {
        public int DetailId { get; set; }
        public int RuleId { get; set; }
        public int? ConditionId { get; set; }
        public int ClassId { get; set; }
        public string? AttrName { get; set; }
        public string? AttrValue { get; set; }
        public string? AttrUnit { get; set; }
    }

    public record CreateRuleDetailDto
    {
        public int RuleId { get; set; }
        public int? ConditionId { get; set; }
        public int ClassId { get; set; }
        public string AttrName { get; set; } = string.Empty;
        public string AttrValue { get; set; } = string.Empty;
        public string? AttrUnit { get; set; }
    }

    public record UpdateRuleDetailDto
    {
        public int? ConditionId { get; set; }
        public string? AttrName { get; set; }
        public string? AttrValue { get; set; }
        public string? AttrUnit { get; set; }
    }
}