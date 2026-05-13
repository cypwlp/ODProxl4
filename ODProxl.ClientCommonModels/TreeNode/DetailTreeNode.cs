using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ODProxl.ClientCommonModels.TreeNode
{
    public class DetailTreeNode : INotifyPropertyChanged
    {
        private int _detailId;
        private int _classId;
        private string? _attrName;
        private string? _attrValue;
        private string? _attrUnit;
        private int? _conditionId;

        public int DetailId
        {
            get => _detailId;
            set { _detailId = value; OnPropertyChanged(); }
        }

        public int ClassId
        {
            get => _classId;
            set { _classId = value; OnPropertyChanged(); }
        }

        public string? AttrName
        {
            get => _attrName;
            set { _attrName = value; OnPropertyChanged(); }
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