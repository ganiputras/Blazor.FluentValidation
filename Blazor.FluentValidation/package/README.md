# 🔢 Blazor NumberInput Component

Komponen Blazor kustom untuk input angka dengan dukungan validasi, format lokal, dan integrasi form yang fleksibel. Terdiri dari dua komponen utama:

---

## 📦 Komponen

### 1. `<NumberInput>`
Komponen ringan dan fleksibel untuk input angka mandiri, tanpa tergantung pada `EditForm`.

#### ✅ Fitur Utama
- Binding: `@bind-NumberValue`
- Format angka (`N0`, `F2`, dsb)
- Validasi manual via parameter `Required`
- Prefix/Suffix (misal: Rp, %, kg)
- Dukungan `MinValue`, `MaxValue`, `Step`
- Dukungan parsing lokal (`CultureInfo`)
- Navigasi keyboard ↑ ↓
- Event interaktif: `@oninput`, `@onchange`, `@onpaste`
- Atribut tambahan HTML (`AdditionalAttributes`)

#### 🧩 Contoh
```razor
<NumberInput @bind-NumberValue="amount"
             Placeholder="Masukkan jumlah"
             DisplayPrefix="Rp"
             Format="N0"
             Required="true"
             RequiredValueValidationMessage="Wajib diisi" />
```

---

### 2. `<NumberInputBase>`
Turunan dari `InputBase<decimal?>`, ideal untuk integrasi `EditForm` dan validasi otomatis dengan anotasi data (`[Required]`, `[Range]`, dsb).

#### ✅ Fitur Tambahan
- Binding: `@bind-Value`
- Terintegrasi otomatis dengan `EditForm`
- Validasi otomatis dengan `DataAnnotations`
- Mendukung `ValidationMessage`
- Dukungan semua fitur `<NumberInput>` (format, batasan, kultur, dsb)
- Mengikuti pipeline Blazor standar

#### 🧩 Contoh
```razor
<EditForm Model="model" OnValidSubmit="Submit">
    <DataAnnotationsValidator />
    <NumberInputBase @bind-Value="model.Amount"
                     Placeholder="Masukkan jumlah"
                     DisplayPrefix="Rp"
                     Format="N0" />
    <ValidationMessage For="@(() => model.Amount)" />
</EditForm>
```

---

## 🔬 Perbandingan Singkat

| Fitur / Aspek                 | `<NumberInput>`                    | `<NumberInputBase>` (InputBase)   |
|------------------------------|------------------------------------|-----------------------------------|
| Binding                      | `@bind-NumberValue`                | `@bind-Value`                     |
| Validasi Otomatis            | ❌ Manual                          | ✅ Ya, via `EditForm`             |
| Digunakan dalam `EditForm`   | Opsional                           | ✅ Direkomendasikan               |
| Prefix / Suffix              | ✅                                  | ✅                                |
| Keyboard ↑ ↓ Step            | ✅                                  | ✅                                |
| Atribut HTML tambahan        | ✅                                  | ✅                                |
| Reusabilitas (form standar)  | ⚠️ Perlu usaha                     | ✅ Sesuai konvensi Blazor         |
| Berat & fleksibilitas        | Ringan & bebas                     | Standar & konsisten               |

---

## 📘 Dokumentasi Properti Umum

| Parameter                    | Tipe                          | Deskripsi                                                 |
|-----------------------------|-------------------------------|-----------------------------------------------------------|
| `NumberValue` / `Value`     | `decimal?`                    | Nilai angka (nullable).                                   |
| `Format`                    | `string`                      | Format angka, contoh: `"N0"`, `"F2"`                      |
| `DecimalPlaces`             | `int`                         | Presisi parsing desimal                                   |
| `Culture`                   | `CultureInfo`                 | Kultur lokal seperti `new("id-ID")`                       |
| `MinValue`, `MaxValue`      | `decimal?`                    | Batas bawah & atas nilai                                  |
| `Step`                      | `decimal?`                    | Besar kenaikan nilai saat `↑↓`                            |
| `Placeholder`               | `string?`                     | Placeholder input                                         |
| `DisplayPrefix` / Suffix    | `string?`                     | Teks tambahan sebelum/sesudah nilai                       |
| `Required`                  | `bool?`                       | Menandakan wajib diisi (manual)                           |
| `RequiredValueValidationMessage` | `string?`               | Pesan validasi jika `Required=true`                      |
| `AdditionalAttributes`      | `Dictionary<string, object>?` | Atribut tambahan seperti `data-*`, `style`, dll.          |

---

## 🚀 Rekomendasi Penggunaan

- Gunakan `<NumberInput>` jika:
  - Butuh komponen input angka ringan
  - Tidak menggunakan `EditForm`
  - Ingin kontrol validasi manual

- Gunakan `<NumberInputBase>` jika:
  - Menggunakan `EditForm`
  - Ingin validasi otomatis dari model
  - Mengikuti pola `InputBase<T>` Blazor

---

## 👨‍💻 Pengembang
Dibuat oleh [ganiputras](https://github.com/ganiputras) · MIT License · Kompatibel Blazor Server & WASM