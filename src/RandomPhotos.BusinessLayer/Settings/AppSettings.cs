namespace RandomPhotos.BusinessLayer.Settings;

public class AppSettings
{
    public string ApplicationName { get; init; } = "TranslatorGPT";

    public string ApplicationDescription { get; init; } = "Translate text using AI";

    public string[] SupportedCultures { get; init; }

    public int PhotoWidth { get; init; }

    public int PhotoHeight { get; init; }

    public string ImageDescriptionPrompt { get; init; } = "Propose a description for a random picture. The picture can represents landscapes, animals, people and everyday's life. Write the description in a single paragraph. The description must be less than 400 characters.";
}
