using FluentValidation;

namespace ePrevzem.Application.Organizations.Create;

public sealed class CreateOrganizationValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.TaxNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.RegistrationNumber)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.DefaultPickupDuration)
            .GreaterThan(TimeSpan.Zero);
    }
}
