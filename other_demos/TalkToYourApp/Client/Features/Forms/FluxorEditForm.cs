using Fluxor;
using TalkToYourApp.Client.Features.AI_Integration.Plugins;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace TalkToYourApp.Client.Features.Forms;

public class FluxorEditForm<TFormModel> : EditForm, IFormCompletion, IDisposable
{
    private TFormModel? _internalMutableModel;
    // private TFormModel? _initialValues;

    [Inject] protected IDispatcher Dispatcher { get; set; } = null!;
    [Inject] protected IState<FluxorFormState> FormState { get; set; } = null!;
    [Inject] protected IFormModelMapper<TFormModel> Mapper { get; set; } = null!;
    [Inject] protected FormCompletionService FormCompletionService { get; set; } = null!;

    [Parameter] public Guid FormId { get; set; } = Guid.NewGuid();
    [Parameter] public TFormModel? InitialValues { get; set; }
    [Parameter] public bool UpdateStateWhenInvalid { get; set; } = false;

    public TFormModel? EditModel => _internalMutableModel;

    public object FormModel => _internalMutableModel!;

    public void Dispose()
    {
        FormState.StateChanged -= OnFormStateChanged;
        EditContext!.OnFieldChanged -= OnFormFieldChanged;

        FormCompletionService.UnregisterFormComponent(this);
        Dispatcher.Dispatch(new FluxorFormDisposing(FormId));
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        FormState.StateChanged += OnFormStateChanged;
        FormCompletionService.RegisterFormComponent(this);
    }

    protected override void OnParametersSet()
    {
        if (Model is not null)
        {
            throw new InvalidOperationException($"{nameof(FluxorEditForm<TFormModel>)} does not support the {nameof(Model)} parameter. Use {nameof(InitialValues)} instead.");
        }

        if (InitialValues is null) throw new InvalidOperationException($"{nameof(InitialValues)} must not be null");

        if (EditContext?.Model is not TFormModel)
        {
            _internalMutableModel = Mapper.MapModel(InitialValues);
            var immutableModel = Mapper.MapModel(_internalMutableModel);

            if (_internalMutableModel is not null)
            {
                EditContext = new EditContext(_internalMutableModel);
                EditContext.OnFieldChanged += OnFormFieldChanged;
            }

            Dispatcher.Dispatch(new FluxorFormCreated(FormId, immutableModel!));
        }

        base.OnParametersSet();
    }

    private bool _isFormFieldChangedStateChange = false;
    protected virtual void OnFormStateChanged(object? sender, EventArgs e)
    {
        if (_isFormFieldChangedStateChange)
        {
            _isFormFieldChangedStateChange = false;
            return;
        }

        var state = FormState.Value[FormId];

        if (state is Newtonsoft.Json.Linq.JObject jObject)
        {
            state = jObject.ToObject<TFormModel>();
        }

        if (state is TFormModel model)
        {
            Mapper.MapModel(model, _internalMutableModel!);
            StateHasChanged();
        }
    }

    protected virtual void OnFormFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        var editContext = sender as EditContext;
        if (editContext is null)
        {
            throw new InvalidOperationException("Edit context not available.");
        }

        if (_internalMutableModel is null)
        {
            throw new InvalidOperationException("Internal model not available.");
        }

        var isValid = !editContext.GetValidationMessages().Any();
        if (!UpdateStateWhenInvalid && isValid || UpdateStateWhenInvalid)
        {
            var immutableModel = Mapper.MapModel(_internalMutableModel);
            _isFormFieldChangedStateChange = true;

            // todo: only dispatch if a value really changed (e.g. by comparing the old and new value)
            Dispatcher.Dispatch(new FluxorFormFieldChanged(FormId, e.FieldIdentifier.FieldName, immutableModel!));
        }
    }
}
