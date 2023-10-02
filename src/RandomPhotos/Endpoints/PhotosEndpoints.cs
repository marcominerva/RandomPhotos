using MinimalHelpers.Routing;
using OperationResults.AspNetCore.Http;
using RandomPhotos.BusinessLayer.Services.Interfaces;
using RandomPhotos.Shared.Models;

namespace RandomPhotos.Endpoints;

public class ChatEndpoints : IEndpointRouteHandler
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var translatorApiGroup = endpoints.MapGroup("/api/photos");

        translatorApiGroup.MapPost(string.Empty, GeneratePhotoAsync)
            .WithName("GenerateRandomPhotos")
            .Produces<Photo>()
            .WithOpenApi();
    }

    public async Task<IResult> GeneratePhotoAsync(IPhotoService photoService, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await photoService.GeneratePhotoAsync(cancellationToken);

        var response = httpContext.CreateResponse(result);
        return response;
    }
}