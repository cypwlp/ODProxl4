
namespace ODProxl.ViewModels.Pages
{
    public class CloudDiskPageViewModel : BindableBase, INavigationAware
    {
        #region INavigationAware implementation
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {

        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {

        }
        #endregion

    }
}
