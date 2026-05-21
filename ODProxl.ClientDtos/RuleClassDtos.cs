namespace ODProxl.ClientDtos
{
    public record RuleClassDto
    {
        public int RuleClassId { get; set; }
        public string? RuleClassKey { get; set; }
        public string? RuleClassName { get; set; }
        public int ParentRuleClassId { get; set; }
        public int GroupId { get; set; }
        public int FileId { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedTime { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
    }

    public record CreateRuleClassDto
    {
        public string? RuleClassKey { get; set; }
        public string? RuleClassName { get; set; }
        public int ParentRuleClassId { get; set; }
        public int GroupId { get; set; }

        public int FileId { get; set; }
    }

    public record TinyRuleClassDto
    {
        public int RuleClassId { get; set; }
        public string? RuleClassKey { get; set; }
        public string? RuleClassName { get; set; }
    }
    public record UpdateRuleClassDto
    {
        public string? RuleClassKey { get; set; }
        public string? RuleClassName { get; set; }
        public int ParentRuleClassId { get; set; }
        public int GroupId { get; set; }
        public int FileId { get; set; }
    }
}
