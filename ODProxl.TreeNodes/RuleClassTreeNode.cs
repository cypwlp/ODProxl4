using System.Collections.ObjectModel;

namespace ODProxl.TreeNodes
{
    public class RuleClassTreeNode : BindableBase
    {
        private int _ruleClassId;
        private string _ruleClassKey = string.Empty;
        private string _ruleClassName = string.Empty;
        private int _fileId;
        private string _fileUrl = string.Empty;
        private string? _createdBy;
        private DateTime _createdTime;
        private string? _updatedBy;
        private DateTime _updatedTime;
        private ObservableCollection<RuleClassTreeNode> _children = new();

        public int RuleClassId
        {
            get => _ruleClassId;
            set => SetProperty(ref _ruleClassId, value);
        }

        public string RuleClassKey
        {
            get => _ruleClassKey;
            set => SetProperty(ref _ruleClassKey, value);
        }

        public string RuleClassName
        {
            get => _ruleClassName;
            set => SetProperty(ref _ruleClassName, value);
        }

        public int FileId
        {
            get => _fileId;
            set => SetProperty(ref _fileId, value);
        }

        public string FileUrl
        {
            get => _fileUrl;
            set => SetProperty(ref _fileUrl, value);
        }

        public string? CreatedBy
        {
            get => _createdBy;
            set => SetProperty(ref _createdBy, value);
        }

        public DateTime CreatedTime
        {
            get => _createdTime;
            set => SetProperty(ref _createdTime, value);
        }

        public string? UpdatedBy
        {
            get => _updatedBy;
            set => SetProperty(ref _updatedBy, value);
        }

        public DateTime UpdatedTime
        {
            get => _updatedTime;
            set => SetProperty(ref _updatedTime, value);
        }

        public ObservableCollection<RuleClassTreeNode> Children
        {
            get => _children;
            set => SetProperty(ref _children, value);
        }
    }
}