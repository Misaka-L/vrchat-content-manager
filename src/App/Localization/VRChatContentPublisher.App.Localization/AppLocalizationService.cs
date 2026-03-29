using System.Globalization;
using Antelcat.I18N.Avalonia;

namespace VRChatContentPublisher.App.Localization;

public static class AppLocalizationService
{
    private const string DetectMatchedCultureStringKey =
        "Internal_ClutureInfoCode_Use_For_Detect_System_Lanuage_Only__Modify_AppLocalizationService_To_Add_Lanauge_Option";

    private const string DefaultAppCulture = "en";

    public static AppLang[] GetLanguages()
    {
        return
        [
            new("en", "English"),
            new("zh-CN", "普通话")
        ];
    }

    private static bool _isInitialized;
    private static CultureInfo _systemDefaultCulture = CultureInfo.CurrentCulture;

    public static void Initialize(string? cultureCode)
    {
        if (_isInitialized)
            return;

        LangKeys.LocalizationProvider.Initialize();
        if (cultureCode is null)
        {
            if (I18NExtension.Translate(DetectMatchedCultureStringKey, DefaultAppCulture) == DefaultAppCulture)
                ReloadAppCulture(DefaultAppCulture);
        }
        else
        {
            ReloadAppCulture(cultureCode);
        }

        _isInitialized = true;
    }

    public static void ReloadAppCulture(string? cultureCode)
    {
        var culture = cultureCode is not null ? CultureInfo.GetCultureInfo(cultureCode) : _systemDefaultCulture;
        Thread.CurrentThread.CurrentCulture = culture;
        I18NExtension.Culture = culture;
    }
}

public sealed class AppLang(string cultureCode, string displayName)
{
    public string CultureCode { get; } = cultureCode;
    public string DisplayName { get; } = displayName;
}