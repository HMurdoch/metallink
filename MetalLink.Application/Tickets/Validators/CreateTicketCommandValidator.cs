using FluentValidation;
using MetalLink.Application.Tickets.Commands;

namespace MetalLink.Application.Tickets.Validators;

public sealed class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.SiteId)
            .GreaterThan(0);

        RuleFor(x => x.CustomerId)
            .GreaterThan(0);

        RuleFor(x => x.OperatorId)
            .GreaterThan(0);

        RuleFor(x => x.TicketType)
            .NotEmpty()
            .Must(t => t == "weighbridge" || t == "platform")
            .WithMessage("TicketType must be 'weighbridge' or 'platform'.");

        RuleFor(x => x.TicketNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.UnitPricePerKg)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.ProductDescription)
            .MaximumLength(200);

        RuleFor(x => x.Notes)
            .MaximumLength(500);
    }
}
