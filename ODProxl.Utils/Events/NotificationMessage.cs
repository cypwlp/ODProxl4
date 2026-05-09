using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ODProxl.Utils.Events;

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
}

public class NotificationMessage
{
    public string Message { get; }
    public NotificationType Type { get; }
    public string? ActionText { get; }
    public ICommand? ActionCommand { get; }

    public NotificationMessage(
        string message,
        NotificationType type = NotificationType.Info,
        string? actionText = null,
        ICommand? actionCommand = null
    )
    {
        Message = message;
        Type = type;
        ActionText = actionText;
        ActionCommand = actionCommand;
    }
}

public partial class NotificationItem : ObservableObject
{
    public string Message { get; }
    public NotificationType Type { get; }
    public int DurationMs { get; } = 4000;
    public ICommand? ActionCommand { get; }
    public string? ActionText { get; }

    public NotificationItem(
        string message,
        NotificationType type,
        string? actionText = null,
        ICommand? actionCommand = null
    )
    {
        Message = message;
        Type = type;
        ActionText = actionText;
        ActionCommand = actionCommand;
    }
}

public partial class NotificationService : BindableBase
{
    public ObservableCollection<NotificationItem> Notifications { get; } = new();

    public void Show(
        string message,
        NotificationType type = NotificationType.Info,
        string? actionText = null,
        ICommand? action = null,
        int durationMs = 4000
    )
    {
        Dispatcher.UIThread.Post(() =>
        {
            var item = new NotificationItem(message, type, actionText, action);
            Notifications.Add(item);

            if (durationMs > 0)
            {
                _ = Task.Delay(durationMs)
                    .ContinueWith(
                        _ =>
                        {
                            Dispatcher.UIThread.Post(() => Notifications.Remove(item));
                        },
                        TaskScheduler.Default
                    );
            }
        });
    }

    [RelayCommand]
    private void Dismiss(NotificationItem item) => Notifications.Remove(item);
}
