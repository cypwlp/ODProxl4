using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using ODProxl.ClientCommonModels.TreeNode;
using ODProxl.ClientDtos;
using ODProxl.ClientServices;
using ODProxl.Utils.HttpService;
using RestSharp;
using System.Collections.ObjectModel;

namespace ODProxl.ViewModels.Pages
{
    public class RuleMakingPageViewModel : BindableBase, INavigationAware
    {
        #region INavigationAware
        public bool IsNavigationTarget(NavigationContext navigationContext) => true;
        public void OnNavigatedFrom(NavigationContext navigationContext) { }
        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            await LoadAllDataAsync();
        }
        #endregion

        #region 字段 & 构造函数
        private readonly IAuthService _authService;
        private readonly IHttpRestClient _httpRestClient;
        private readonly IEventAggregator _eventAggregator;
        private ObservableCollection<UnifiedTreeNode>? _rootNodes;  // 保存根节点引用

        public RuleMakingPageViewModel(IAuthService authService, IHttpRestClient httpRestClient, IEventAggregator eventAggregator)
        {
            _authService = authService;
            _httpRestClient = httpRestClient;
            _eventAggregator = eventAggregator;

            AddRuleCommand = new DelegateCommand(OnAddRule);
            SaveAllCommand = new DelegateCommand(OnSaveAllAsync);
            AddConditionCommand = new DelegateCommand(OnAddCondition, CanAddCondition).ObservesProperty(() => SelectedTreeItem);
            AddDetailCommand = new DelegateCommand(OnAddDetail, CanAddDetail).ObservesProperty(() => SelectedTreeItem);
            DeleteSelectedCommand = new DelegateCommand(OnDeleteSelected, CanDeleteSelected).ObservesProperty(() => SelectedTreeItem);

            Operators = new ObservableCollection<string> { "<=", ">", "==", "!=", "<", ">=" };
        }
        #endregion

        #region 属性
        private HierarchicalTreeDataGridSource<UnifiedTreeNode>? _treeSource;
        public HierarchicalTreeDataGridSource<UnifiedTreeNode>? TreeSource
        {
            get => _treeSource;
            private set => SetProperty(ref _treeSource, value);
        }

        private object? _selectedTreeItem;
        public object? SelectedTreeItem
        {
            get => _selectedTreeItem;
            set
            {
                if (SetProperty(ref _selectedTreeItem, value))
                {
                    (AddConditionCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (AddDetailCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                    (DeleteSelectedCommand as DelegateCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        private string _statusText = "就绪";
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public ObservableCollection<string> Operators { get; }

        public DelegateCommand AddRuleCommand { get; }
        public DelegateCommand SaveAllCommand { get; }
        public DelegateCommand AddConditionCommand { get; }
        public DelegateCommand AddDetailCommand { get; }
        public DelegateCommand DeleteSelectedCommand { get; }
        #endregion

        #region 数据加载 - 构建树
        private async Task LoadAllDataAsync()
        {
            StatusText = "正在加载数据...";
            var rules = await LoadProductRulesAsync();
            if (rules == null || rules.Count == 0)
            {
                TreeSource = null;
                _rootNodes = null;
                StatusText = "无数据，请新增规则";
                return;
            }

            var allConditions = new List<RuleConditionDto>();
            var allDetails = new List<RuleDetailDto>();
            foreach (var rule in rules)
            {
                var conds = await LoadConditionsByRuleId(rule.RuleId);
                allConditions.AddRange(conds);
                var details = await LoadDetailsByRuleId(rule.RuleId);
                allDetails.AddRange(details);
            }

            _rootNodes = new ObservableCollection<UnifiedTreeNode>();
            foreach (var rule in rules)
            {
                var ruleNode = new UnifiedTreeNode
                {
                    Type = NodeType.Rule,
                    Id = rule.RuleId,
                    Name = rule.RuleName,
                    IsActive = rule.IsActive
                };

                var ruleConds = allConditions.Where(c => c.RuleId == rule.RuleId).ToList();
                foreach (var cond in ruleConds)
                {
                    var condNode = new UnifiedTreeNode
                    {
                        Type = NodeType.Condition,
                        Id = cond.ConditionId,
                        Name = cond.ConditionName,
                        Operator = cond.Operator,
                        Value = cond.Value,
                        Unit = cond.Unit
                    };

                    var condDetails = allDetails.Where(d => d.ConditionId == cond.ConditionId).ToList();
                    foreach (var det in condDetails)
                    {
                        condNode.Children.Add(new UnifiedTreeNode
                        {
                            Type = NodeType.Detail,
                            Id = det.DetailId,
                            Name = det.AttrName,
                            ClassId = det.ClassId,
                            AttrValue = det.AttrValue,
                            AttrUnit = det.AttrUnit,
                            ConditionId = det.ConditionId
                        });
                    }
                    ruleNode.Children.Add(condNode);
                }

                // 无条件明细
                var unconditionalDetails = allDetails.Where(d => d.RuleId == rule.RuleId && (d.ConditionId == null || d.ConditionId == 0)).ToList();
                if (unconditionalDetails.Any())
                {
                    var dummyCond = new UnifiedTreeNode
                    {
                        Type = NodeType.Condition,
                        Id = 0,
                        Name = "无条件",
                        Operator = "",
                        Value = 0,
                        Unit = ""
                    };
                    foreach (var det in unconditionalDetails)
                    {
                        dummyCond.Children.Add(new UnifiedTreeNode
                        {
                            Type = NodeType.Detail,
                            Id = det.DetailId,
                            Name = det.AttrName,
                            ClassId = det.ClassId,
                            AttrValue = det.AttrValue,
                            AttrUnit = det.AttrUnit,
                            ConditionId = null
                        });
                    }
                    ruleNode.Children.Add(dummyCond);
                }
                _rootNodes.Add(ruleNode);
            }

            RebuildTreeSource();

            StatusText = "数据加载完成";
        }

        // 重建 TreeDataGrid 源（当根节点集合改变时调用）
        private void RebuildTreeSource()
        {
            if (_rootNodes == null) return;

            TreeSource = new HierarchicalTreeDataGridSource<UnifiedTreeNode>(_rootNodes)
            {
                Columns =
                {
                    new HierarchicalExpanderColumn<UnifiedTreeNode>(
                        new TemplateColumn<UnifiedTreeNode>("名称/ID",
                            new FuncDataTemplate<UnifiedTreeNode>((node, _) => BuildNodeNameControl(node)),
                            new FuncDataTemplate<UnifiedTreeNode>((node, _) => BuildNodeNameControl(node)),
                            null),
                        node => node.Children
                    ),
                    new TemplateColumn<UnifiedTreeNode>("详细信息",
                        new FuncDataTemplate<UnifiedTreeNode>((node, _) => BuildDetailControl(node)),
                        new FuncDataTemplate<UnifiedTreeNode>((node, _) => BuildDetailControl(node)),
                        null),
                    new TemplateColumn<UnifiedTreeNode>("状态",
                        new FuncDataTemplate<UnifiedTreeNode>((node, _) => BuildStatusControl(node)),
                        new FuncDataTemplate<UnifiedTreeNode>((node, _) => BuildStatusControl(node)),
                        null),
                }
            };

            TreeSource.RowSelection!.SingleSelect = true;
            TreeSource.RowSelection.SelectionChanged += (s, e) =>
            {
                SelectedTreeItem = TreeSource.RowSelection.SelectedItem;
            };
        }

        // 构建第一列控件（使用 Binding 或直接订阅事件，避免 GetObservable 依赖）
        private Control BuildNodeNameControl(UnifiedTreeNode node)
        {
            var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
            var tb = new TextBox();
            switch (node.Type)
            {
                case NodeType.Rule:
                    panel.Children.Add(new TextBlock { Text = $"规则 [{node.Id}]" });
                    tb.Text = node.Name;
                    tb.TextChanged += (s, e) => node.Name = tb.Text;
                    panel.Children.Add(tb);
                    break;
                case NodeType.Condition:
                    panel.Children.Add(new TextBlock { Text = $"条件 [{node.Id}]" });
                    tb.Text = node.Name;
                    tb.TextChanged += (s, e) => node.Name = tb.Text;
                    panel.Children.Add(tb);
                    break;
                case NodeType.Detail:
                    panel.Children.Add(new TextBlock { Text = $"明细 [{node.Id}]" });
                    tb.Text = node.Name;
                    tb.TextChanged += (s, e) => node.Name = tb.Text;
                    panel.Children.Add(tb);
                    break;
            }
            return panel;
        }

        private Control BuildDetailControl(UnifiedTreeNode node)
        {
            if (node.Type == NodeType.Condition)
            {
                var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 4 };
                var opCombo = new ComboBox { ItemsSource = Operators, SelectedItem = node.Operator, Width = 60 };
                opCombo.SelectionChanged += (s, e) => node.Operator = opCombo.SelectedItem?.ToString();
                panel.Children.Add(opCombo);

                var valBox = new TextBox { Text = node.Value.ToString(), Width = 80 };
                valBox.TextChanged += (s, e) =>
                {
                    if (decimal.TryParse(valBox.Text, out var dec)) node.Value = dec;
                };
                panel.Children.Add(valBox);

                var unitBox = new TextBox { Text = node.Unit, Width = 60 };
                unitBox.TextChanged += (s, e) => node.Unit = unitBox.Text;
                panel.Children.Add(unitBox);
                return panel;
            }
            if (node.Type == NodeType.Detail)
            {
                var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 4 };
                var valBox = new TextBox { Text = node.AttrValue, Width = 100 };
                valBox.TextChanged += (s, e) => node.AttrValue = valBox.Text;
                panel.Children.Add(valBox);

                var unitBox = new TextBox { Text = node.AttrUnit, Width = 60 };
                unitBox.TextChanged += (s, e) => node.AttrUnit = unitBox.Text;
                panel.Children.Add(unitBox);
                return panel;
            }
            return new TextBlock();
        }

        private Control BuildStatusControl(UnifiedTreeNode node)
        {
            if (node.Type == NodeType.Rule)
            {
                var chk = new CheckBox { IsChecked = node.IsActive };
                chk.IsCheckedChanged += (s, e) => node.IsActive = chk.IsChecked ?? false;
                return chk;
            }
            return new TextBlock();
        }
        #endregion

        #region HTTP 请求方法（保持不变）
        private async Task<List<ProductRuleDto>> LoadProductRulesAsync()
        {
            var request = new ClientRequest { Url = "ProductRule", Method = Method.Get, ContentType = "application/json" };
            var response = await _httpRestClient.ExecuteAsync<List<ProductRuleDto>>(request);
            return response.IsSuccess && response.Data != null ? response.Data : new List<ProductRuleDto>();
        }

        private async Task<List<RuleConditionDto>> LoadConditionsByRuleId(int ruleId)
        {
            var request = new ClientRequest { Url = $"RuleCondition/byrule/{ruleId}", Method = Method.Get };
            var response = await _httpRestClient.ExecuteAsync<List<RuleConditionDto>>(request);
            return response.IsSuccess ? response.Data ?? new List<RuleConditionDto>() : new List<RuleConditionDto>();
        }

        private async Task<List<RuleDetailDto>> LoadDetailsByRuleId(int ruleId)
        {
            var request = new ClientRequest { Url = $"RuleDetail/byrule/{ruleId}", Method = Method.Get };
            var response = await _httpRestClient.ExecuteAsync<List<RuleDetailDto>>(request);
            return response.IsSuccess ? response.Data ?? new List<RuleDetailDto>() : new List<RuleDetailDto>();
        }
        #endregion

        #region 命令实现
        private void OnAddRule()
        {
            if (_rootNodes == null) _rootNodes = new ObservableCollection<UnifiedTreeNode>();
            var newNode = new UnifiedTreeNode { Type = NodeType.Rule, Id = 0, Name = "新规则", IsActive = true };
            _rootNodes.Add(newNode);
            RebuildTreeSource();  // 刷新UI
            SelectedTreeItem = newNode;
            StatusText = "请填写规则内容后点击【保存全部】";
        }

        private async void OnSaveAllAsync()
        {
            if (_rootNodes == null) return;
            StatusText = "正在保存...";
            foreach (var ruleNode in _rootNodes.ToList())
            {
                // 保存规则
                if (ruleNode.Id == 0)
                {
                    var createDto = new CreateProductRuleDto { ProductCode = "NEW_CODE", RuleName = ruleNode.Name, IsActive = ruleNode.IsActive };
                    var request = new ClientRequest { Url = "ProductRule", Method = Method.Post, Parameters = createDto };
                    var response = await _httpRestClient.ExecuteAsync<ProductRuleDto>(request);
                    if (response.IsSuccess && response.Data != null) ruleNode.Id = response.Data.RuleId;
                    else { StatusText = $"保存规则失败"; return; }
                }
                else
                {
                    var updateDto = new UpdateProductRuleDto { ProductCode = null, RuleName = ruleNode.Name, IsActive = ruleNode.IsActive };
                    var request = new ClientRequest { Url = $"ProductRule/{ruleNode.Id}", Method = Method.Put, Parameters = updateDto };
                    await _httpRestClient.ExecuteAsync<ProductRuleDto>(request);
                }

                // 保存条件
                foreach (var condNode in ruleNode.Children.Where(c => c.Type == NodeType.Condition).ToList())
                {
                    if (condNode.Id == 0)
                    {
                        var createDto = new CreateRuleConditionDto
                        {
                            RuleId = ruleNode.Id,
                            ConditionName = condNode.Name,
                            Operator = condNode.Operator,
                            Value = condNode.Value,
                            Unit = condNode.Unit
                        };
                        var request = new ClientRequest { Url = "RuleCondition", Method = Method.Post, Parameters = createDto };
                        var response = await _httpRestClient.ExecuteAsync<RuleConditionDto>(request);
                        if (response.IsSuccess && response.Data != null)
                        {
                            condNode.Id = response.Data.ConditionId;
                            foreach (var det in condNode.Children)
                                det.ConditionId = condNode.Id;
                        }
                        else { StatusText = $"保存条件失败"; return; }
                    }
                    else
                    {
                        var updateDto = new UpdateRuleConditionDto
                        {
                            ConditionName = condNode.Name,
                            Operator = condNode.Operator,
                            Value = condNode.Value,
                            Unit = condNode.Unit
                        };
                        var request = new ClientRequest { Url = $"RuleCondition/{condNode.Id}", Method = Method.Put, Parameters = updateDto };
                        await _httpRestClient.ExecuteAsync<RuleConditionDto>(request);
                    }

                    // 保存明细
                    foreach (var detNode in condNode.Children.Where(d => d.Type == NodeType.Detail).ToList())
                    {
                        if (detNode.Id == 0)
                        {
                            var createDto = new CreateRuleDetailDto
                            {
                                RuleId = ruleNode.Id,
                                ConditionId = condNode.Id == 0 ? null : condNode.Id,
                                ClassId = detNode.ClassId,
                                AttrName = detNode.Name ?? "",
                                AttrValue = detNode.AttrValue ?? "",
                                AttrUnit = detNode.AttrUnit
                            };
                            var request = new ClientRequest { Url = "RuleDetail", Method = Method.Post, Parameters = createDto };
                            var response = await _httpRestClient.ExecuteAsync<RuleDetailDto>(request);
                            if (response.IsSuccess && response.Data != null) detNode.Id = response.Data.DetailId;
                        }
                        else
                        {
                            var updateDto = new UpdateRuleDetailDto
                            {
                                ConditionId = condNode.Id == 0 ? null : condNode.Id,
                                AttrName = detNode.Name,
                                AttrValue = detNode.AttrValue,
                                AttrUnit = detNode.AttrUnit
                            };
                            var request = new ClientRequest { Url = $"RuleDetail/{detNode.Id}", Method = Method.Put, Parameters = updateDto };
                            await _httpRestClient.ExecuteAsync<RuleDetailDto>(request);
                        }
                    }
                }
            }
            StatusText = "全部保存成功";
            await LoadAllDataAsync();  // 重新加载，刷新ID等
        }

        private void OnAddCondition()
        {
            if (SelectedTreeItem is UnifiedTreeNode node && node.Type == NodeType.Rule)
            {
                var newCond = new UnifiedTreeNode
                {
                    Type = NodeType.Condition,
                    Id = 0,
                    Name = "新条件",
                    Operator = "<=",
                    Value = 0,
                    Unit = ""
                };
                node.Children.Add(newCond);
                RebuildTreeSource();   // 刷新树
                SelectedTreeItem = newCond;
                StatusText = "请填写条件内容后保存";
            }
        }

        private void OnAddDetail()
        {
            if (SelectedTreeItem is UnifiedTreeNode node && node.Type == NodeType.Condition)
            {
                var newDetail = new UnifiedTreeNode
                {
                    Type = NodeType.Detail,
                    Id = 0,
                    Name = "新属性",
                    ClassId = 0,
                    AttrValue = "",
                    AttrUnit = ""
                };
                node.Children.Add(newDetail);
                RebuildTreeSource();
                SelectedTreeItem = newDetail;
                StatusText = "请填写明细内容后保存";
            }
        }

        private async void OnDeleteSelected()
        {
            if (SelectedTreeItem is not UnifiedTreeNode node) return;

            if (node.Type == NodeType.Rule)
            {
                if (node.Id > 0)
                {
                    var request = new ClientRequest { Url = $"ProductRule/{node.Id}", Method = Method.Delete };
                    await _httpRestClient.ExecuteAsync<object>(request);
                }
                _rootNodes?.Remove(node);
                RebuildTreeSource();
                StatusText = "已删除规则";
            }
            else if (node.Type == NodeType.Condition)
            {
                var parentRule = FindParentRule(node);
                if (parentRule != null)
                {
                    if (node.Id > 0)
                    {
                        var request = new ClientRequest { Url = $"RuleCondition/{node.Id}", Method = Method.Delete };
                        await _httpRestClient.ExecuteAsync<object>(request);
                    }
                    parentRule.Children.Remove(node);
                    RebuildTreeSource();
                }
                StatusText = "已删除条件";
            }
            else if (node.Type == NodeType.Detail)
            {
                var parentCond = FindParentCondition(node);
                if (parentCond != null)
                {
                    if (node.Id > 0)
                    {
                        var request = new ClientRequest { Url = $"RuleDetail/{node.Id}", Method = Method.Delete };
                        await _httpRestClient.ExecuteAsync<object>(request);
                    }
                    parentCond.Children.Remove(node);
                    RebuildTreeSource();
                }
                StatusText = "已删除明细";
            }
        }

        private UnifiedTreeNode? FindParentRule(UnifiedTreeNode cond)
        {
            if (_rootNodes == null) return null;
            foreach (var rule in _rootNodes)
                if (rule.Children.Contains(cond)) return rule;
            return null;
        }

        private UnifiedTreeNode? FindParentCondition(UnifiedTreeNode detail)
        {
            if (_rootNodes == null) return null;
            foreach (var rule in _rootNodes)
                foreach (var cond in rule.Children.Where(c => c.Type == NodeType.Condition))
                    if (cond.Children.Contains(detail)) return cond;
            return null;
        }

        private bool CanAddCondition() => SelectedTreeItem is UnifiedTreeNode n && n.Type == NodeType.Rule;
        private bool CanAddDetail() => SelectedTreeItem is UnifiedTreeNode n && n.Type == NodeType.Condition;
        private bool CanDeleteSelected() => SelectedTreeItem is UnifiedTreeNode;
        #endregion
    }
}