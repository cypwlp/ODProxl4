using Avalonia.Controls;
using Avalonia.Input;

namespace ODProxl.Pages
{
    public partial class RuleMakingPage : UserControl
    {
        public RuleMakingPage()
        {
            InitializeComponent();
            MainTreeGrid.DoubleTapped += OnTreeGridDoubleTapped;
        }

        private void OnTreeGridDoubleTapped(object? sender, TappedEventArgs e)
        {
            if (DataContext is ViewModels.Pages.RuleMakingPageViewModel vm && vm.EditSelectedCommand.CanExecute())
            {
                vm.EditSelectedCommand.Execute();
            }
        }
    }
}