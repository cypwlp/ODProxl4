using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Material.Icons;
using Material.Icons.Avalonia;
using ODProxl.ViewModels.Dialogs;

namespace ODProxl.Dialogs;

public partial class LoginDialog : UserControl
{
    public LoginDialog()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Prism 會把 LoginDialog (UserControl) 包裝在一個獨立 Window 中顯示
        // 在這裡配置宿主窗口的外觀
        if (VisualRoot is Window window)
        {
            window.SystemDecorations = SystemDecorations.None;
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.CanResize = false;
            window.Width = 420;
            window.Height = 580;
            window.ShowInTaskbar = true;
            window.Background = Avalonia.Media.Brushes.Transparent;
            window.TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };

            window.Opened += (_, _) =>
            {
                window.Activate();
                Dispatcher.UIThread.Post(() =>
                {
                    UserNameTextBox?.Focus();
                });
            };
        }
    }

    private void BtnMin_Click(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is Window window)
            window.WindowState = WindowState.Minimized;
    }

    private void BtnMax_Click(object? sender, RoutedEventArgs e) => ToggleMaximize();

    private void BtnClose_Click(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is Window window)
            window.Close();
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
            if (DataContext is LoginDialogViewModel vm)
                vm.LoginCommand?.Execute();
            e.Handled = true;
        }
    }

    private void ToggleMaximize()
    {
        if (VisualRoot is Window window)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                window.WindowState = WindowState.Normal;
                if (this.FindControl<MaterialIcon>("MaxIcon") is MaterialIcon icon)
                    icon.Kind = MaterialIconKind.WindowMaximize;
            }
            else
            {
                window.WindowState = WindowState.Maximized;
                if (this.FindControl<MaterialIcon>("MaxIcon") is MaterialIcon icon)
                    icon.Kind = MaterialIconKind.WindowRestore;
            }
        }
    }
}