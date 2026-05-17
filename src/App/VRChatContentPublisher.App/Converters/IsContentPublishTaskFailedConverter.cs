using System.Globalization;
using Avalonia.Data.Converters;
using VRChatContentPublisher.Core.Models;
using VRChatContentPublisher.Core.Models.PublishTask;

namespace VRChatContentPublisher.App.Converters;

public sealed class IsContentPublishTaskFailedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType != typeof(bool))
            return null;

        return value is ContentPublishTaskStatus.Failed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}