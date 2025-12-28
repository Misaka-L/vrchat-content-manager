using Avalonia.Xaml.Interactions.Custom;

namespace VRChatContentPublisher.App.Interaction.Validation;

public class HttpValidationRule : IValidationRule<string>
{
    public bool Validate(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));
    }

    public string? ErrorMessage { get; set; } = "Must be a http or https URI";
}