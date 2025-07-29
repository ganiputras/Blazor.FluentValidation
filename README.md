![Blazor.FluentValidation](https://raw.githubusercontent.com/ganiputras/Blazor.FluentValidation/refs/heads/master/logo.png)

# Blazor.FluentValidation

**Integrasi otomatis FluentValidation dengan EditForm di Blazor (.NET 8+)**

[![NuGet](https://img.shields.io/nuget/v/Blazor.FluentValidation.svg?style=flat-square)](https://www.nuget.org/packages/Blazor.FluentValidation)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://github.com/ganiputras/Blazor.FluentValidation/blob/master/Blazor.FluentValidation/LICENSE.txt)

---

## ✨ Fitur

- 🔄 Integrasi otomatis dengan `<EditForm>` Blazor
- ✅ Mendukung validasi via `ValidatorType` (dari DI)
- 🔍 Alternatif: validasi dengan `ValidatorInstance` (manual)
- ⚠️ Kompatibel dengan `ValidationMessage`, `ValidationSummary`
- ⚡ Validasi instan saat field berubah (`OnFieldChanged`)
- 🧼 Tidak membutuhkan ekstensi `FluentValidation.AspNetCore`

---

## 🚀 Instalasi

Tambahkan package melalui NuGet CLI atau .NET CLI:

```bash
dotnet add package Blazor.FluentValidation
```

---

## 🛠️ Konfigurasi Validator

Buat class validator untuk model Anda menggunakan FluentValidation:

```csharp
using FluentValidation;

public class Person
{
    public string Name { get; set; } = string.Empty;
}

public class PersonValidator : AbstractValidator<Person>
{
    public PersonValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama wajib diisi");
    }
}
```

---

## ✅ Cara Penggunaan

### Opsi 1: Gunakan `ValidatorType` (lebih disarankan)

**Langkah 1 — Daftarkan validator di `Program.cs`:**

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<PersonValidator>();
```

**Langkah 2 — Gunakan di komponen Blazor:**

```razor
@page "/form"
@using FluentValidation
@using Blazor.FluentValidation

<EditForm Model="@person" OnValidSubmit="HandleSubmit">
    <FluentValidationValidator ValidatorType="typeof(PersonValidator)" />
    <InputText @bind-Value="person.Name" />
    <ValidationMessage For="@(() => person.Name)" />
    <button type="submit">Submit</button>
</EditForm>

@code {
    private Person person = new();

    private void HandleSubmit()
    {
        // Lakukan sesuatu saat validasi berhasil
    }
}
```

---

### Opsi 2: Gunakan `ValidatorInstance` secara manual

Jika kamu tidak menggunakan DI atau ingin kontrol penuh:

```razor
@inject IServiceProvider ServiceProvider

<EditForm Model="@person" OnValidSubmit="HandleSubmit">
    <FluentValidationValidator ValidatorInstance="ManualValidator" />
    <InputText @bind-Value="person.Name" />
    <ValidationMessage For="@(() => person.Name)" />
    <button type="submit">Submit</button>
</EditForm>

@code {
    private Person person = new();

    private IValidator<Person> ManualValidator =>
        new PersonValidator(); // Atau inject IServiceProvider jika perlu

    private void HandleSubmit()
    {
        // Lakukan sesuatu saat validasi berhasil
    }
}
```

---

## 🔄 Dukungan Validasi Blazor

Komponen ini secara otomatis menyinkronkan validasi FluentValidation dengan sistem `EditContext` milik Blazor:

- Mendukung `<ValidationMessage />`
- Mendukung `<ValidationSummary />`
- Error ditampilkan langsung saat submit atau saat field berubah (`OnFieldChanged`)

---

## 💡 Tips Tambahan

- Tidak perlu menggunakan `FluentValidation.AspNetCore` dalam Blazor
- Untuk validasi instan, gunakan binding dua arah `@bind-Value`
- Bisa dikombinasikan dengan komponen kustom

---

## 📦 Kompatibilitas

| Framework      | Status   |
|----------------|----------|
| .NET 8         | ✅ Full support |
| Blazor Server  | ✅ Full support |
| Blazor WASM    | ✅ Full support |
| FluentValidation 11–12 | ✅ Didukung |

---

## 📄 Lisensi

Blazor.FluentValidation dirilis di bawah lisensi MIT.  
Silakan digunakan untuk proyek pribadi maupun komersial.

📄 [Lihat file LICENSE](https://github.com/ganiputras/Blazor.FluentValidation/blob/master/Blazor.FluentValidation/LICENSE.txt)

---

## 🔗 Tautan Terkait

- 💡 GitHub: [ganiputras/Blazor.FluentValidation](https://github.com/ganiputras/Blazor.FluentValidation)
- 📦 NuGet: [nuget.org/packages/Blazor.FluentValidation](https://www.nuget.org/packages/Blazor.FluentValidation)
