using ePrevzem.Domain.Identity;
using FluentValidation;

namespace ePrevzem.Application.Identity.IssueProvisioningCode;

public sealed class IssueProvisioningCodeValidator : AbstractValidator<IssueProvisioningCodeCommand>
{
    public IssueProvisioningCodeValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
        RuleFor(x => x.Roles).NotEmpty()
            .Must(roles => roles.All(r => r == EmployeeAccountRole.RecordManager || r == EmployeeAccountRole.Operator))
            .WithMessage("Only RecordManager and Operator roles are valid.");
        RuleFor(x => x.ExpiresInHours).InclusiveBetween(1, 168);
    }
}
