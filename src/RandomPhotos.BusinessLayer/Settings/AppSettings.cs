namespace RandomPhotos.BusinessLayer.Settings;

public class AppSettings
{
    public string ApplicationName { get; init; } = "TranslatorGPT";

    public string ApplicationDescription { get; init; } = "Translate text using AI";

    public string[] SupportedCultures { get; init; }
}
