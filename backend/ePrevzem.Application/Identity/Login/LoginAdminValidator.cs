using FluentValidation;

namespace ePrevzem.Application.Identity.Login;

public sealed class LoginAdminValidator : AbstractValidator<LoginAdminCommand>
{
    public LoginAdminValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(256);
    }
}
