using BlazorApp.Components.Models;
using FluentValidation;

namespace BlazorApp.Components.Validators;

public class AddressValidator : AbstractValidator<Address>
{
    public AddressValidator()
    {
        RuleFor(a => a.Street)
            .NotEmpty().WithMessage("Jalan tidak boleh kosong.");

        RuleFor(a => a.City)
            .NotEmpty().WithMessage("Kota tidak boleh kosong.");
    }
}