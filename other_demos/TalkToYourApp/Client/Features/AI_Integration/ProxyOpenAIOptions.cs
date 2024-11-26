namespace TalkToYourApp.Client.Features.AI_Integration;

public class ProxyOpenAIOptions : OpenAIOptions
{
    public bool IsOpenAI { get; set; }
    public bool UseProxy { get; set; }
    public string ProxyAddress { get; set; } = null!;
}
