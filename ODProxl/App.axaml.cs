using Avalonia;
using Avalonia.Markup.Xaml;
using ODProxl.ClientServices;
using ODProxl.ClientServices.Impls;
using ODProxl.Pages;
using ODProxl.Utils.HttpService;
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
            containerRegistry.Register<HttpRestClient>(() => new HttpRestClient(new RestClient()));

            // 頁面註冊
            containerRegistry.RegisterForNavigation<LoginPage, LoginPageViewModel>();
            containerRegistry.RegisterForNavigation<HomePage, HomePageViewModel>();
            containerRegistry.RegisterForNavigation<MainWin, MainWinViewModel>();
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

            // Shell 已就緒，Region 已註冊，在這裡直接導航
            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate("MainRegion", "LoginPage");
        }
    }
}
