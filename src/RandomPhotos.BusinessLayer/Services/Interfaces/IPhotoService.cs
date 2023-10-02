using OperationResults;
using RandomPhotos.Shared.Models;

namespace RandomPhotos.BusinessLayer.Services.Interfaces;

public interface IPhotoService
{
    Task<Result<Photo>> GeneratePhotoAsync(CancellationToken cancellation = default);
}