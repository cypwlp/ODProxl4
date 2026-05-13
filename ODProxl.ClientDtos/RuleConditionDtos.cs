namespace ODProxl.ClientDtos
{
    public record RuleConditionDto
    {
        public int ConditionId { get; set; }
        public int RuleId { get; set; }
        public string? ConditionName { get; set; }
        public string? Operator { get; set; }
        public decimal Value { get; set; }
        public string? Unit { get; set; }
    }

    public record CreateRuleConditionDto
    {
        public int RuleId { get; set; }
        public string? ConditionName { get; set; }
        public string? Operator { get; set; }
        public decimal Value { get; set; }
        public string? Unit { get; set; }
    }

    public record UpdateRuleConditionDto
    {
        public string? ConditionName { get; set; }
        public string? Operator { get; set; }
        public decimal? Value { get; set; }
        public string? Unit { get; set; }
    }
}