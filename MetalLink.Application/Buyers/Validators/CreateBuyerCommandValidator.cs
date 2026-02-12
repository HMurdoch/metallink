using FluentValidation;
using MetalLink.Application.Buyers.Commands;

namespace MetalLink.Application.Buyers.Validators;

public sealed class CreateBuyerCommandValidator
    : AbstractValidator<CreateBuyerCommand>
{
    public CreateBuyerCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.IsCompany);

        RuleFor(x => x.SiteId)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.IsCompany);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.IdNumber)
            .MaximumLength(50);

        RuleFor(x => x.AccountNumber)
            .GreaterThan(0)
            .WithMessage("Account Number must be greater than 0.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
