using FluentValidation;

namespace ePrevzem.Application.Identity.LoginUnified;

public sealed class LoginUnifiedValidator : AbstractValidator<LoginUnifiedCommand>
{
    public LoginUnifiedValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
