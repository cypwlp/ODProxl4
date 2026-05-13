using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ODProxl.ClientCommonModels.TreeNode
{
    public enum NodeType
    {
        Rule,
        Condition,
        Detail
    }

    public class UnifiedTreeNode : INotifyPropertyChanged
    {
        private NodeType _type;
        private int _id;
        private string? _name;
        private bool _isActive;
        private string? _operator;
        private decimal _value;
        private string? _unit;
        private int _classId;
        private string? _attrValue;
        private string? _attrUnit;
        private int? _conditionId;

        // 子节点集合（所有层级共用）
        public ObservableCollection<UnifiedTreeNode> Children { get; } = new();

        // === 公共属性 ===
        public NodeType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string? Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        // === 规则节点特有 ===
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        // === 条件节点特有 ===
        public string? Operator
        {
            get => _operator;
            set { _operator = value; OnPropertyChanged(); }
        }

        public decimal Value
        {
            get => _value;
            set { _value = value; OnPropertyChanged(); }
        }

        public string? Unit
        {
            get => _unit;
            set { _unit = value; OnPropertyChanged(); }
        }

        // === 明细节点特有 ===
        public int ClassId
        {
            get => _classId;
            set { _classId = value; OnPropertyChanged(); }
        }

        public string? AttrValue
        {
            get => _attrValue;
            set { _attrValue = value; OnPropertyChanged(); }
        }

        public string? AttrUnit
        {
            get => _attrUnit;
            set { _attrUnit = value; OnPropertyChanged(); }
        }

        public int? ConditionId
        {
            get => _conditionId;
            set { _conditionId = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}