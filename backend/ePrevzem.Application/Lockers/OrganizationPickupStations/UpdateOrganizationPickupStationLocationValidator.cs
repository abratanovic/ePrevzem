using FluentValidation;

namespace ePrevzem.Application.Lockers.OrganizationPickupStations;

public sealed class UpdateOrganizationPickupStationLocationValidator
    : AbstractValidator<UpdateOrganizationPickupStationLocationCommand>
{
    public UpdateOrganizationPickupStationLocationValidator()
    {
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m);

        RuleFor(x => x.Address)
            .NotEmpty();

        RuleFor(x => x.HouseNumber)
            .NotEmpty();

        RuleFor(x => x.ZipCode)
            .NotEmpty();

        RuleFor(x => x.City)
            .NotEmpty();
    }
}
