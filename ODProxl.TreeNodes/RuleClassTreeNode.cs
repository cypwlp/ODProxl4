using ODProxl.ClientServices;
using System.Collections;
using System.Collections.ObjectModel;

namespace ODProxl.TreeNodes
{
    public class RuleClassTreeNode : BindableBase, ITreeNode
    {
        public int RuleClassId { get; set; }
        public string RuleClassKey { get; set; } = string.Empty;
        public string RuleClassName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string? CreatedBy { get; set; }
        public DateTime CreatedTime { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedTime { get; set; }

        // 子节点集合（关键：类型为 RuleClassTreeNode）
        public ObservableCollection<RuleClassTreeNode> Children { get; } = new();

        // ITreeNode 实现
        IEnumerable ITreeNode.Children => Children;

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public bool HasChildren => Children.Count > 0;
    }
}