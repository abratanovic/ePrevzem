using FluentValidation;

namespace ePrevzem.Application.Identity.ChangePasswordUnified;

public sealed class ChangePasswordUnifiedValidator : AbstractValidator<ChangePasswordUnifiedCommand>
{
    public ChangePasswordUnifiedValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
