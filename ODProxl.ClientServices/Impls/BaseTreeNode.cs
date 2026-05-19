using System.Collections;
using System.Collections.ObjectModel;

namespace ODProxl.ClientServices.Impls
{
    public partial class BaseTreeNode : BindableBase, ITreeNode
    {
        // 子节点集合
        public ObservableCollection<BaseTreeNode> Children { get; } = new();

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
