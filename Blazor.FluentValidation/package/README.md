![Blazor.FluentValidation](https://raw.githubusercontent.com/ganiputras/Blazor.FluentValidation/refs/heads/master/logo.png)

# Blazor.FluentValidation

Integrasi otomatis [FluentValidation](https://docs.fluentvalidation.net) ke dalam `<EditForm>` Blazor.  
Kompatibel dengan .NET 8+, mendukung nested object, collection, rule set, dan validasi real-time per field.

[![NuGet](https://img.shields.io/nuget/v/Blazor.FluentValidation.svg?style=flat-square)](https://www.nuget.org/packages/Blazor.FluentValidation)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://licenses.nuget.org/MIT)

---

## Fitur Utama

- Integrasi langsung ke `<EditForm>` tanpa konfigurasi kompleks
- **Auto-discovery validator** dari DI berdasarkan tipe model — tanpa perlu set parameter apapun
- Support `ValidatorType` (resolve dari DI) dan `ValidatorInstance` (instance manual)
- Validasi nested object otomatis (`Address.City`, `Order.Items[0].Name`)
- Dukungan collection dengan indexer (`Items[0]`, `Tags[2]`)
- Validasi real-time saat field berubah (`OnFieldChanged`)
- Support **Rule Set** FluentValidation via parameter `RuleSets`
- Opsi menonaktifkan validasi per field via `ValidateOnFieldChange`
- Penanganan error yang aman — tidak merusak Blazor Server circuit

---

## Instalasi

```shell
dotnet add package Blazor.FluentValidation
```

---

## Registrasi DI

Daftarkan validator ke DI container di `Program.cs`:

```csharp
using FluentValidation;

// Satu assembly
builder.Services.AddValidatorsFromAssemblyContaining<PersonValidator>();

// Atau semua assembly yang ter-load
var assemblies = AppDomain.CurrentDomain
    .GetAssemblies()
    .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location));

builder.Services.AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true);
```

---

## Parameter

| Parameter | Tipe | Default | Keterangan |
|---|---|---|---|
| `ValidatorType` | `Type?` | `null` | Tipe validator yang di-resolve dari DI. Diabaikan jika `ValidatorInstance` diset. |
| `ValidatorInstance` | `IValidator?` | `null` | Instance validator manual. Jika diset, `ValidatorType` diabaikan. |
| `RuleSets` | `string[]?` | `null` | Rule set FluentValidation yang dieksekusi. `null` = semua rule dijalankan. |
| `ValidateOnFieldChange` | `bool` | `true` | Jika `false`, validasi hanya dijalankan saat form disubmit. |

---

## Contoh Penggunaan

### Mode 1: Auto-Discovery (Direkomendasikan)

Jika `IValidator<TModel>` sudah terdaftar di DI, tidak perlu set parameter apapun:

```razor
<EditForm Model="@person" OnValidSubmit="@HandleSubmit">
    <FluentValidationExtension />

    <InputText @bind-Value="person.Name" />
    <ValidationMessage For="@(() => person.Name)" />

    <button type="submit">Simpan</button>
</EditForm>
```

### Mode 2: ValidatorType (dari DI)

```razor
<EditForm Model="@person" OnValidSubmit="@HandleSubmit">
    <FluentValidationExtension ValidatorType="typeof(PersonValidator)" />

    <InputText @bind-Value="person.Name" />
    <ValidationMessage For="@(() => person.Name)" />
</EditForm>
```

### Mode 3: ValidatorInstance (manual, tanpa DI)

```razor
<EditForm Model="@person" OnValidSubmit="@HandleSubmit">
    <FluentValidationExtension ValidatorInstance="@(new PersonValidator())" />

    <InputText @bind-Value="person.Name" />
    <ValidationMessage For="@(() => person.Name)" />
</EditForm>
```

### Mode 4: Rule Set

```razor
<EditForm Model="@person" OnValidSubmit="@HandleSubmit">
    <FluentValidationExtension RuleSets="@(new[] { "Create", "Default" })" />

    <InputText @bind-Value="person.Name" />
    <ValidationMessage For="@(() => person.Name)" />
</EditForm>
```

### Mode 5: Hanya Validasi saat Submit

```razor
<EditForm Model="@person" OnValidSubmit="@HandleSubmit">
    <FluentValidationExtension ValidateOnFieldChange="false" />

    <InputText @bind-Value="person.Name" />
    <ValidationMessage For="@(() => person.Name)" />
</EditForm>
```

---

## Nested Object

### Model

```csharp
public class Person
{
    public string Name { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
}

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}
```

### Validator

```csharp
public class PersonValidator : AbstractValidator<Person>
{
    public PersonValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Nama wajib diisi.")
            .MinimumLength(3).WithMessage("Minimal 3 karakter.");

        RuleFor(p => p.Address)
            .SetValidator(new AddressValidator());
    }
}

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(a => a.Street).NotEmpty().WithMessage("Jalan wajib diisi.");
        RuleFor(a => a.City).NotEmpty().WithMessage("Kota wajib diisi.");
    }
}
```

### Form

```razor
<EditForm Model="@person" OnValidSubmit="@HandleSubmit">
    <FluentValidationExtension />

    <InputText @bind-Value="person.Name" />
    <ValidationMessage For="@(() => person.Name)" />

    <InputText @bind-Value="person.Address.Street" />
    <ValidationMessage For="@(() => person.Address.Street)" />

    <InputText @bind-Value="person.Address.City" />
    <ValidationMessage For="@(() => person.Address.City)" />

    <button type="submit">Simpan</button>
</EditForm>
```

---

## Collection

```csharp
public class Order
{
    public List<OrderItem> Items { get; set; } = [];
}

public class OrderItem
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleForEach(o => o.Items).SetValidator(new OrderItemValidator());
    }
}

public class OrderItemValidator : AbstractValidator<OrderItem>
{
    public OrderItemValidator()
    {
        RuleFor(i => i.ProductName).NotEmpty().WithMessage("Nama produk wajib diisi.");
        RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Jumlah harus lebih dari 0.");
    }
}
```

```razor
<EditForm Model="@order" OnValidSubmit="@HandleSubmit">
    <FluentValidationExtension />

    @for (int i = 0; i < order.Items.Count; i++)
    {
        var item = order.Items[i];

        <InputText @bind-Value="item.ProductName" />
        <ValidationMessage For="@(() => item.ProductName)" />

        <InputNumber @bind-Value="item.Quantity" />
        <ValidationMessage For="@(() => item.Quantity)" />
    }

    <button type="submit">Pesan</button>
</EditForm>
```

---

## Catatan untuk .NET 8+ (Static SSR)

Jika menggunakan `[SupplyParameterFromForm]`, wajib menyertakan `FormName` yang unik:

```razor
<EditForm FormName="personForm" Model="@person" OnValidSubmit="@HandleSubmit">
    <FluentValidationExtension />
    ...
</EditForm>
```

Tanpa `FormName`, Blazor akan melempar error:

```
The POST request does not specify which form is being submitted.
```

---

## Kontribusi & Dukungan

Kontribusi terbuka. Silakan laporkan issue, ajukan fitur, atau buat pull request di:

[https://github.com/ganiputras/Blazor.FluentValidation](https://github.com/ganiputras/Blazor.FluentValidation)

---

## Lisensi

Blazor.FluentValidation dirilis di bawah [MIT License](https://licenses.nuget.org/MIT).
