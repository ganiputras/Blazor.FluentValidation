![Blazor.FluentValidation](https://raw.githubusercontent.com/ganiputras/Blazor.FluentValidation/refs/heads/master/logo.png)

# Blazor.FluentValidation

🚀 Integrasi otomatis [FluentValidation](https://docs.fluentvalidation.net) ke dalam `<EditForm>` Blazor.  
✨ Kompatibel dengan .NET 8+, mendukung nested validation, DI, dan validasi per field secara real-time!

[![NuGet](https://img.shields.io/nuget/v/Blazor.FluentValidation.svg?style=flat-square)](https://www.nuget.org/packages/Blazor.FluentValidation)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://licenses.nuget.org/MIT)

---

## 🧩 Fitur Utama

- 🔗 Integrasi langsung ke `<EditForm>` — tanpa konfigurasi kompleks
- 💉 Support `ValidatorType` via Dependency Injection (DI)
- 🛠️ Support `ValidatorInstance` tanpa DI (manual)
- 🧠 Validasi properti nested seperti `Address.City` otomatis tampil
- ⚡ Validasi real-time saat field diubah (`OnFieldChanged`)
- 🔐 Kompatibel dengan `[SupplyParameterFromForm]` (.NET 8+ Interactive Rendering)

---

## 📦 Instalasi

```bash
dotnet add package Blazor.FluentValidation
```


## 🔧 Registrasi DI (jika pakai ValidatorType)
```bash
using FluentValidation;
builder.Services.AddValidatorsFromAssemblyContaining<PersonValidator>();

// atau untuk banyak assembly
var assemblies = AppDomain.CurrentDomain.GetAssemblies();
builder.Services.AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true);
```
## ✅ Contoh Penggunaan

🧷 Mode 1: ValidatorType (dari DI)
```bash
<EditForm Model="@person" OnValidSubmit="@HandleSubmit" FormName="formA">
    <FluentValidationExtension ValidatorType="typeof(PersonValidator)" />
    <InputText @bind-Value="person.Name" />
    <ValidationMessage For="@(() => person.Name)" />
</EditForm>

 ```
  
✍️ Mode 2: ValidatorInstance (manual)
```bash
<EditForm Model="@person" OnValidSubmit="@HandleSubmit" FormName="formB">
    <FluentValidationExtension ValidatorInstance="new PersonValidator()" />
    <InputText @bind-Value="person.Name" />
    <ValidationMessage For="@(() => person.Name)" />
</EditForm>

 ```

👪 Contoh Nested Validation (Parent-Child)

🔹 Model
```bash
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

🔹 Validator
```bash
public class PersonValidator : AbstractValidator<Person>
{
    public PersonValidator()
    {
        RuleFor(p => p.Name).NotEmpty().WithMessage("Nama wajib diisi.");
        RuleFor(p => p.Address).SetValidator(new AddressValidator());
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

🔹 Razor Form
```bash
<EditForm Model="@person" OnValidSubmit="@HandleSubmit" FormName="nestedForm">
    <FluentValidationExtension ValidatorType="typeof(PersonValidator)" />

    <InputText @bind-Value="person.Name" />
    <ValidationMessage For="@(() => person.Name)" />

    <InputText @bind-Value="person.Address.Street" />
    <ValidationMessage For="@(() => person.Address.Street)" />

    <InputText @bind-Value="person.Address.City" />
    <ValidationMessage For="@(() => person.Address.City)" />
</EditForm>

 ```   


⚠️ Penting untuk .NET 8+

Jika Anda menggunakan [SupplyParameterFromForm], pastikan menyertakan FormName:
```bash
<EditForm FormName="uniqueFormName" ... />
 ```   
Tanpa FormName, Blazor akan menampilkan error seperti:
```bash
The POST request does not specify which form is being submitted.
 ```   

## 💬 Kontribusi & Dukungan

Kontribusi terbuka!
Silakan laporkan issue, ajukan fitur, atau buat pull request di:

🔗 https://github.com/ganiputras/Blazor.FluentValidation

## ⚖️ Lisensi
Blazor.FluentValidation dirilis di bawah MIT License