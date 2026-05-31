using FluentValidation;

namespace ePrevzem.Application.Organizations.AddMember;

public sealed class AddEmployeeMemberValidator : AbstractValidator<AddEmployeeMemberCommand>
{
    public AddEmployeeMemberValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
