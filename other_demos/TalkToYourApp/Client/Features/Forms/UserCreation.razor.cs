using Microsoft.AspNetCore.Components.Forms;
using Riok.Mapperly.Abstractions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TalkToYourApp.Client.Features.Forms;

[Description("This page presents a form that lets you register a new user.")]
public partial class UserCreation
{
    protected UserCreationModel _initialValues = new UserCreationModel();

    public FluxorEditForm<UserCreationModel> _form = null!;
    public UserCreationModel? Model => _form.EditModel;

    protected bool success;

    private void OnValidSubmit(EditContext context)
    {
        success = true;
        StateHasChanged();
    }

    protected void Test()
    {
        if (Model is null) return;

        Model.Email = "test@pest.de";

        _form.EditContext?.NotifyFieldChanged(new FieldIdentifier(Model, nameof(UserCreationModel.Email)));
    }
}

[Description("This is a form that lets you register a new user.")]
public class UserCreationModel
{
    [Required]
    [StringLength(20, ErrorMessage = "Name length can't be more than 20 characters.")]
    [Description("The name of the user")]
    public string Username { get; set; } = null!;

    [Required]
    [EmailAddress]
    [Description("The email of the user")]
    public string Email { get; set; } = null!;

    [Required]
    [StringLength(30, ErrorMessage = "Password must be at least 8 characters long.", MinimumLength = 8)]
    [Description("The new password for the user. If the provided value is shorter than 8 characters, omit it and tell the user to manually set a longer password.")]
    public string Password { get; set; } = null!;

    [Required]
    [Compare(nameof(Password))]
    [Description("The repetition of the new password for the user. For security reasons, always set this to an empty string (\"\") and tell the user to enter the repetition manually.")]
    public string Password2 { get; set; } = null!;
}

[Mapper(UseDeepCloning = true)]
public partial class UserCreationModelMapper : IFormModelMapper<UserCreationModel>
{
    public partial UserCreationModel MapModel(UserCreationModel source);
    public partial void MapModel(UserCreationModel source, UserCreationModel target);
}
