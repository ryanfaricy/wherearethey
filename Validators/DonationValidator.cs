using FluentValidation;
using WhereAreThey.Models;

namespace WhereAreThey.Validators;

public class DonationValidator : AbstractValidator<Donation>
{
    public DonationValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Donation amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .MaximumLength(3)
            .WithMessage("Currency must be a 3-letter code.");

        RuleFor(x => x.DonorEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.DonorEmail))
            .WithMessage("Please provide a valid email address.");

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required.");
    }
}
