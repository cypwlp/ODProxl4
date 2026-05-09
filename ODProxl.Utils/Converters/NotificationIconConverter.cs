using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;
using ODProxl.Utils.Events;

namespace ODProxl.Utils.Converters
{
    public class NotificationIconConverter : IValueConverter
    {
        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        )
        {
            if (value is NotificationType type)
            {
                return type switch
                {
                    NotificationType.Success => MaterialIconKind.CheckCircle,
                    NotificationType.Error => MaterialIconKind.AlertCircle,
                    NotificationType.Warning => MaterialIconKind.Alert,
                    NotificationType.Info => MaterialIconKind.Information,
                    _ => MaterialIconKind.Information,
                };
            }
            return MaterialIconKind.Information;
        }

        public object? ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture
        ) => throw new NotImplementedException();
    }
}
