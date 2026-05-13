using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ODProxl.ClientCommonModels.TreeNode
{
    public class ConditionTreeNode : INotifyPropertyChanged
    {
        private int _conditionId;
        private string? _conditionName;
        private string? _operator;
        private decimal _value;
        private string? _unit;

        public int ConditionId
        {
            get => _conditionId;
            set { _conditionId = value; OnPropertyChanged(); }
        }

        public string? ConditionName
        {
            get => _conditionName;
            set { _conditionName = value; OnPropertyChanged(); }
        }

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

        public ObservableCollection<DetailTreeNode> Details { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}