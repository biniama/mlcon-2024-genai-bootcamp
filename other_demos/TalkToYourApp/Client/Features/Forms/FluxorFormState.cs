using Fluxor;

namespace TalkToYourApp.Client.Features.Forms;

// Actions
public record FluxorFormCreated(Guid FormId, object FormModel);
public record FluxorFormFieldChanged(Guid FormId, string FieldName, object FormValues);
public record FluxorFormDisposing(Guid FormId);

// State
[FeatureState]
public record FluxorFormState
{
    public IReadOnlyDictionary<Guid, object> FormsModels { get; init; } = new Dictionary<Guid, object>();

    public object? this[Guid formId] => GetForm(formId);

    private object? GetForm(Guid formId)
    {
        try
        {
            return FormsModels[formId];
        }
        catch
        {
            return null;
        }
    }
}

// Reducers
public static class FluxorFormReducers
{
    [ReducerMethod]
    public static FluxorFormState ReduceFluxorFormCreated(FluxorFormState currentState, FluxorFormCreated action)
    {
        var newFormsDictionary = new Dictionary<Guid, object>(currentState.FormsModels.ToDictionary(k => k.Key, v => v.Value))
        {
            { action.FormId, action.FormModel }
        };

        return currentState with { FormsModels = newFormsDictionary, };
    }

    [ReducerMethod]
    public static FluxorFormState ReduceFluxorFormFieldChanged(FluxorFormState currentState, FluxorFormFieldChanged action)
    {
        var updateableFormsDictionary = currentState.FormsModels.ToDictionary(k => k.Key, v => v.Value);
        updateableFormsDictionary[action.FormId] = action.FormValues;

        return currentState with { FormsModels = updateableFormsDictionary, };
    }

    [ReducerMethod]
    public static FluxorFormState ReduceFluxorFormDisposing(FluxorFormState currentState, FluxorFormDisposing action)
    {
        if (!currentState.FormsModels.ContainsKey(action.FormId))
        {
            return currentState;
        }

        var updateableFormsDictionary = currentState.FormsModels.ToDictionary(k => k.Key, v => v.Value);
        updateableFormsDictionary.Remove(action.FormId);

        return currentState with { FormsModels = updateableFormsDictionary, };
    }
}
