namespace ODProxl.ViewModels.Pages
{
    public class UserConfigPageViewModel : BindableBase, INavigationAware
    {
        #region INavigation 接口
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
