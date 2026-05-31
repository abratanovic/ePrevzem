using FluentValidation;

namespace ePrevzem.Application.Identity.RegisterCitizen;

public sealed class RegisterCitizenValidator : AbstractValidator<RegisterCitizenCommand>
{
    public RegisterCitizenValidator()
    {
        RuleFor(x => x.SiTrustToken).NotEmpty();
    }
}
