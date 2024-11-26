using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Fluxor;
using TalkToYourApp.Client;
using TalkToYourApp.Client.Features.Forms;
using TalkToYourApp.Client.Features.AI_Integration;
using TalkToYourApp.Client.Features.AI_Integration.Plugins;

var builder = WebAssemblyHostBuilder
    .CreateDefault(args);

builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.Configure<ProxyOpenAIOptions>(o => {
    var service = builder.Configuration.GetValue<string>("AIService") ?? "OpenAI";
    builder.Configuration.GetSection(service).Bind(o);

    if (o.UseProxy)
    {
        o.ProxyAddress = builder.HostEnvironment.BaseAddress
            + (builder.HostEnvironment.BaseAddress.EndsWith("/") ? String.Empty : "/")
            + service + "/";
    }
});

builder.Services.AddMudServices();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddFluxor(o => {

    o.ScanAssemblies(typeof(Program).Assembly);

    o.AddMiddleware<DebuggingMiddleware>();
    if (builder.HostEnvironment.IsDevelopment())
    {
        o.UseReduxDevTools();
    }
});

builder.Services.AddScoped<IFormModelMapper<UserCreationModel>, UserCreationModelMapper>();

// AI Services
builder.Services
    .AddSingleton(new NavigationRouteProvider(new [] { typeof(Program).Assembly }))
    .AddScoped<BlazorNavigationPlugin>()
    .AddScoped<FormCompletionService>()
    .AddScoped<FormCompletionPlugin>()
    .AddScoped<TalkToYourAppAI>();

await builder.Build().RunAsync();

public class DebuggingMiddleware : Middleware
{
    private readonly ILogger<DebuggingMiddleware> _logger;

    public DebuggingMiddleware(ILogger<DebuggingMiddleware> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogTrace("Ctor called");
    }

    public override Task InitializeAsync(IDispatcher dispatcher, IStore store)
    {
        _logger.LogTrace("Initializing Middleware");
        return Task.CompletedTask;
    }

    public override void AfterInitializeAllMiddlewares()
    {
        _logger.LogInformation("All middlewares have been initialized");
    }

    public override void BeforeDispatch(object action)
    {
        _logger.LogDebug("Before dispatch: {@action}", action);
    }

    public override bool MayDispatchAction(object action)
    {
        _logger.LogDebug("May dispatch: {@action}", action);
        return true;
    }

    public override void AfterDispatch(object action)
    {
        _logger.LogDebug("After dispatch: {@action}", action);
    }
}
