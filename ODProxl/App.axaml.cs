using Avalonia;
using Avalonia.Markup.Xaml;
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
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            base.Initialize();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 全局服務（單例）
            containerRegistry.RegisterSingleton<IAuthService, AuthService>();
            containerRegistry.RegisterSingleton<IHttpRestClient>(sp => new HttpRestClient(
                new RestClient(),
                "https://localhost:44364/api/",
                sp.Resolve<IAuthService>()
            ));

            // 頁面註冊
            containerRegistry.RegisterForNavigation<LoginPage, LoginPageViewModel>();
            containerRegistry.RegisterForNavigation<HomePage, HomePageViewModel>();
            containerRegistry.RegisterForNavigation<MainWin, MainWinViewModel>();
            containerRegistry.RegisterForNavigation<OnnxModelPage, OnnxModelPageViewModel>();
            containerRegistry.RegisterForNavigation<ModelClassDialog, ModelClassDialogViewModel>();
            // 其他頁面陸續加在這裡
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
