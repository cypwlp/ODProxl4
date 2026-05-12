
namespace ODProxl.ViewModels.Pages
{
    public class AnnotationPageViewModel : BindableBase, INavigationAware
    {
        #region INavigationAware成員
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
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
