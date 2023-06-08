using ChatGptNet;
using DallENet;

namespace RandomPhotos.BusinessLayer.Services;

internal class PhotoService
{
    private readonly IChatGptClient chatGptClient;
    private readonly IDallEClient dallEClient;

    public PhotoService(IChatGptClient chatGptClient, IDallEClient dallEClient)
    {
        this.chatGptClient = chatGptClient;
        this.dallEClient = dallEClient;
    }
}
