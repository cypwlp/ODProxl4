using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using ODProxl.TreeNodes;
using System.Collections.ObjectModel;

namespace ODProxl.ViewModels.Pages
{
    public class RuleClassPageViewModel : BindableBase, INavigationAware
    {
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
        public void OnNavigatedTo(NavigationContext navigationContext) { }

        private ObservableCollection<RuleClassTreeNode>? _ruleClassTreeNodes;
        private HierarchicalTreeDataGridSource<RuleClassTreeNode>? _treeSource;

        public RuleClassPageViewModel()
        {
            // 构建测试数据（两层）
            var root1 = new RuleClassTreeNode
            {
                RuleClassId = 1,
                RuleClassKey = "key01",
                RuleClassName = "CE標誌",
                FileUrl = "http://ce.png",
                CreatedBy = "L5940",
                CreatedTime = new DateTime(2025, 11, 20)
            };
            root1.Children.Add(new RuleClassTreeNode
            {
                RuleClassId = 2,
                RuleClassKey = "key02",
                RuleClassName = "F標誌",
                FileUrl = "http://f.png",
                CreatedBy = "L5940",
                CreatedTime = new DateTime(2026, 1, 20)
            });
            root1.Children.Add(new RuleClassTreeNode
            {
                RuleClassId = 3,
                RuleClassKey = "key03",
                RuleClassName = "G標誌",
                FileUrl = "http://g.png",
                CreatedBy = "L5940",
                CreatedTime = new DateTime(2026, 1, 20)
            });

            var root2 = new RuleClassTreeNode
            {
                RuleClassId = 4,
                RuleClassKey = "key04",
                RuleClassName = "CEK標誌",
                FileUrl = "http://cek.png",
                CreatedBy = "L5940",
                CreatedTime = new DateTime(2025, 11, 20)
            };

            var root3 = new RuleClassTreeNode
            {
                RuleClassId = 5,
                RuleClassKey = "key05",
                RuleClassName = "CEP標誌",
                FileUrl = "http://cep.png",
                CreatedBy = "L5940",
                CreatedTime = new DateTime(2025, 11, 20)
            };

            var root4 = new RuleClassTreeNode
            {
                RuleClassId = 6,
                RuleClassKey = "key06",
                RuleClassName = "CEY標誌",
                FileUrl = "http://cey.png",
                CreatedBy = "L5940",
                CreatedTime = new DateTime(2025, 11, 20)
            };

            RuleClassTreeNodes = new ObservableCollection<RuleClassTreeNode> { root1, root2, root3, root4 };
            BuildTreeSource();
        }

        private void BuildTreeSource()
        {
            if (RuleClassTreeNodes == null) return;

            TreeSource = new HierarchicalTreeDataGridSource<RuleClassTreeNode>(RuleClassTreeNodes)
            {
                Columns =
                {
                    new HierarchicalExpanderColumn<RuleClassTreeNode>(
                        new TextColumn<RuleClassTreeNode, int>("類別ID", x => x.RuleClassId),
                        x => x.Children
                    ),
                    new TextColumn<RuleClassTreeNode, string>("類別鍵值", x => x.RuleClassKey),
                    new TextColumn<RuleClassTreeNode, string>("類別名稱", x => x.RuleClassName),
                    new TextColumn<RuleClassTreeNode, string>("URL", x => x.FileUrl),
                    new TextColumn<RuleClassTreeNode, string?>("創建人", x => x.CreatedBy),
                    new TextColumn<RuleClassTreeNode, DateTime>("創建時間", x => x.CreatedTime)
                }
            };

            TreeSource.RowSelection!.SingleSelect = true;
        }

        public ObservableCollection<RuleClassTreeNode>? RuleClassTreeNodes
        {
            get => _ruleClassTreeNodes;
            set
            {
                SetProperty(ref _ruleClassTreeNodes, value);
                BuildTreeSource();
            }
        }

        public HierarchicalTreeDataGridSource<RuleClassTreeNode>? TreeSource
        {
            get => _treeSource;
            private set => SetProperty(ref _treeSource, value);
        }

        // 预留搜索和命令绑定（根据需要添加）
        public string SearchText { get; set; } = string.Empty;
        public DelegateCommand? SearchCommand { get; }
        public DelegateCommand? EditSelectedCommand { get; }
    }
}