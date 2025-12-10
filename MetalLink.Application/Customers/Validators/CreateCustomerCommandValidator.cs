using FluentValidation;
using MetalLink.Application.Customers.Commands;

namespace MetalLink.Application.Customers.Validators;

public sealed class CreateCustomerCommandValidator
    : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.SiteId)
            .GreaterThan(0);

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .When(x => x.IsCompany);

        RuleFor(x => x.IdNumber)
            .MaximumLength(50);

        RuleFor(x => x.AccountNumber)
            .MaximumLength(50);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
