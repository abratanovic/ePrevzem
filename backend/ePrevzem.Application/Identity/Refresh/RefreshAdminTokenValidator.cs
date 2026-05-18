using FluentValidation;

namespace ePrevzem.Application.Identity.Refresh;

public sealed class RefreshAdminTokenValidator : AbstractValidator<RefreshAdminTokenCommand>
{
    public RefreshAdminTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}
