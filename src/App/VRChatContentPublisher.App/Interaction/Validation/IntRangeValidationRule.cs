using Avalonia.Xaml.Interactions.Custom;

namespace VRChatContentPublisher.App.Interaction.Validation;

public class IntRangeValidationRule : IValidationRule<string>
{
    public int Min { get; set; } = 0;
    public int Max { get; set; } = int.MaxValue;

    public bool Validate(string? value)
    {
        if (!int.TryParse(value, out var intValue))
        {
            return false;
        }

        return intValue >= Min && intValue <= Max;
    }

    public string? ErrorMessage { get; set; } = "Input out of range.";
}