using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel;

namespace TalkToYourApp.Client.Features.AI_Integration.Plugins;

public interface IFormCompletion : IHandleEvent
{
    object? FormModel { get; }    
}

public class FormCompletionService
{
    private readonly ILogger _logger;
    private HashSet<IFormCompletion> _formComponents = new();

    public FormCompletionService(ILogger<FormCompletionService> logger)
    {
        _logger = logger;
    }

    public void RegisterFormComponent(IFormCompletion formComponent)
    {
        _logger.LogDebug("Registering form component {FormComponent}", formComponent.GetType().Name);

        _formComponents.Add(formComponent);
    }

    public void UnregisterFormComponent(IFormCompletion formComponent)
    {
        _logger.LogDebug("Unregistering form component {FormComponent}", formComponent.GetType().Name);

        _formComponents.Remove(formComponent);
    }

    // returns a json describing all currently registered form models and their fields
    public string GetFormsStructure()
    {
        _logger.LogDebug("Getting form structure");

        var formStructure = _formComponents
            .Where(fc => fc.FormModel is not null)
            .Select(fc => fc.FormModel!)
            .Select(fm => new {
                FormName = fm.GetType().Name,
                FormDescription = fm.GetType().GetCustomAttributes<DescriptionAttribute>().FirstOrDefault()?.Description,
                FormFields = fm.GetType().GetProperties().Select(p =>
                    new {
                        FieldName = p.Name,
                        Description = p.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault()?.Description ?? p.Name,
                        FieldType = p.PropertyType.Name,
                    })
                })
            .ToArray();

        return JsonSerializer.Serialize(formStructure);
    }

    public object? GetFormModel(string formName)
    {
        _logger.LogDebug("Getting form model for {FormName}", formName);

        return _formComponents
            .Where(fc => fc.FormModel is not null)
            .Select(fc => fc.FormModel!)
            .FirstOrDefault(fm => fm.GetType().Name == formName);
    }

    public async Task NotifyFormChangedAsync(string fromName)
    {
        var callback = new EventCallbackWorkItem();

        var component = _formComponents.FirstOrDefault(fc => fc.FormModel?.GetType().Name == fromName);

        if (component is not null)
        {
            await component.HandleEventAsync(callback, null);            
        }
    }
}

public class FormCompletionPlugin
{
    private readonly ILogger _logger;
    private readonly FormCompletionService _completionService;

    public FormCompletionPlugin(ILogger<FormCompletionPlugin> logger, FormCompletionService completionService)
    {
        _logger = logger;
        _completionService = completionService;
    }

    [KernelFunction]
    [Description("Retrieves a list of the currently displayed forms and all their fields.")]
    [return: Description("A string describing all currently displayed forms and their fields.")]
    public Task<string> ListCurrentlyDisplayedForms()
    {
        _logger.LogDebug("Listing currently displayed forms");

        var prompt = "This is a list of all of the forms that are currently displayed on the screen: \n";
        var formStructure = _completionService.GetFormsStructure();

        return Task.FromResult(prompt + formStructure);
    }

    [KernelFunction]
    [Description("Fills out fields on currently displayed forms. Make sure to list the currently displayed forms before calling this function to know what fields to fill out.")]
    [return: Description("A string describing the result of the form completion.")]
    public async Task<string> CompleteForms(
        [Description("A structured string that defines the form name as the first property name and the values need to be dictionaries of form field names with their values. All values should be strings. "
            + "The structure looks like this. Be aware that this is NOT json, but follows the same structure only with parenthesis instead of curly braces:"
            + "( \"formName1\": ( \"fieldName1\": \"fieldValue2\", \"fieldName2\" : \"fieldValue2\" ), \"formName2\": ( \"nextField\": \"someOtherValue\" ) )\n"
            )] string formData
        )
    {
        _logger.LogDebug("Completing forms with json {FormCompletionJson}", formData);

        try
        {
            var jsonFormData = MakeJson(formData);
            var parsedFormData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(jsonFormData)!;

            foreach (var form_kvp in parsedFormData)
            {
                var model = _completionService.GetFormModel(form_kvp.Key);
                if (model is null)
                {
                    _logger.LogInformation("Form with name '{FormName}' was not found", form_kvp.Key);
                    continue;
                }

                foreach (var property_kvp in form_kvp.Value)
                {
                    var propertyInfo = model.GetType().GetProperty(property_kvp.Key);
                    if (propertyInfo is null)
                    {
                        _logger.LogInformation("Property with name '{PropertyName}' was not found on form '{FormName}'", property_kvp.Key, form_kvp.Key);
                        continue;
                    }

                    try
                    {
                        propertyInfo.SetValue(model, Convert.ChangeType(property_kvp.Value, propertyInfo.PropertyType));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Could not convert value '{PropertyValue}' to type '{PropertyType}' on property '{PropertyName}' for form '{FormName}'", property_kvp.Value, propertyInfo.PropertyType, property_kvp.Key, form_kvp.Key);
                    }
                }

                await _completionService.NotifyFormChangedAsync(form_kvp.Key);
            }

            return "The properties of the forms have been filled out.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not parse form completion json {FormCompletionJson}", formData);

            return $"The form completion json could not be parsed. The error message was '{ex.Message}'. You can either try again with a reformatted json string or you can cancel the form completion.";
        }
    }

    private string MakeJson(string parethesisson)
    {
        _logger.LogDebug("Making json from parenthesisson '{Parethesisson}'", parethesisson);
        var sb = new StringBuilder();
        bool inQuotes = false;

        foreach (char c in parethesisson)
        {
            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }

            if (!inQuotes && c == '(')
            {
                sb.Append('{');
            }      
            else if (!inQuotes && c == ')')
            {
                sb.Append('}');
            }
            else
            {
                sb.Append(c);
            }
        }

        var result = sb.ToString();
        _logger.LogDebug("Returning json from parenthesisson '{json}'", result);
        return result;
    }
}
