using Avalonia;
using ODProxl.ClientServices;
using ODProxl.ClientServices.Impls;
using ODProxl.Dialogs;
using ODProxl.Pages;
using ODProxl.Utils.HttpService;
using ODProxl.ViewModels.Dialogs;
using ODProxl.ViewModels.Pages;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Navigation.Regions;
using RestSharp;

namespace ODProxl
{
    public partial class App : PrismApplication
    {
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IAuthService, AuthService>();
            containerRegistry.RegisterSingleton<IHttpRestClient>(sp => new HttpRestClient(
                new RestClient(),
                "https://localhost:44364/api/",
                sp.Resolve<IAuthService>()
            ));

            containerRegistry.RegisterForNavigation<LoginPage, LoginPageViewModel>();
            containerRegistry.RegisterForNavigation<HomePage, HomePageViewModel>();
            containerRegistry.RegisterForNavigation<MainWin, MainWinViewModel>();
            containerRegistry.RegisterForNavigation<OnnxModelPage, OnnxModelPageViewModel>();
            containerRegistry.RegisterForNavigation<ModelClassDialog, ModelClassDialogViewModel>();
        }

        protected override AvaloniaObject CreateShell()
        {
            var mainWin = Container.Resolve<MainWin>();
            mainWin.DataContext = Container.Resolve<MainWinViewModel>();
            return mainWin;
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate("MainRegion", "LoginPage");
        }
    }
}