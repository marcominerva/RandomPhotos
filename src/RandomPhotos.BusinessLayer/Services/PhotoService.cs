using ChatGptNet;
using DallENet;
using Microsoft.Extensions.Options;
using OperationResults;
using Polly;
using Polly.Registry;
using RandomPhotos.BusinessLayer.Services.Interfaces;
using RandomPhotos.BusinessLayer.Settings;
using RandomPhotos.Shared.Models;

namespace RandomPhotos.BusinessLayer.Services;

public class PhotoService : IPhotoService
{
    private readonly IChatGptClient chatGptClient;
    private readonly IDallEClient dallEClient;
    private readonly ResiliencePipeline pipeline;
    private readonly AppSettings appSettings;

    public PhotoService(IChatGptClient chatGptClient, IDallEClient dallEClient, ResiliencePipelineProvider<string> pipelineProvider, IOptions<AppSettings> appSettingsOptions)
    {
        this.chatGptClient = chatGptClient;
        this.dallEClient = dallEClient;

        pipeline = pipelineProvider.GetPipeline("DallEContentFilterResiliencePipeline");

        appSettings = appSettingsOptions.Value;
    }

    public async Task<Result<Photo>> GeneratePhotoAsync(CancellationToken cancellationToken = default)
    {
        var result = await pipeline.ExecuteAsync(async (cancellationToken) =>
        {
            var language = Thread.CurrentThread.CurrentCulture.EnglishName;

            var conversationId = await chatGptClient.SetupAsync($"You are an assistant that answers always in {language} language.", cancellationToken);
            var photoDesriptionResponse = await chatGptClient.AskAsync(conversationId, appSettings.ImageDescriptionPrompt, cancellationToken: cancellationToken);
            var photoDescription = photoDesriptionResponse.GetContent();

            var prompt = photoDescription[..Math.Min(950, photoDescription.Length)];
            var photo = await dallEClient.GenerateImagesAsync(prompt, cancellationToken: cancellationToken);

            var result = new Photo(photoDescription, photo.GetImageUrl());
            return result;
        }, cancellationToken);

        return result;
    }
}
