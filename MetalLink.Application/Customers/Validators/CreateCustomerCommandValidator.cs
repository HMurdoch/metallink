using FluentValidation;
using MetalLink.Application.Customers.Commands;

namespace MetalLink.Application.Customers.Validators;

public sealed class CreateCustomerCommandValidator
    : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
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
