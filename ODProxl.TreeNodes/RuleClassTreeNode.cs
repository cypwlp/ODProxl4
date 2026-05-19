using ODProxl.ClientServices.Impls;

namespace ODProxl.TreeNodes
{
    public class RuleClassTreeNode : BaseTreeNode
    {
        public int RuleClassId { get; set; }
        public string RuleClassKey { get; set; } = string.Empty;
        public string RuleClassName { get; set; } = string.Empty;
        public string? CreatedBy { get; set; }
        public DateTime CreatedTime { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }
        public string? GroupHeader
        { get; set; }
    }
}
