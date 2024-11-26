using System.Reflection;
using TalkToYourApp.Client.Features.AI_Integration.Plugins;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using MudBlazor;

namespace TalkToYourApp.Client.Features.AI_Integration;

public class TalkToYourAppAI
{
	private readonly ILogger _logger;
	private readonly IServiceProvider _serviceProvider;
	private readonly ProxyOpenAIOptions _options;
	private readonly ISnackbar _snackbar;

	private Kernel? _kernel;

	public Kernel Kernel => _kernel ??= BuildKernel();

	public TalkToYourAppAI(ILogger<TalkToYourAppAI> logger, IServiceProvider serviceProvider, IOptions<ProxyOpenAIOptions> options, ISnackbar snackbar)
	{
		_logger = logger;
		_serviceProvider = serviceProvider;
		_options = options.Value;
		_snackbar = snackbar;
	}

	private Kernel BuildKernel()
	{
		var builder = Kernel.CreateBuilder();
		builder.Services.AddSingleton(_serviceProvider.GetRequiredService<ILoggerFactory>());

		var httpClient = _options.UseProxy
			? new HttpClient(new ProxyClientHandler(new Uri(_options.ProxyAddress)))
			: new HttpClient();

		if (_options.IsOpenAI)
		{
			builder.AddOpenAIChatCompletion(_options.Model, _options.ApiKey, httpClient: httpClient);
		}
		else
		{
			builder.AddAzureOpenAIChatCompletion(_options.Model,
				_options.BaseUrl ?? throw new InvalidOperationException("Azure OpenAI base URL is not set."),
				_options.ApiKey,
				httpClient: httpClient);
		}

		var kernel = builder.Build();

		_logger.LogDebug("Kernel built with model {Model}", _options.Model);
	
		kernel.Plugins.AddFromObject(_serviceProvider.GetRequiredService<BlazorNavigationPlugin>(), serviceProvider: _serviceProvider);
		_logger.LogDebug("Plugin {Plugin} imported", "BlazorApplicationNavigationPlugin");
		
		kernel.Plugins.AddFromObject(_serviceProvider.GetRequiredService<FormCompletionPlugin>(), serviceProvider: _serviceProvider);
		_logger.LogDebug("Plugin {Plugin} imported", "FormCompletionPlugin");

		return kernel;
	}

	public async Task<string> ExecuteUserRequest(string userInput)
	{
		string prompt = @$"		
You are TalkToMyApp, an AI assistant agent embedded in an application that is mainly used for Customer Relationship Management, but it also provides some general functionalities that do not have to do with CRM.
You have partial control over some functionality of the application, provided to you through functions.
You are able to navigate the application and select screens for the user, and sometimes it may be possible to extract information from the users request and use it to fill in a form after navigating there.
You probably want to start by listing the available screens and their functionalities, to check if there is a page available that can help the user to do what he wants.
Always give the final answer in english.

[USER INPUT]
{userInput}
[END OF USER INPUT]
";
		
#pragma warning disable SKEXP0061		
		var planner = new FunctionCallingStepwisePlanner();
		var result = await planner.ExecuteAsync(Kernel, prompt);

		var answer = result.FinalAnswer;
#pragma warning restore SKEXP0061
		
		_logger.LogInformation("Result: {Result}", answer);
		_snackbar.Configuration.PositionClass = Defaults.Classes.Position.BottomRight;

		_snackbar.Add($"🤖: {answer}", Severity.Info, config =>
		{
			config.Icon = Icons.Material.Filled.Android;
			config.IconColor = Color.Info;
			config.IconSize = Size.Large;
		});

		return answer;
	}
}