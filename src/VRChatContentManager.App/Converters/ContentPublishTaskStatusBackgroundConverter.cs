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
            ContentPublishTaskStatus.Failed => new SolidColorBrush(Color.Parse("#d50000")),
            ContentPublishTaskStatus.InProgress => new SolidColorBrush(Color.Parse("#304ffe")),
            ContentPublishTaskStatus.Completed => new SolidColorBrush(Color.Parse("#00c853")),
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}