using System.Globalization;
using Avalonia.Data.Converters;
using VRChatContentPublisher.Core.Models;

namespace VRChatContentPublisher.App.Converters;

public sealed class IsContentPublishTaskInProgressConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ContentPublishTaskStatus status)
            return false;

        return status == ContentPublishTaskStatus.InProgress;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}