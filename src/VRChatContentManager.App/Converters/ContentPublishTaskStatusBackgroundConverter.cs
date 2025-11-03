using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using VRChatContentManager.Core.Models;

namespace VRChatContentManager.App.Converters;

public sealed class ContentPublishTaskStatusBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ContentPublishTaskStatus status)
            return null;

        return status switch
        {
            ContentPublishTaskStatus.Failed => new SolidColorBrush(Color.Parse("#ffcdd2")),
            ContentPublishTaskStatus.InProgress => new SolidColorBrush(Color.Parse("#c5cae9")),
            ContentPublishTaskStatus.Completed => new SolidColorBrush(Color.Parse("#c8e6c9")),
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}