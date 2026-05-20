using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ODProxl.ViewModels.Dialogs;

namespace ODProxl.Dialogs
{
    public partial class ReviseRuleClassDialog : UserControl
    {
        public ReviseRuleClassDialog()
        {
            InitializeComponent();
        }

        private async void OnSelectFileClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
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
                var filePath = files[0].Path.AbsolutePath;
                if (DataContext is ReviseRuleClassDialogViewModel vm)
                {
                    await vm.UploadFileAsync(filePath);
                }
            }
        }
    }
}