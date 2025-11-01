using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using VRChatContentManager.Core.Models;

namespace VRChatContentManager.App.Converters;

public sealed class ContentPublishTaskStatusForegroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ContentPublishTaskStatus status)
            return null;

        return status switch
        {
            ContentPublishTaskStatus.Failed => new SolidColorBrush(Color.Parse("#f44336")),
            ContentPublishTaskStatus.InProgress => new SolidColorBrush(Color.Parse("#3f51b5")),
            ContentPublishTaskStatus.Completed => new SolidColorBrush(Color.Parse("#4caf50")),
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}