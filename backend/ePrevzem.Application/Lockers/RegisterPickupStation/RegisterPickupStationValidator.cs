using FluentValidation;

namespace ePrevzem.Application.Lockers.RegisterPickupStation;

public sealed class RegisterPickupStationValidator : AbstractValidator<RegisterPickupStationCommand>
{
    public RegisterPickupStationValidator()
    {
        RuleFor(x => x.SerialNumber)
            .NotEmpty();

        RuleFor(x => x.LockerNumbers)
            .NotEmpty()
            .Must(numbers => numbers.Distinct().Count() == numbers.Count)
            .WithMessage("Locker numbers must be unique.");

        RuleForEach(x => x.LockerNumbers)
            .GreaterThan(0)
            .WithMessage("Each locker number must be positive.");
    }
}
