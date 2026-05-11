namespace ODProxl.ViewModels.Pages;

public class HomePageViewModel : BindableBase, INavigationAware
{
    #region  INavigationAware members

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
      
    }

    public bool IsNavigationTarget(NavigationContext navigationContext)
    {
        return true;
    }

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    
    }

    #endregion
}
