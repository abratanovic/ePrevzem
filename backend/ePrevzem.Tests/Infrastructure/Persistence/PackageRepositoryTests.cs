using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Infrastructure.Persistence;
using ePrevzem.Infrastructure.Pickups;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ePrevzem.Tests.Infrastructure.Persistence;

public class PackageRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetByIdForOrganizationAsync_loads_placement_history()
    {
        var options = new DbContextOptionsBuilder<EPrevzemDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var organizationId = OrganizationId.New();
        var package = Package.CreateByEmployee(
            PackageId.New(),
            organizationId,
            CitizenUserId.New(),
            EmployeeAccountId.New(),
            PickupStationId.New(),
            "EP-2026-000123",
            "Osebna izkaznica",
            Now);
        package.Place(
            PlacementId.New(),
            LockerId.New(),
            EmployeeAccountId.New(),
            TimeSpan.FromDays(5),
            Now);
        package.RemoveByEmployee(EmployeeAccountId.New(), Now.AddMinutes(1));

        await using (var writeDb = new EPrevzemDbContext(options))
        {
            writeDb.Packages.Add(package);
            await writeDb.SaveChangesAsync();
        }

        await using var readDb = new EPrevzemDbContext(options);
        var repository = new PackageRepository(readDb);

        var loaded = await repository.GetByIdForOrganizationAsync(package.Id, organizationId);

        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(PackageStatus.AwaitingPlacement);
        loaded.Placements.Should().ContainSingle();
    }
}
