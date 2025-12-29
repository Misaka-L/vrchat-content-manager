using System.Globalization;
using Avalonia.Data.Converters;
using VRChatContentPublisher.Core.Models;

namespace VRChatContentPublisher.App.Converters;

public sealed class CanContentPublishTaskRemoveConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ContentPublishTaskStatus status)
            return false;

        return status is
            ContentPublishTaskStatus.Failed or
            ContentPublishTaskStatus.Canceled or
            ContentPublishTaskStatus.Completed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}