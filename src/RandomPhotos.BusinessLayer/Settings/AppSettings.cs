namespace RandomPhotos.BusinessLayer.Settings;

public class AppSettings
{
    public string ApplicationName { get; init; } = "Random Photos";

    public string ApplicationDescription { get; init; } = "Use AI to generate random photos";

    public string[] SupportedCultures { get; init; }
}
