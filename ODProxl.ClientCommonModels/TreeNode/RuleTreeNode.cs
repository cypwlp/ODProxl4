using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ODProxl.ClientCommonModels.TreeNode
{
    public class RuleTreeNode : INotifyPropertyChanged
    {
        private int _ruleId;
        private string? _ruleName;
        private bool _isActive;

        public int RuleId
        {
            get => _ruleId;
            set { _ruleId = value; OnPropertyChanged(); }
        }

        public string? RuleName
        {
            get => _ruleName;
            set { _ruleName = value; OnPropertyChanged(); }
        }

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ConditionTreeNode> Conditions { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}