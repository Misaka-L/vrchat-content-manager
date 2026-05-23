using System.Globalization;
using Avalonia.Markup.Xaml;
using VRChatContentPublisher.CrashHandler.App.Resources;

namespace VRChatContentPublisher.CrashHandler.App.Extensions;

public class LocExtension : MarkupExtension
{
    public LocExtension() { }

    public LocExtension(string key)
    {
        Key = key;
    }

    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Strings.ResourceManager.GetString(Key, CultureInfo.CurrentUICulture) ?? $"[[{Key}]]";
    }
}
