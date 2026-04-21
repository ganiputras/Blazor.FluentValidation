using System.Collections;
using System.Reflection;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Blazor.FluentValidation;

/// <summary>
/// Komponen Blazor untuk mengintegrasikan FluentValidation ke dalam <c>EditForm</c> secara otomatis.
/// Letakkan di dalam <c>EditForm</c> dan opsional tetapkan <see cref="ValidatorType"/>
/// atau <see cref="ValidatorInstance"/>. Jika keduanya tidak diset, validator akan dicari
/// otomatis dari DI container berdasarkan tipe model.
/// </summary>
public class FluentValidationExtension : ComponentBase, IDisposable
{
    private ValidationMessageStore _messageStore = null!;
    private IValidator _validator = null!;
    private EditContext? _subscribedContext;
    private CancellationTokenSource _cts = new();
    private bool _initialized;

    /// <summary>
    /// EditContext dari <c>EditForm</c>, diambil otomatis melalui <c>[CascadingParameter]</c>.
    /// </summary>
    [CascadingParameter]
    private EditContext EditContext { get; set; } = null!;

    /// <summary>
    /// Tipe validator yang akan di-resolve dari DI container.
    /// Contoh: <c>ValidatorType="typeof(PersonValidator)"</c>.
    /// Diabaikan jika <see cref="ValidatorInstance"/> sudah diset.
    /// </summary>
    [Parameter]
    public Type? ValidatorType { get; set; }

    /// <summary>
    /// Instance validator yang digunakan langsung tanpa DI.
    /// Jika diset, <see cref="ValidatorType"/> diabaikan.
    /// </summary>
    [Parameter]
    public IValidator? ValidatorInstance { get; set; }

    /// <summary>
    /// Rule set FluentValidation yang dieksekusi saat validasi.
    /// <c>null</c> berarti semua rule (termasuk default) dijalankan.
    /// </summary>
    [Parameter]
    public string[]? RuleSets { get; set; }

    /// <summary>
    /// Jika <c>true</c> (default), validasi per-field dijalankan setiap kali field berubah.
    /// Set ke <c>false</c> untuk hanya validasi saat form disubmit.
    /// </summary>
    [Parameter]
    public bool ValidateOnFieldChange { get; set; } = true;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = null!;

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var previousContext = EditContext;
        var previousValidatorType = ValidatorType;
        var previousValidatorInstance = ValidatorInstance;

        await base.SetParametersAsync(parameters);

        if (EditContext is null)
            throw new InvalidOperationException(
                $"{nameof(FluentValidationExtension)} harus berada di dalam {nameof(EditForm)}.");

        var contextChanged = !ReferenceEquals(EditContext, previousContext);
        var validatorChanged = !ReferenceEquals(ValidatorInstance, previousValidatorInstance)
                               || ValidatorType != previousValidatorType;

        // Re-resolve validator saat pertama kali, validator berubah,
        // atau context berganti dengan auto-discovery aktif.
        var needsResolve = !_initialized || validatorChanged
                           || (contextChanged && ValidatorInstance is null && ValidatorType is null);

        if (needsResolve)
            ResolveValidator();

        if (!_initialized || contextChanged)
        {
            UnsubscribeFromContext(_subscribedContext);
            SubscribeToContext();
        }

        _initialized = true;
    }

    // -------------------------------------------------------------------------
    // Validator resolution
    // -------------------------------------------------------------------------

    private void ResolveValidator()
    {
        if (ValidatorInstance is not null)
        {
            _validator = ValidatorInstance;
            return;
        }

        if (ValidatorType is not null)
        {
            if (!typeof(IValidator).IsAssignableFrom(ValidatorType))
                throw new ArgumentException(
                    $"'{ValidatorType.Name}' harus mengimplementasi {typeof(IValidator).FullName}.");

            _validator = ServiceProvider.GetService(ValidatorType) as IValidator
                ?? throw new InvalidOperationException(
                    $"Validator '{ValidatorType.FullName}' tidak terdaftar di DI container.");
            return;
        }

        // Auto-discovery: cari IValidator<TModel> dari DI berdasarkan tipe model.
        var modelType = EditContext.Model.GetType();
        var validatorInterface = typeof(IValidator<>).MakeGenericType(modelType);
        _validator = ServiceProvider.GetService(validatorInterface) as IValidator
            ?? throw new InvalidOperationException(
                $"Tidak ditemukan validator untuk '{modelType.Name}'. " +
                $"Daftarkan IValidator<{modelType.Name}> di DI container, " +
                $"atau set parameter {nameof(ValidatorType)} / {nameof(ValidatorInstance)}.");
    }

    // -------------------------------------------------------------------------
    // Event subscription
    // -------------------------------------------------------------------------

    private void SubscribeToContext()
    {
        _messageStore = new ValidationMessageStore(EditContext);
        _subscribedContext = EditContext;
        EditContext.OnValidationRequested += OnValidationRequested;
        EditContext.OnFieldChanged += OnFieldChanged;
    }

    private void UnsubscribeFromContext(EditContext? context)
    {
        if (context is null) return;
        context.OnValidationRequested -= OnValidationRequested;
        context.OnFieldChanged -= OnFieldChanged;
    }

    // -------------------------------------------------------------------------
    // Event handlers
    // -------------------------------------------------------------------------

    private async void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        CancelPending();
        var token = _cts.Token;
        try
        {
            await ValidateModelAsync(token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            // Lempar ke console agar mudah di-debug; tidak merusak circuit.
            Console.Error.WriteLine($"[FluentValidationExtension] Validasi model gagal: {ex.Message}");
        }
    }

    private async void OnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        if (!ValidateOnFieldChange) return;

        CancelPending();
        var token = _cts.Token;
        try
        {
            await ValidateFieldAsync(e.FieldIdentifier, token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[FluentValidationExtension] Validasi field gagal: {ex.Message}");
        }
    }

    private void CancelPending()
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = new CancellationTokenSource();
    }

    // -------------------------------------------------------------------------
    // Validation logic
    // -------------------------------------------------------------------------

    private async Task ValidateModelAsync(CancellationToken token)
    {
        _messageStore.Clear();

        var context = BuildContext(EditContext.Model);
        var result = await _validator.ValidateAsync(context, token);

        ApplyResult(result);
    }

    private async Task ValidateFieldAsync(FieldIdentifier fieldId, CancellationToken token)
    {
        _messageStore.Clear(fieldId);

        var fullPath = ResolveFullPath(fieldId);

        IValidationContext context = RuleSets?.Length > 0
            ? ValidationContext<object>.CreateWithOptions(
                EditContext.Model,
                opts => opts.IncludeRuleSets(RuleSets).IncludeProperties(fullPath))
            : new ValidationContext<object>(
                EditContext.Model,
                new PropertyChain(),
                new MemberNameValidatorSelector([fullPath]));

        var result = await _validator.ValidateAsync(context, token);

        ApplyResult(result);
    }

    private void ApplyResult(ValidationResult result)
    {
        foreach (var failure in result.Errors)
        {
            var fieldId = ToFieldIdentifier(EditContext, failure.PropertyName);
            _messageStore.Add(fieldId, failure.ErrorMessage);
        }

        EditContext.NotifyValidationStateChanged();
    }

    private ValidationContext<object> BuildContext(object model)
    {
        return RuleSets?.Length > 0
            ? ValidationContext<object>.CreateWithOptions(
                model,
                opts => opts.IncludeRuleSets(RuleSets))
            : new ValidationContext<object>(model);
    }

    // -------------------------------------------------------------------------
    // Path resolution
    // -------------------------------------------------------------------------

    /// <summary>
    /// Membangun path properti lengkap dari root model ke field yang diberikan.
    /// Diperlukan untuk nested object: FieldIdentifier.Model bisa berupa child object
    /// (misal <c>Address</c>), sehingga path harus diawali dengan prefix-nya (misal <c>Address.Street</c>).
    /// </summary>
    private string ResolveFullPath(FieldIdentifier fieldId)
    {
        if (ReferenceEquals(fieldId.Model, EditContext.Model))
            return fieldId.FieldName;

        var prefix = FindObjectPath(EditContext.Model, fieldId.Model, visited: new HashSet<object>(ReferenceEqualityComparer.Instance));
        return prefix is not null ? $"{prefix}.{fieldId.FieldName}" : fieldId.FieldName;
    }

    /// <summary>
    /// Mencari path dari <paramref name="root"/> ke objek <paramref name="target"/> secara rekursif.
    /// Melindungi dari referensi siklis menggunakan <paramref name="visited"/>.
    /// Mendukung properti biasa dan collection.
    /// </summary>
    private static string? FindObjectPath(object root, object target, string currentPath = "", HashSet<object>? visited = null)
    {
        visited ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        if (!visited.Add(root)) return null;

        foreach (var prop in root.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetIndexParameters().Length > 0) continue;

            object? value;
            try { value = prop.GetValue(root); }
            catch { continue; }

            if (value is null || value is string || value.GetType().IsValueType) continue;

            var propPath = currentPath.Length > 0 ? $"{currentPath}.{prop.Name}" : prop.Name;

            if (ReferenceEquals(value, target)) return propPath;

            if (value is IEnumerable enumerable)
            {
                int idx = 0;
                foreach (var item in enumerable)
                {
                    if (item is null || item is string || item.GetType().IsValueType) { idx++; continue; }

                    var itemPath = $"{propPath}[{idx}]";

                    if (ReferenceEquals(item, target)) return itemPath;

                    var nestedResult = FindObjectPath(item, target, itemPath, visited);
                    if (nestedResult is not null) return nestedResult;

                    idx++;
                }
                continue;
            }

            var result = FindObjectPath(value, target, propPath, visited);
            if (result is not null) return result;
        }

        return null;
    }

    /// <summary>
    /// Mengonversi path properti FluentValidation (misal <c>"Address.Street"</c> atau <c>"Items[0].Name"</c>)
    /// menjadi <see cref="FieldIdentifier"/> yang dikenali Blazor.
    /// </summary>
    private static FieldIdentifier ToFieldIdentifier(EditContext editContext, string propertyPath)
    {
        if (string.IsNullOrEmpty(propertyPath))
            return new FieldIdentifier(editContext.Model, string.Empty);

        var segments = SplitPropertyPath(propertyPath);
        object current = editContext.Model;

        for (int i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];

            if (TryParseIndexedSegment(segment, out var collectionName, out var index))
            {
                var collProp = current.GetType().GetProperty(collectionName);
                if (collProp is null) return Fallback(editContext, propertyPath);

                var collection = collProp.GetValue(current);
                if (collection is not IEnumerable enumerable) return Fallback(editContext, propertyPath);

                var item = GetItemAtIndex(enumerable, index);
                if (item is null) return Fallback(editContext, propertyPath);

                current = item;
            }
            else
            {
                var prop = current.GetType().GetProperty(segment);
                if (prop is null) return Fallback(editContext, propertyPath);

                var value = prop.GetValue(current);
                if (value is null) return Fallback(editContext, propertyPath);

                current = value;
            }
        }

        var lastSegment = segments[^1];

        // Segmen terakhir bisa berupa indexed (misal collection of value types).
        if (TryParseIndexedSegment(lastSegment, out var lastCollName, out var lastIndex))
        {
            var collProp = current.GetType().GetProperty(lastCollName);
            if (collProp is not null)
            {
                var collection = collProp.GetValue(current);
                if (collection is IEnumerable enumerable)
                {
                    var item = GetItemAtIndex(enumerable, lastIndex);
                    if (item is not null) return new FieldIdentifier(item, string.Empty);
                }
            }
            return Fallback(editContext, propertyPath);
        }

        return new FieldIdentifier(current, lastSegment);
    }

    // -------------------------------------------------------------------------
    // Path parsing helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Memisahkan path properti pada titik (<c>.</c>) yang berada di luar kurung kotak.
    /// Contoh: <c>"Items[0].Name"</c> → <c>["Items[0]", "Name"]</c>.
    /// </summary>
    private static string[] SplitPropertyPath(string path)
    {
        var segments = new List<string>();
        int start = 0, depth = 0;

        for (int i = 0; i < path.Length; i++)
        {
            if (path[i] == '[') depth++;
            else if (path[i] == ']') depth--;
            else if (path[i] == '.' && depth == 0)
            {
                segments.Add(path[start..i]);
                start = i + 1;
            }
        }

        if (start < path.Length)
            segments.Add(path[start..]);

        return [.. segments];
    }

    /// <summary>
    /// Mem-parse segmen berindeks seperti <c>"Items[2]"</c> menjadi nama properti (<c>"Items"</c>)
    /// dan indeks (<c>2</c>).
    /// </summary>
    private static bool TryParseIndexedSegment(string segment, out string propertyName, out int index)
    {
        propertyName = string.Empty;
        index = 0;

        var bracketOpen = segment.IndexOf('[');
        if (bracketOpen < 0) return false;

        var bracketClose = segment.IndexOf(']', bracketOpen);
        if (bracketClose < 0) return false;

        propertyName = segment[..bracketOpen];
        var indexStr = segment[(bracketOpen + 1)..bracketClose];
        return int.TryParse(indexStr, out index);
    }

    private static object? GetItemAtIndex(IEnumerable collection, int index)
    {
        int i = 0;
        foreach (var item in collection)
        {
            if (i == index) return item;
            i++;
        }
        return null;
    }

    private static FieldIdentifier Fallback(EditContext editContext, string propertyPath)
        => new(editContext.Model, propertyPath);

    // -------------------------------------------------------------------------
    // Dispose
    // -------------------------------------------------------------------------

    /// <inheritdoc />
    public void Dispose()
    {
        UnsubscribeFromContext(_subscribedContext);
        _cts.Cancel();
        _cts.Dispose();
    }
}
