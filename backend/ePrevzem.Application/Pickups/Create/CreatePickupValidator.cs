using FluentValidation;

namespace ePrevzem.Application.Pickups.Create;

public sealed class CreatePickupValidator : AbstractValidator<CreatePickupCommand>
{
    public CreatePickupValidator()
    {
        RuleFor(x => x.RecipientEmso)
            .NotEmpty()
            .Matches(@"^\d{13}$")
            .WithMessage("EMŠO mora vsebovati natanko 13 številk.");
        RuleFor(x => x.TargetPickupStationId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
    }
}
