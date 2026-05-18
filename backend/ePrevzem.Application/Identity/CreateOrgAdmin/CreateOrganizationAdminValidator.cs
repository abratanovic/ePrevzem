using FluentValidation;

namespace ePrevzem.Application.Identity.CreateOrgAdmin;

public sealed class CreateOrganizationAdminValidator : AbstractValidator<CreateOrganizationAdminCommand>
{
    public CreateOrganizationAdminValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.OrganizationId)
            .NotEmpty();
    }
}
