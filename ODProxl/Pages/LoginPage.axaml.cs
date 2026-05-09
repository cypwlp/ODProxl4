using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ODProxl.ViewModels.Pages;

namespace ODProxl.Pages;

public partial class LoginPage : UserControl
{
    public LoginPage()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // 進入頁面後自動聚焦用戶名輸入框
        Dispatcher.UIThread.Post(() => UserNameTextBox?.Focus(), DispatcherPriority.Background);
    }

    private void UserNameTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PasswordTextBox?.Focus();
            e.Handled = true;
        }
    }

    private void PasswordTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (DataContext is LoginPageViewModel vm)
                vm.LoginCommand?.Execute();
            e.Handled = true;
        }
    }
}
