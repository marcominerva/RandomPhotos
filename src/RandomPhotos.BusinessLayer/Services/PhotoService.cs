using ChatGptNet;
using DallENet;
using OperationResults;
using RandomPhotos.BusinessLayer.Services.Interfaces;
using RandomPhotos.Shared.Models;

namespace RandomPhotos.BusinessLayer.Services;

public class PhotoService : IPhotoService
{
    private readonly IChatGptClient chatGptClient;
    private readonly IDallEClient dallEClient;

    public PhotoService(IChatGptClient chatGptClient, IDallEClient dallEClient)
    {
        this.chatGptClient = chatGptClient;
        this.dallEClient = dallEClient;
    }

    public async Task<Result<Photo>> GeneratePhotoAsync()
    {
        var language = Thread.CurrentThread.CurrentCulture.EnglishName;

        var photoDesriptionResponse = await chatGptClient.AskAsync($"Propose a description for a random picture, specifying a random style. Write the description in {language}, using a single paragraph. The description must be less than 700 characters.");
        var photoDescription = photoDesriptionResponse.GetMessage();

        var prompt = photoDescription[..Math.Min(999, photoDescription.Length)];
        var photo = await dallEClient.GenerateImagesAsync(prompt);

        var result = new Photo(photoDescription, photo.GetImageUrl());
        return result;
    }
}
