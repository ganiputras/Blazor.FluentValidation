
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Blazor.FluentValidation;

/// <summary>
/// Komponen Blazor untuk mengintegrasikan FluentValidation ke dalam <c>EditForm</c> secara otomatis.
/// Gunakan di dalam <c>EditForm</c> dan tetapkan <see cref="ValidatorType"/> atau <see cref="ValidatorInstance"/>.
/// </summary>
public class FluentValidationExtension : ComponentBase, IDisposable
{
    private ValidationMessageStore _validationMessageStore = null!;
    private IValidator _validator = null!;
    private bool _initialized;

    /// <summary>
    /// EditContext dari <c>EditForm</c> secara otomatis diambil melalui <c>[CascadingParameter]</c>.
    /// </summary>
    [CascadingParameter]
    private EditContext EditContext { get; set; } = null!;

    /// <summary>
    /// Tipe validator yang diambil dari DI container.
    /// Misalnya: <c>ValidatorType="typeof(MyValidator)"</c>.
    /// Abaikan jika menggunakan <see cref="ValidatorInstance"/>.
    /// </summary>
    [Parameter]
    public Type? ValidatorType { get; set; }

    /// <summary>
    /// Jika Anda ingin menyuplai validator secara langsung (misalnya new MyValidator()), gunakan ini.
    /// Jika diset, maka <see cref="ValidatorType"/> akan diabaikan.
    /// </summary>
    [Parameter]
    public IValidator? ValidatorInstance { get; set; }

    /// <summary>
    /// Dependency injection provider untuk resolve validator.
    /// </summary>
    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var previousEditContext = EditContext;
        var previousValidatorType = ValidatorType;
        var previousValidatorInstance = ValidatorInstance;

        await base.SetParametersAsync(parameters);

        if (EditContext == null)
            throw new InvalidOperationException($"{nameof(FluentValidationExtension)} harus berada di dalam {nameof(EditForm)}.");

        if (ValidatorInstance is null && ValidatorType is null)
            throw new InvalidOperationException($"Salah satu dari {nameof(ValidatorType)} atau {nameof(ValidatorInstance)} harus diset.");

        if (ValidatorType != null && !typeof(IValidator).IsAssignableFrom(ValidatorType))
            throw new ArgumentException($"{ValidatorType.Name} harus mengimplementasi {typeof(IValidator).FullName}.");

        var validatorChanged = ValidatorInstance != previousValidatorInstance || ValidatorType != previousValidatorType;

        if (!_initialized || validatorChanged)
            LoadValidator();

        if (!_initialized || EditContext != previousEditContext)
            RegisterValidationEvents();

        _initialized = true;
    }

    /// <summary>
    /// Memuat validator dari <see cref="ValidatorInstance"/> atau dari <see cref="ServiceProvider"/>.
    /// </summary>
    private void LoadValidator()
    {
        if (ValidatorInstance != null)
        {
            _validator = ValidatorInstance;
        }
        else
        {
            _validator = ServiceProvider.GetService(ValidatorType!) as IValidator
                ?? throw new InvalidOperationException($"Validator dari tipe {ValidatorType!.FullName} tidak ditemukan di DI container.");
        }
    }

    /// <summary>
    /// Mendaftarkan event handler validasi untuk EditForm.
    /// </summary>
    private void RegisterValidationEvents()
    {
        _validationMessageStore = new ValidationMessageStore(EditContext);
        EditContext.OnValidationRequested += OnValidationRequestedHandler;
        EditContext.OnFieldChanged += OnFieldChangedHandler;
    }

    /// <summary>
    /// Validasi seluruh model saat form disubmit.
    /// </summary>
    private async void OnValidationRequestedHandler(object? sender, ValidationRequestedEventArgs e)
    {
        await ValidateModelAsync();
    }

    /// <summary>
    /// Validasi properti tertentu saat field berubah.
    /// </summary>
    private async void OnFieldChangedHandler(object? sender, FieldChangedEventArgs e)
    {
        await ValidateFieldAsync(e.FieldIdentifier);
    }

    /// <summary>
    /// Melakukan validasi seluruh model dan menampilkan hasilnya.
    /// </summary>
    private async Task ValidateModelAsync()
    {
        _validationMessageStore.Clear();

        var context = new ValidationContext<object>(EditContext.Model);
        var result = await _validator.ValidateAsync(context);

        ApplyValidationResult(result);
    }

    /// <summary>
    /// Melakukan validasi pada satu field.
    /// </summary>
    private async Task ValidateFieldAsync(FieldIdentifier field)
    {
        _validationMessageStore.Clear(field);

        var context = new ValidationContext<object>(
            EditContext.Model,
            new PropertyChain(),
            new MemberNameValidatorSelector(new[] { field.FieldName }));

        var result = await _validator.ValidateAsync(context);

        ApplyValidationResult(result);
    }

    /// <summary>
    /// Menambahkan hasil validasi ke ValidationMessageStore agar muncul di UI.
    /// </summary>
    private void ApplyValidationResult(ValidationResult result)
    {
        foreach (var failure in result.Errors)
        {
            var fieldIdentifier = ToFieldIdentifier(EditContext, failure.PropertyName);
            _validationMessageStore.Add(fieldIdentifier, failure.ErrorMessage);
        }

        EditContext.NotifyValidationStateChanged();
    }

    /// <summary>
    /// Mengubah path properti nested (misalnya "Contact.Name") menjadi FieldIdentifier.
    /// Ini diperlukan agar validasi nested property bisa tampil di UI.
    /// </summary>
    private static FieldIdentifier ToFieldIdentifier(EditContext editContext, string propertyPath)
    {
        object currentObject = editContext.Model;
        var props = propertyPath.Split('.');

        for (int i = 0; i < props.Length - 1; i++)
        {
            var propInfo = currentObject.GetType().GetProperty(props[i]);
            if (propInfo == null || propInfo.GetValue(currentObject) == null)
            {
                // Fallback: gunakan root model jika property null atau tidak ditemukan
                return new FieldIdentifier(editContext.Model, propertyPath);
            }

            currentObject = propInfo.GetValue(currentObject)!;
        }

        return new FieldIdentifier(currentObject, props[^1]);
    }

    /// <summary>
    /// Melepas event handler saat komponen dihancurkan.
    /// </summary>
    public void Dispose()
    {
        EditContext.OnValidationRequested -= OnValidationRequestedHandler;
        EditContext.OnFieldChanged -= OnFieldChangedHandler;
    }
}

