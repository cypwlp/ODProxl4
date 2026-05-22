using Avalonia;
using ODProxl.ClientServices;
using ODProxl.ClientServices.Impls;
using ODProxl.Dialogs;
using ODProxl.Global.Servcies;
using ODProxl.Global.Servcies.impls;
using ODProxl.Global.Services;
using ODProxl.Global.Services.impls;
using ODProxl.Pages;
using ODProxl.Utils.HttpService;
using ODProxl.ViewModels.Dialogs;
using ODProxl.ViewModels.Pages;
using ODProxl.ViewModels.Pages.AnnotationPageViewModels;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Navigation.Regions;
using RestSharp;
using System.Net.Http;

namespace ODProxl
{
    public partial class App : PrismApplication
    {
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IAuthService, AuthService>();
            containerRegistry.RegisterSingleton<IConfigManager, ConfigManager>();
            containerRegistry.RegisterSingleton<IFileManager, FileManager>();
            containerRegistry.RegisterSingleton<ISignalRService, SignalRService>();
            containerRegistry.RegisterSingleton<HttpClient>();
            containerRegistry.RegisterSingleton<IHttpRestClient>(sp => new HttpRestClient(
                new RestClient(),
                //"http://interior.topmix.net/Info/System/SoftWare/ODApi/api/",
                "https://localhost:44364/api/",
                sp.Resolve<IAuthService>()
            ));

            containerRegistry.RegisterForNavigation<LoginPage, LoginPageViewModel>();
            containerRegistry.RegisterForNavigation<HomePage, HomePageViewModel>();
            containerRegistry.RegisterForNavigation<MainWin, MainWinViewModel>();
            containerRegistry.RegisterForNavigation<OnnxModelPage, OnnxModelPageViewModel>();
            containerRegistry.RegisterForNavigation<ModelClassDialog, ModelClassDialogViewModel>();
            containerRegistry.RegisterForNavigation<AnnotationPage, AnnotationPageViewModel>();
            containerRegistry.RegisterForNavigation<RuleMakingPage, RuleMakingPageViewModel>();
            containerRegistry.RegisterForNavigation<ProductPage, ProductPageViewModel>();
            containerRegistry.RegisterForNavigation<ProductGroupPage, ProductGroupPageViewModel>();
            containerRegistry.RegisterForNavigation<UserConfigPage, UserConfigPageViewModel>();
            containerRegistry.RegisterForNavigation<RuleClassPage, RuleClassPageViewModel>();
            containerRegistry.RegisterDialog<AddOrEditProductDialog, AddOrEditProductDialogViewModel>();
            containerRegistry.RegisterDialog<RevisedRulesDialog, RevisedRulesDialogViewModel>();
            containerRegistry.RegisterDialog<RevisionDetailsDialog, RevisionDetailsDialogViewModel>();
            containerRegistry.RegisterDialog<RevisionConditionsDialog, RevisionConditionsDialogViewModel>();
            containerRegistry.RegisterDialog<ReviseProductGroupDialog, ReviseProductGroupDialogViewModel>();
            containerRegistry.RegisterDialog<ReviseConfigDialog, ReviseConfigDialogViewModel>();
            containerRegistry.RegisterDialog<ReviseRuleClassDialog, ReviseRuleClassDialogViewModel>();
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