using Avalonia.Controls;
using Avalonia.Controls.Selection;
using ODProxl.TreeNodes;
using ODProxl.ViewModels.Pages;
using System;

namespace ODProxl.Pages
{
    public partial class RuleClassPage : UserControl
    {
        public RuleClassPage()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            if (DataContext is RuleClassPageViewModel vm)
            {
                vm.OnTreeSourceReady = () =>
                {
                    if (RuleClassTreeGrid.RowSelection != null)
                    {
                        RuleClassTreeGrid.RowSelection.SelectionChanged += OnTreeGridSelectionChanged;
                    }
                };
            }
        }

        private void OnTreeGridSelectionChanged(object? sender, TreeSelectionModelSelectionChangedEventArgs e)
        {
            if (DataContext is RuleClassPageViewModel vm)
            {
                vm.SelectedRuleClass = RuleClassTreeGrid.RowSelection?.SelectedItem as RuleClassTreeNode;
            }
        }
    }
}