using FluentValidation;

namespace ePrevzem.Application.Identity.RefreshOrgAdminToken;

public sealed class RefreshOrganizationAdminTokenValidator : AbstractValidator<RefreshOrganizationAdminTokenCommand>
{
    public RefreshOrganizationAdminTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
