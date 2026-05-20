using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using ODProxl.ClientDtos;
using ODProxl.TreeNodes;
using ODProxl.Utils.HttpService;
using RestSharp;
using System.Collections.ObjectModel;

namespace ODProxl.ViewModels.Pages
{
    public class RuleClassPageViewModel : BindableBase, INavigationAware
    {
        private readonly IHttpRestClient _httpRestClient;
        private readonly IDialogService _dialogService;
        private readonly IEventAggregator _eventAggregator;

        private ObservableCollection<RuleClassTreeNode>? _ruleClassTreeNodes;
        private HierarchicalTreeDataGridSource<RuleClassTreeNode>? _treeSource;
        private string _searchText = string.Empty;
        private RuleClassTreeNode? _selectedRuleClass;

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public RuleClassTreeNode? SelectedRuleClass
        {
            get => _selectedRuleClass;
            set => SetProperty(ref _selectedRuleClass, value);
        }

        public DelegateCommand SearchCommand { get; }
        public DelegateCommand AddNewRuleClassCommand { get; }
        public DelegateCommand EditRuleClassCommand { get; }

        public RuleClassPageViewModel(IHttpRestClient httpRestClient, IDialogService dialogService, IEventAggregator eventAggregator)
        {
            _httpRestClient = httpRestClient;
            _dialogService = dialogService;
            _eventAggregator = eventAggregator;

            SearchCommand = new DelegateCommand(OnSearch);
            // 修改1：传入当前选中节点作为父节点
            AddNewRuleClassCommand = new DelegateCommand(async () => await ShowRuleClassDialogAsync(null, SelectedRuleClass));
            EditRuleClassCommand = new DelegateCommand(async () => await ShowRuleClassDialogAsync(SelectedRuleClass));
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) => true;

        public void OnNavigatedFrom(NavigationContext navigationContext) { }

        public async void OnNavigatedTo(NavigationContext navigationContext)
        {
            await InitializeRuleClassAsync();
        }

        public ObservableCollection<RuleClassTreeNode>? RuleClassTreeNodes
        {
            get => _ruleClassTreeNodes;
            set => SetProperty(ref _ruleClassTreeNodes, value);
        }

        public HierarchicalTreeDataGridSource<RuleClassTreeNode>? TreeSource
        {
            get => _treeSource;
            private set => SetProperty(ref _treeSource, value);
        }

        private async Task InitializeRuleClassAsync()
        {
            var request = new ClientRequest { Url = "RuleClass", Method = Method.Get, ContentType = "application/json" };
            var response = await _httpRestClient.ExecuteAsync<List<RuleClassDto>>(request);

            if (!response.IsSuccess || response.Data == null) return;

            var fileIds = response.Data.Select(dto => dto.FileId).Distinct().ToList();
            Dictionary<int, string> fileUrlDict = new Dictionary<int, string>();
            if (fileIds.Count > 0)
            {
                var idsParam = string.Join(",", fileIds);
                var fileRequest = new ClientRequest { Url = $"File/name-urls?ids={idsParam}", Method = Method.Get };
                var fileResponse = await _httpRestClient.ExecuteAsync<ClientResponse<Dictionary<int, object>>>(fileRequest);
                if (fileResponse.IsSuccess && fileResponse.Data?.Data != null)
                {
                    foreach (var kvp in fileResponse.Data.Data)
                    {
                        if (kvp.Value is IDictionary<string, object> fileInfo && fileInfo.ContainsKey("url"))
                            fileUrlDict[kvp.Key] = fileInfo["url"]?.ToString() ?? string.Empty;
                    }
                }
            }

            var allNodes = response.Data.Select(dto => new RuleClassTreeNode
            {
                RuleClassId = dto.RuleClassId,
                RuleClassKey = dto.RuleClassKey ?? string.Empty,
                RuleClassName = dto.RuleClassName ?? string.Empty,
                FileId = dto.FileId,
                FileUrl = fileUrlDict.GetValueOrDefault(dto.FileId, string.Empty),
                CreatedBy = dto.CreatedBy,
                CreatedTime = dto.CreatedTime,
                UpdatedBy = dto.UpdatedBy,
                UpdatedTime = dto.UpdatedTime
            }).ToList();

            var rootNodes = BuildTree(allNodes, response.Data);
            RuleClassTreeNodes = new ObservableCollection<RuleClassTreeNode>(rootNodes);
            BuildTreeSource();
        }

        private List<RuleClassTreeNode> BuildTree(List<RuleClassTreeNode> allNodes, List<RuleClassDto> dtos)
        {
            var nodeDict = allNodes.ToDictionary(n => n.RuleClassId);
            var rootNodes = new List<RuleClassTreeNode>();

            foreach (var dto in dtos)
            {
                var node = nodeDict[dto.RuleClassId];
                if (dto.ParentRuleClassId == 0 || !nodeDict.ContainsKey(dto.ParentRuleClassId))
                {
                    rootNodes.Add(node);
                }
                else
                {
                    var parent = nodeDict[dto.ParentRuleClassId];
                    parent.Children.Add(node);
                }
            }

            return rootNodes;
        }

        private void BuildTreeSource()
        {
            if (RuleClassTreeNodes == null) return;

            TreeSource = new HierarchicalTreeDataGridSource<RuleClassTreeNode>(RuleClassTreeNodes)
            {
                Columns =
                {
                    new HierarchicalExpanderColumn<RuleClassTreeNode>(
                        new TextColumn<RuleClassTreeNode, int>("類別ID", x => x.RuleClassId, width: new GridLength(80)),
                        x => x.Children),
                    new TextColumn<RuleClassTreeNode, string>("類別鍵值", x => x.RuleClassKey, width: new GridLength(120)),
                    new TextColumn<RuleClassTreeNode, string>("類別名稱", x => x.RuleClassName, width: new GridLength(1, GridUnitType.Star)),
                    new TextColumn<RuleClassTreeNode, string>("參考圖片", x => x.FileUrl, width: new GridLength(200)),
                    new TextColumn<RuleClassTreeNode, string?>("創建人", x => x.CreatedBy, width: GridLength.Auto),
                    new TextColumn<RuleClassTreeNode, DateTime>("創建時間", x => x.CreatedTime, width: new GridLength(150)),
                    new TextColumn<RuleClassTreeNode, string?>("修改人", x => x.UpdatedBy, width: new GridLength(120)),
                    new TextColumn<RuleClassTreeNode, DateTime>("修改時間", x => x.UpdatedTime, width: new GridLength(150))
                }
            };

            TreeSource.RowSelection!.SingleSelect = true;
        }

        private void OnSearch()
        {
            // 实现搜索逻辑
        }

        // 修改2：增加 parentNode 参数
        private async Task ShowRuleClassDialogAsync(RuleClassTreeNode? ruleClass, RuleClassTreeNode? parentNode = null)
        {
            IDialogResult result;
            if (ruleClass == null)
            {
                // 新增时，标题可以加上父节点提示
                string title = parentNode == null ? "新增規則類別" : $"新增子類別（父：{parentNode.RuleClassName}）";
                result = await _dialogService.ShowDialogAsync("ReviseRuleClassDialog", new DialogParameters { { "Title", title } });
            }
            else
            {
                var parameters = new DialogParameters
                {
                    { "Title", "修改規則類別" },
                    { "RuleClassName", ruleClass.RuleClassName },
                    { "RuleClassKey", ruleClass.RuleClassKey },
                    { "FileId", ruleClass.FileId },
                    { "FileUrl", ruleClass.FileUrl }
                };
                result = await _dialogService.ShowDialogAsync("ReviseRuleClassDialog", parameters);
            }

            if (result.Result != ButtonResult.OK || result.Parameters == null) return;

            var ruleClassName = result.Parameters.GetValue<string>("RuleClassName");
            var ruleClassKey = result.Parameters.GetValue<string>("RuleClassKey");
            var ruleClassFileId = result.Parameters.GetValue<int>("FileId");

            if (ruleClass == null)
            {
                var createDto = new CreateRuleClassDto
                {
                    RuleClassName = ruleClassName,
                    RuleClassKey = ruleClassKey,
                    FileId = ruleClassFileId,
                    ParentRuleClassId = parentNode?.RuleClassId ?? 0  // 修改3：设置父ID
                };
                var createRequest = new ClientRequest { Url = "RuleClass", Method = Method.Post, Parameters = createDto };
                await _httpRestClient.ExecuteAsync<RuleClassDto>(createRequest);
            }
            else
            {
                var updateDto = new UpdateRuleClassDto
                {
                    RuleClassName = ruleClassName,
                    RuleClassKey = ruleClassKey,
                    FileId = ruleClassFileId
                };
                var updateRequest = new ClientRequest { Url = $"RuleClass/{ruleClass.RuleClassId}", Method = Method.Put, Parameters = updateDto };
                await _httpRestClient.ExecuteAsync<RuleClassDto>(updateRequest);
            }

            await InitializeRuleClassAsync();
        }
    }
}