using ChatGptNet;
using DallENet;
using DallENet.Exceptions;
using Microsoft.Extensions.Logging;
using OperationResults;
using Polly;
using RandomPhotos.BusinessLayer.Services.Interfaces;
using RandomPhotos.Shared.Models;

namespace RandomPhotos.BusinessLayer.Services;

public class PhotoService : IPhotoService
{
    private readonly IChatGptClient chatGptClient;
    private readonly IDallEClient dallEClient;
    private readonly ILogger<PhotoService> logger;

    public PhotoService(IChatGptClient chatGptClient, IDallEClient dallEClient, ILogger<PhotoService> logger)
    {
        this.chatGptClient = chatGptClient;
        this.dallEClient = dallEClient;
        this.logger = logger;
    }

    public async Task<Result<Photo>> GeneratePhotoAsync()
    {
        var policy = Policy.Handle<DallEException>(ex => ex.Error?.Code == "contentFilter").RetryAsync(3, onRetry: (error, retryCount) =>
        {
            logger.LogError(error, "Unexpected error while generating image");
        });

        var result = await policy.ExecuteAsync(async () =>
        {
            var language = Thread.CurrentThread.CurrentCulture.EnglishName;

            var conversationId = await chatGptClient.SetupAsync($"You are an assistant that answers always in {language} language.");
            var photoDesriptionResponse = await chatGptClient.AskAsync(conversationId, $"Propose a description for a random picture. Write the description in a single paragraph. The description must be less than 400 characters.");
            var photoDescription = photoDesriptionResponse.GetMessage();

            var prompt = photoDescription[..Math.Min(950, photoDescription.Length)];
            var photo = await dallEClient.GenerateImagesAsync(prompt);

            var result = new Photo(photoDescription, photo.GetImageUrl());
            return result;
        });

        return result;
    }
}
