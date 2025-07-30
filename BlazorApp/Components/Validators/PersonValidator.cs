using BlazorApp.Components.Models;
using FluentValidation;

namespace BlazorApp.Components.Validators;

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