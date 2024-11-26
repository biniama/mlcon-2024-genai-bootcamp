namespace TalkToYourApp.Client.Features.AI_Integration;

public class OpenAIOptions
{
    public string Model { get; set; } = String.Empty;
    public string ApiKey { get; set; } = String.Empty;
    public string? BaseUrl { get; set; }
}
