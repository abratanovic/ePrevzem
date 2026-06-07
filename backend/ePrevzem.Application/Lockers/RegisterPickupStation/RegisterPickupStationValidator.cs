using FluentValidation;

namespace ePrevzem.Application.Lockers.RegisterPickupStation;

public sealed class RegisterPickupStationValidator : AbstractValidator<RegisterPickupStationCommand>
{
    public RegisterPickupStationValidator()
    {
        RuleFor(x => x.SerialNumber)
            .NotEmpty();

        RuleFor(x => x.Lockers)
            .NotEmpty()
            .Must(lockers => lockers.Select(l => l.Number).Distinct().Count() == lockers.Count)
            .WithMessage("Locker numbers must be unique.");

        RuleForEach(x => x.Lockers)
            .Must(l => l.Number > 0)
            .WithMessage("Each locker number must be positive.")
            .Must(l => l.BoxId > 0)
            .WithMessage("Each locker box id must be positive.");
    }
}
