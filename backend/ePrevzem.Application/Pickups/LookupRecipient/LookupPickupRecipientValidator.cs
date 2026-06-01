using FluentValidation;

namespace ePrevzem.Application.Pickups.LookupRecipient;

public sealed class LookupPickupRecipientValidator : AbstractValidator<LookupPickupRecipientQuery>
{
    public LookupPickupRecipientValidator()
    {
        RuleFor(x => x.Emso)
            .NotEmpty()
            .Matches(@"^\d{13}$")
            .WithMessage("EMŠO mora vsebovati natanko 13 številk.");
    }
}
