using FluentValidation;

namespace ePrevzem.Application.Identity.ChangeOrgAdminPassword;

public sealed class ChangeOrganizationAdminPasswordValidator
    : AbstractValidator<ChangeOrganizationAdminPasswordCommand>
{
    public ChangeOrganizationAdminPasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
