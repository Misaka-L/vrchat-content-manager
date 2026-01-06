using System.Globalization;
using Avalonia.Data.Converters;

namespace VRChatContentPublisher.App.Converters;

public sealed class IsEqualZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType != typeof(bool))
        {
            return null;
        }

        return value switch
        {
            int intValue => intValue == 0,
            double doubleValue => doubleValue == 0.0,
            float floatValue => floatValue == 0.0f,
            long longValue => longValue == 0L,
            decimal decimalValue => decimalValue == 0.0m,
            _ => null
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}