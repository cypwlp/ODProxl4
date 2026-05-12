using Avalonia;
using Avalonia.Controls;

namespace ODProxl.Utils.Extends
{
    public abstract class DialogBase : UserControl
    {
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (this.VisualRoot is not Window window)
                return;

            var screen = TopLevel.GetTopLevel(this)?.Screens.Primary?.WorkingArea
                         ?? new PixelRect(0, 0, 1280, 800);

            window.Width = Math.Clamp(screen.Width * 0.85, 1000, 1680);
            window.Height = Math.Clamp(screen.Height * 0.85, 650, 1080);

            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.MinWidth = 900;
            window.MinHeight = 600;
        }
    }
}
