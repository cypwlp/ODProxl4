using Avalonia.Controls;
using Avalonia.Controls.Selection;
using Avalonia.Interactivity;
using ODProxl.TreeNodes;
using ODProxl.ViewModels.Pages;

namespace ODProxl.Pages
{
    public partial class RuleClassPage : UserControl
    {
        public RuleClassPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (RuleClassTreeGrid.RowSelection != null)
            {
                RuleClassTreeGrid.RowSelection.SelectionChanged += OnTreeGridSelectionChanged;
            }
        }

        private void OnTreeGridSelectionChanged(object sender, TreeSelectionModelSelectionChangedEventArgs e)
        {
            if (DataContext is RuleClassPageViewModel vm)
            {
                vm.SelectedRuleClass = RuleClassTreeGrid.RowSelection?.SelectedItem as RuleClassTreeNode;
            }
        }
    }
}