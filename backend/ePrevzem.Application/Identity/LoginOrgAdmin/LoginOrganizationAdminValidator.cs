using FluentValidation;

namespace ePrevzem.Application.Identity.LoginOrgAdmin;

public sealed class LoginOrganizationAdminValidator : AbstractValidator<LoginOrganizationAdminCommand>
{
    public LoginOrganizationAdminValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
