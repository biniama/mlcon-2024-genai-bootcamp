using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel;
using IComponent = Microsoft.AspNetCore.Components.IComponent;

namespace TalkToYourApp.Client.Features.AI_Integration.Plugins;

public class BlazorNavigationPlugin
{
    private readonly ILogger _logger;
    private readonly NavigationManager _navigationManager;
    private readonly NavigationRouteProvider _routeProvider;

    public BlazorNavigationPlugin(ILogger<BlazorNavigationPlugin> logger,
        NavigationRouteProvider routeProvider,
        NavigationManager navigationManager)
    {
        _logger = logger;
        _routeProvider = routeProvider;
        _navigationManager = navigationManager;
    }

    [KernelFunction]
    [Description(
        "Provides a list of the screens that are available in the application with a description of what they do. A screen can be navigated to using different routes, some of which may have parameters. If a page has multiple routes, any of them can be used. If you can specify parameters in one of the routes by replacing the {parameterName:Type} part of the route with a known value, the route with the most known parameters should be preferred. Example: '{carPart:string}' could be 'Tire' and '{temperatureCelsius:int}' would be '32'.")]
    [return:Description("A JSON string containing the screen descriptions.")]
    public Task<string> GetScreensAsync()
    {
        _logger.LogInformation("Getting screens");

        return Task.FromResult(_routeProvider.PageDescriptionsJson);
    }

    [KernelFunction]
    [Description("Navigates the application to the specified screen.")]
    [return:Description("A message indicating that the application has navigated to the specified screen.")]
    public Task<string> NavigateToAsync(
        [Description(
            "The route of the screen to navigate to. All parameter placeholders in the route must already be replaced with the values.")]
        string screenRoute
    )
    {
        _logger.LogInformation("Navigating to route {Route}", screenRoute);
        _navigationManager.NavigateTo(screenRoute);

        return Task.FromResult($"Application navigated to screen '{_navigationManager.Uri}'.");
    }
}

public record PageDescription(string Name, string[] Routes, string? Description);

public class NavigationRouteProvider
{
    private readonly IEnumerable<Assembly> _assemblies;

    private string? _routeString = null;

    // All components that are routable (aka Pages)
    private IEnumerable<Type> RoutableComponents => _assemblies
        .SelectMany(a => a.ExportedTypes)
        .Where(t => typeof(IComponent).IsAssignableFrom(t) && t.IsDefined(typeof(RouteAttribute)));

    // Page descriptions and routes from the routable components and additional attributes
    private IEnumerable<PageDescription> PageDescriptions => RoutableComponents
        .Select(t => new PageDescription(
            t.Name,
            t.GetCustomAttributes<RouteAttribute>().Select(a => a.Template).ToArray(),
            t.GetCustomAttribute<DescriptionAttribute>()?.Description
        ));

    public string PageDescriptionsJson => _routeString ??= JsonSerializer.Serialize(PageDescriptions);

    public NavigationRouteProvider(IEnumerable<Assembly> assemblies)
    {
        _assemblies = assemblies;
    }
}
