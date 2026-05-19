using System.Collections;

namespace ODProxl.ClientServices
{
    public interface ITreeNode
    {
        /// <summary>子节点集合</summary>
        IEnumerable Children { get; }
        /// <summary>是否展开（需要绑定 TwoWay）</summary>
        bool IsExpanded { get; set; }
        /// <summary>是否包含子节点（用于显示/隐藏展开按钮）</summary>
        bool HasChildren { get; }
    }
}
