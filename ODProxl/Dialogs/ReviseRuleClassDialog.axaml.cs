using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ODProxl.ViewModels.Dialogs;
using System;

namespace ODProxl.Dialogs
{
    public partial class ReviseRuleClassDialog : UserControl
    {
        public ReviseRuleClassDialog()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }


        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                // 禁止調整大小 → 連帶禁用最大化按鈕
                window.CanResize = false;

                // 確保打開時是正常大小，而不是最大化
                window.WindowState = WindowState.Normal;

                // 居中於父視窗
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
        }

        private void OnImageDoubleTapped(object? sender, TappedEventArgs e)
        {
            e.Handled = true;
            if (sender is Image image && image.Source is Bitmap bmp)
            {
                ShowFullScreenImage(bmp);
            }
        }

        private async void ShowFullScreenImage(Bitmap bitmap)
        {
            try
            {
                var ownerWindow = TopLevel.GetTopLevel(this) as Window;
                if (ownerWindow == null) return;

                var window = new Window
                {
                    Width = 1200,
                    Height = 800,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new Image
                    {
                        Source = bitmap,
                        Stretch = Avalonia.Media.Stretch.Uniform,
                        Margin = new Avalonia.Thickness(20)
                    },
                    Title = "圖片預覽"
                };
                await window.ShowDialog(ownerWindow);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"放大圖片失敗: {ex.Message}");
            }
        }

        private async void OnSelectFileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            var options = new FilePickerOpenOptions
            {
                Title = "選擇圖片文件",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("圖片文件")
                    {
                        Patterns = new[] { "*.jpg", "*.png", "*.jpeg", "*.gif", "*.bmp" }
                    },
                    new FilePickerFileType("所有文件")
                    {
                        Patterns = new[] { "*" }
                    }
                }
            };

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);
            if (files.Count >= 1)
            {
                var uri = files[0].Path;
                string localPath = uri.LocalPath;
                if (DataContext is ReviseRuleClassDialogViewModel vm)
                {
                    await vm.UploadFileAsync(localPath);
                }
            }
        }
    }
}