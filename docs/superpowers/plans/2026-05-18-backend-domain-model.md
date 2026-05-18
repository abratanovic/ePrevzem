# ePrevzem Backend — Domain Layer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the domain layer described in `docs/superpowers/specs/2026-05-18-backend-domain-model-design.md` — entities, value objects, enums, strongly-typed IDs, guarded state transitions, domain events, invariants — with full xUnit + FluentAssertions test coverage of behavior. No EF Core, no MediatR, no ASP.NET in this plan.

**Architecture:** Clean Architecture monolith already scaffolded under `backend/`. Work happens entirely inside `backend/ePrevzem.Domain` (production code) and `backend/ePrevzem.Tests` (unit tests). Code is organized into feature folders per `backend/AGENTS.md`: `Organizations`, `Lockers`, `Identity`, `Pickups`, `Delegations`, `Audit`. Aggregates reference each other only by strongly-typed Id structs — never by entity references — so each feature folder is independently coherent. Test code mirrors the same feature folder layout.

**Tech Stack:** .NET 9, C# 13, `net9.0` target, nullable + implicit usings enabled. xUnit 2.9.x, FluentAssertions 8.x. No additional NuGet packages are required for Domain.

**Out of scope for this plan (each gets its own plan later):**
- EF Core configurations, the `EPrevzemDbContext`, migrations.
- Repositories, the audit MediatR pipeline behavior, domain-event dispatch infrastructure.
- Application-layer use cases, validators, DTOs, ports.
- API controllers, auth, OpenAPI, CORS.
- SI-TRUST adapter, Direct4.me locker gateway.

**Conventions used by every task:**
- All public types in `ePrevzem.Domain` go under namespace `ePrevzem.Domain.<Feature>` (e.g. `ePrevzem.Domain.Pickups`).
- Strongly-typed Ids are `readonly record struct` with a single `Guid Value` and a static `New()` factory.
- Aggregate roots inherit `AggregateRoot<TId>`; entities that are not aggregate roots but have their own identity inherit `Entity<TId>`. Pure value objects inherit nothing.
- Constructors are `private`; creation goes through a `public static Create(...)` factory that validates invariants. State changes happen through guarded instance methods that throw `InvalidOperationException` (illegal state transition) or `ArgumentException` (bad arguments) on violation.
- Domain events live next to the aggregate that raises them: `Pickups/Events/PackagePlaced.cs`.
- Persisted enums are stored as strings later in Infrastructure — but the Domain definition is a plain C# enum.
- All times are `DateTimeOffset` (UTC at write time). A clock value is **passed in** to factory/state methods that need "now"; the Domain itself never reads `DateTimeOffset.UtcNow`.

---

## File structure created by this plan

```
backend/ePrevzem.Domain/
├── Common/
│   ├── AggregateRoot.cs                 (already exists)
│   ├── Entity.cs                        (already exists)
│   └── IDomainEvent.cs                  (already exists)
├── Organizations/
│   ├── Organization.cs
│   ├── OrganizationId.cs
│   └── Events/
│       └── OrganizationCreated.cs
├── Lockers/
│   ├── PickupStation.cs
│   ├── PickupStationId.cs
│   ├── Locker.cs
│   ├── LockerId.cs
│   ├── Location.cs                      (value object)
│   ├── StationClaim.cs
│   ├── StationClaimId.cs
│   └── Events/
│       ├── StationClaimed.cs
│       └── StationReleased.cs
├── Identity/
│   ├── CitizenUser.cs
│   ├── CitizenUserId.cs
│   ├── CitizenDevice.cs
│   ├── CitizenDeviceId.cs
│   ├── EmployeeAccount.cs
│   ├── EmployeeAccountId.cs
│   ├── EmployeeAccountRole.cs           (enum)
│   ├── EmployeeAccountStatus.cs         (enum)
│   ├── EmployeeDevice.cs
│   ├── EmployeeDeviceId.cs
│   ├── ProvisioningCode.cs
│   ├── ProvisioningCodeId.cs
│   ├── SystemAdmin.cs
│   ├── SystemAdminId.cs
│   └── Events/
│       ├── CitizenOnboarded.cs
│       ├── CitizenDeviceRegistered.cs
│       ├── CitizenDeviceRevoked.cs
│       ├── EmployeeAccountCreated.cs
│       ├── EmployeeAccountDisabled.cs
│       ├── EmployeeAccountReenabled.cs
│       ├── EmployeeDeviceRegistered.cs
│       ├── EmployeeDeviceRevoked.cs
│       ├── ProvisioningCodeIssued.cs
│       └── ProvisioningCodeRedeemed.cs
├── Pickups/
│   ├── Package.cs
│   ├── PackageId.cs
│   ├── PackageStatus.cs                 (enum)
│   ├── Placement.cs
│   ├── PlacementId.cs
│   ├── PlacementEndReason.cs            (enum)
│   └── Events/
│       ├── PackageCreated.cs
│       ├── PackagePlaced.cs
│       ├── PackagePickedUpByCitizen.cs
│       ├── PackageRemovedByEmployee.cs
│       ├── PackageExpired.cs
│       ├── PackageRetrievedAfterExpiry.cs
│       ├── PackageMarkedPickedUpManually.cs
│       └── PackageCancelled.cs
├── Delegations/
│   ├── Delegation.cs
│   ├── DelegationId.cs
│   └── Events/
│       ├── DelegationCreated.cs
│       └── DelegationRevoked.cs
└── Audit/
    ├── AuditLogEntry.cs
    ├── AuditLogEntryId.cs
    ├── AuditAction.cs                   (enum — string-persisted later)
    ├── AuditActorKind.cs                (enum)
    └── AuditTargetKind.cs               (enum)

backend/ePrevzem.Tests/
└── Domain/
    ├── Organizations/
    │   └── OrganizationTests.cs
    ├── Lockers/
    │   ├── PickupStationTests.cs
    │   ├── LockerTests.cs
    │   ├── LocationTests.cs
    │   └── StationClaimTests.cs
    ├── Identity/
    │   ├── CitizenUserTests.cs
    │   ├── EmployeeAccountTests.cs
    │   ├── ProvisioningCodeTests.cs
    │   └── SystemAdminTests.cs
    ├── Pickups/
    │   ├── PackageCreationTests.cs
    │   ├── PackagePlacementTests.cs
    │   ├── PackagePickupTests.cs
    │   ├── PackageRemovalTests.cs
    │   ├── PackageExpiryTests.cs
    │   └── PackageCancellationTests.cs
    ├── Delegations/
    │   └── DelegationTests.cs
    └── Audit/
        └── AuditLogEntryTests.cs
```

---

## Task 1: Foundation — typed Ids, Location value object, shared enum hosting

**Files:**
- Create: `backend/ePrevzem.Domain/Lockers/Location.cs`
- Test: `backend/ePrevzem.Tests/Domain/Lockers/LocationTests.cs`

This task introduces the only value object in the domain (`Location`). Strongly-typed Ids are introduced *with* the aggregate that owns them in subsequent tasks (no shared base struct needed — a tiny record struct per Id is cheap and keeps each Id colocated with its aggregate).

- [ ] **Step 1: Write the failing Location tests**

`backend/ePrevzem.Tests/Domain/Lockers/LocationTests.cs`:
```csharp
using ePrevzem.Domain.Lockers;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Lockers;

public class LocationTests
{
    [Fact]
    public void Create_with_valid_fields_constructs_value_object()
    {
        var location = Location.Create(
            latitude: 46.0569m,
            longitude: 14.5058m,
            address: "Slovenska cesta",
            houseNumber: "11",
            zipCode: "1000",
            city: "Ljubljana");

        location.Latitude.Should().Be(46.0569m);
        location.Longitude.Should().Be(14.5058m);
        location.Address.Should().Be("Slovenska cesta");
        location.HouseNumber.Should().Be("11");
        location.ZipCode.Should().Be("1000");
        location.City.Should().Be("Ljubljana");
    }

    [Theory]
    [InlineData(-90.001)]
    [InlineData(90.001)]
    public void Create_with_out_of_range_latitude_throws(decimal latitude)
    {
        var act = () => Location.Create(latitude, 0m, "a", "1", "1000", "Ljubljana");
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("latitude");
    }

    [Theory]
    [InlineData(-180.001)]
    [InlineData(180.001)]
    public void Create_with_out_of_range_longitude_throws(decimal longitude)
    {
        var act = () => Location.Create(0m, longitude, "a", "1", "1000", "Ljubljana");
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("longitude");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_address_throws(string address)
    {
        var act = () => Location.Create(0m, 0m, address, "1", "1000", "Ljubljana");
        act.Should().Throw<ArgumentException>().WithParameterName("address");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_city_throws(string city)
    {
        var act = () => Location.Create(0m, 0m, "a", "1", "1000", city);
        act.Should().Throw<ArgumentException>().WithParameterName("city");
    }

    [Fact]
    public void Two_locations_with_same_values_are_equal()
    {
        var a = Location.Create(46m, 14m, "a", "1", "1000", "Ljubljana");
        var b = Location.Create(46m, 14m, "a", "1", "1000", "Ljubljana");
        a.Should().Be(b);
    }
}
```

- [ ] **Step 2: Run tests to confirm failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~LocationTests"`
Expected: compile error (`Location` does not exist).

- [ ] **Step 3: Implement Location**

`backend/ePrevzem.Domain/Lockers/Location.cs`:
```csharp
namespace ePrevzem.Domain.Lockers;

public sealed record Location
{
    public decimal Latitude { get; }
    public decimal Longitude { get; }
    public string Address { get; }
    public string HouseNumber { get; }
    public string ZipCode { get; }
    public string City { get; }

    private Location(
        decimal latitude,
        decimal longitude,
        string address,
        string houseNumber,
        string zipCode,
        string city)
    {
        Latitude = latitude;
        Longitude = longitude;
        Address = address;
        HouseNumber = houseNumber;
        ZipCode = zipCode;
        City = city;
    }

    public static Location Create(
        decimal latitude,
        decimal longitude,
        string address,
        string houseNumber,
        string zipCode,
        string city)
    {
        if (latitude < -90m || latitude > 90m)
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be in [-90, 90].");
        if (longitude < -180m || longitude > 180m)
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be in [-180, 180].");
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address is required.", nameof(address));
        if (string.IsNullOrWhiteSpace(houseNumber))
            throw new ArgumentException("House number is required.", nameof(houseNumber));
        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ArgumentException("Zip code is required.", nameof(zipCode));
        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));

        return new Location(latitude, longitude, address, houseNumber, zipCode, city);
    }
}
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~LocationTests"`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Lockers/Location.cs backend/ePrevzem.Tests/Domain/Lockers/LocationTests.cs
git commit -m "feat(domain): add Location value object under Lockers feature"
```

---

## Task 2: Organizations — Organization aggregate

**Files:**
- Create: `backend/ePrevzem.Domain/Organizations/OrganizationId.cs`
- Create: `backend/ePrevzem.Domain/Organizations/Organization.cs`
- Create: `backend/ePrevzem.Domain/Organizations/Events/OrganizationCreated.cs`
- Test: `backend/ePrevzem.Tests/Domain/Organizations/OrganizationTests.cs`

- [ ] **Step 1: Write the failing Organization tests**

`backend/ePrevzem.Tests/Domain/Organizations/OrganizationTests.cs`:
```csharp
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Organizations.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Organizations;

public class OrganizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_with_valid_fields_constructs_aggregate_and_raises_event()
    {
        var id = OrganizationId.New();
        var org = Organization.Create(
            id,
            name: "Notarska zbornica",
            taxNumber: "SI12345678",
            registrationNumber: "1234567000",
            defaultPickupDuration: TimeSpan.FromDays(5),
            now: Now);

        org.Id.Should().Be(id);
        org.Name.Should().Be("Notarska zbornica");
        org.TaxNumber.Should().Be("SI12345678");
        org.RegistrationNumber.Should().Be("1234567000");
        org.DefaultPickupDuration.Should().Be(TimeSpan.FromDays(5));
        org.CreatedAt.Should().Be(Now);
        org.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<OrganizationCreated>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_name_throws(string name)
    {
        var act = () => Organization.Create(OrganizationId.New(), name, "SI1", "1", TimeSpan.FromDays(1), Now);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_tax_number_throws(string taxNumber)
    {
        var act = () => Organization.Create(OrganizationId.New(), "n", taxNumber, "1", TimeSpan.FromDays(1), Now);
        act.Should().Throw<ArgumentException>().WithParameterName("taxNumber");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_registration_number_throws(string registrationNumber)
    {
        var act = () => Organization.Create(OrganizationId.New(), "n", "SI1", registrationNumber, TimeSpan.FromDays(1), Now);
        act.Should().Throw<ArgumentException>().WithParameterName("registrationNumber");
    }

    [Fact]
    public void Create_with_non_positive_pickup_duration_throws()
    {
        var act = () => Organization.Create(OrganizationId.New(), "n", "SI1", "1", TimeSpan.Zero, Now);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("defaultPickupDuration");
    }

    [Fact]
    public void ChangeDefaultPickupDuration_updates_value()
    {
        var org = Organization.Create(OrganizationId.New(), "n", "SI1", "1", TimeSpan.FromDays(5), Now);
        org.ChangeDefaultPickupDuration(TimeSpan.FromDays(10));
        org.DefaultPickupDuration.Should().Be(TimeSpan.FromDays(10));
    }

    [Fact]
    public void ChangeDefaultPickupDuration_with_non_positive_throws()
    {
        var org = Organization.Create(OrganizationId.New(), "n", "SI1", "1", TimeSpan.FromDays(5), Now);
        var act = () => org.ChangeDefaultPickupDuration(TimeSpan.Zero);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
```

- [ ] **Step 2: Run tests, confirm compile failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~OrganizationTests"`
Expected: compile error.

- [ ] **Step 3: Implement OrganizationId, Organization, OrganizationCreated**

`backend/ePrevzem.Domain/Organizations/OrganizationId.cs`:
```csharp
namespace ePrevzem.Domain.Organizations;

public readonly record struct OrganizationId(Guid Value)
{
    public static OrganizationId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Organizations/Events/OrganizationCreated.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Organizations.Events;

public sealed record OrganizationCreated(OrganizationId OrganizationId, DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Organizations/Organization.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Organizations.Events;

namespace ePrevzem.Domain.Organizations;

public sealed class Organization : AggregateRoot<OrganizationId>
{
    public string Name { get; private set; } = default!;
    public string TaxNumber { get; private set; } = default!;
    public string RegistrationNumber { get; private set; } = default!;
    public TimeSpan DefaultPickupDuration { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Organization() { }

    public static Organization Create(
        OrganizationId id,
        string name,
        string taxNumber,
        string registrationNumber,
        TimeSpan defaultPickupDuration,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(taxNumber))
            throw new ArgumentException("Tax number is required.", nameof(taxNumber));
        if (string.IsNullOrWhiteSpace(registrationNumber))
            throw new ArgumentException("Registration number is required.", nameof(registrationNumber));
        if (defaultPickupDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(defaultPickupDuration), defaultPickupDuration, "Default pickup duration must be positive.");

        var org = new Organization
        {
            Id = id,
            Name = name,
            TaxNumber = taxNumber,
            RegistrationNumber = registrationNumber,
            DefaultPickupDuration = defaultPickupDuration,
            CreatedAt = now
        };
        org.Raise(new OrganizationCreated(id, now));
        return org;
    }

    public void ChangeDefaultPickupDuration(TimeSpan newDuration)
    {
        if (newDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(newDuration), newDuration, "Default pickup duration must be positive.");
        DefaultPickupDuration = newDuration;
    }
}
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~OrganizationTests"`
Expected: PASS (7 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Organizations backend/ePrevzem.Tests/Domain/Organizations
git commit -m "feat(domain): add Organization aggregate"
```

---

## Task 3: Lockers — PickupStation aggregate (with Locker child entity)

**Files:**
- Create: `backend/ePrevzem.Domain/Lockers/PickupStationId.cs`
- Create: `backend/ePrevzem.Domain/Lockers/LockerId.cs`
- Create: `backend/ePrevzem.Domain/Lockers/Locker.cs`
- Create: `backend/ePrevzem.Domain/Lockers/PickupStation.cs`
- Test: `backend/ePrevzem.Tests/Domain/Lockers/PickupStationTests.cs`
- Test: `backend/ePrevzem.Tests/Domain/Lockers/LockerTests.cs`

`Locker` is part of the `PickupStation` aggregate (added/removed only via station methods). Lockers are referenced by `Placement` from a different aggregate, but only via `LockerId`.

- [ ] **Step 1: Write the failing Locker tests**

`backend/ePrevzem.Tests/Domain/Lockers/LockerTests.cs`:
```csharp
using ePrevzem.Domain.Lockers;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Lockers;

public class LockerTests
{
    [Fact]
    public void Locker_added_via_station_is_serviceable_by_default()
    {
        var station = NewStation();
        var locker = station.AddLocker(LockerId.New(), lockerNumber: 1);

        locker.LockerNumber.Should().Be(1);
        locker.IsServiceable.Should().BeTrue();
        locker.PickupStationId.Should().Be(station.Id);
    }

    [Fact]
    public void MarkOutOfService_flips_flag_to_false()
    {
        var station = NewStation();
        var locker = station.AddLocker(LockerId.New(), 1);

        locker.MarkOutOfService();

        locker.IsServiceable.Should().BeFalse();
    }

    [Fact]
    public void MarkServiceable_flips_flag_to_true()
    {
        var station = NewStation();
        var locker = station.AddLocker(LockerId.New(), 1);
        locker.MarkOutOfService();

        locker.MarkServiceable();

        locker.IsServiceable.Should().BeTrue();
    }

    private static PickupStation NewStation() => PickupStation.Create(
        PickupStationId.New(),
        Location.Create(46m, 14m, "a", "1", "1000", "Ljubljana"),
        new DateTimeOffset(2026, 5, 18, 10, 0, 0, TimeSpan.Zero));
}
```

`backend/ePrevzem.Tests/Domain/Lockers/PickupStationTests.cs`:
```csharp
using ePrevzem.Domain.Lockers;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Lockers;

public class PickupStationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static Location ValidLocation() => Location.Create(46m, 14m, "a", "1", "1000", "Ljubljana");

    [Fact]
    public void Create_with_valid_fields_constructs_station()
    {
        var id = PickupStationId.New();
        var station = PickupStation.Create(id, ValidLocation(), Now);

        station.Id.Should().Be(id);
        station.Location.Should().Be(ValidLocation());
        station.CreatedAt.Should().Be(Now);
        station.Lockers.Should().BeEmpty();
    }

    [Fact]
    public void AddLocker_appends_to_lockers_collection()
    {
        var station = PickupStation.Create(PickupStationId.New(), ValidLocation(), Now);
        var locker = station.AddLocker(LockerId.New(), 1);

        station.Lockers.Should().ContainSingle().Which.Should().BeSameAs(locker);
    }

    [Fact]
    public void AddLocker_with_duplicate_number_throws()
    {
        var station = PickupStation.Create(PickupStationId.New(), ValidLocation(), Now);
        station.AddLocker(LockerId.New(), 1);

        var act = () => station.AddLocker(LockerId.New(), 1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*locker number*1*");
    }

    [Fact]
    public void AddLocker_with_non_positive_number_throws()
    {
        var station = PickupStation.Create(PickupStationId.New(), ValidLocation(), Now);
        var act = () => station.AddLocker(LockerId.New(), 0);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("lockerNumber");
    }
}
```

- [ ] **Step 2: Run tests, confirm failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~Lockers.PickupStationTests|FullyQualifiedName~Lockers.LockerTests"`
Expected: compile error.

- [ ] **Step 3: Implement Ids, Locker, PickupStation**

`backend/ePrevzem.Domain/Lockers/PickupStationId.cs`:
```csharp
namespace ePrevzem.Domain.Lockers;

public readonly record struct PickupStationId(Guid Value)
{
    public static PickupStationId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Lockers/LockerId.cs`:
```csharp
namespace ePrevzem.Domain.Lockers;

public readonly record struct LockerId(Guid Value)
{
    public static LockerId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Lockers/Locker.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Lockers;

public sealed class Locker : Entity<LockerId>
{
    public PickupStationId PickupStationId { get; private set; }
    public int LockerNumber { get; private set; }
    public bool IsServiceable { get; private set; }

    private Locker() { }

    internal static Locker Create(LockerId id, PickupStationId stationId, int lockerNumber)
    {
        if (lockerNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(lockerNumber), lockerNumber, "Locker number must be positive.");

        return new Locker
        {
            Id = id,
            PickupStationId = stationId,
            LockerNumber = lockerNumber,
            IsServiceable = true
        };
    }

    public void MarkOutOfService() => IsServiceable = false;
    public void MarkServiceable() => IsServiceable = true;
}
```

`backend/ePrevzem.Domain/Lockers/PickupStation.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Lockers;

public sealed class PickupStation : AggregateRoot<PickupStationId>
{
    private readonly List<Locker> _lockers = new();

    public Location Location { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<Locker> Lockers => _lockers.AsReadOnly();

    private PickupStation() { }

    public static PickupStation Create(PickupStationId id, Location location, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(location);
        return new PickupStation
        {
            Id = id,
            Location = location,
            CreatedAt = now
        };
    }

    public Locker AddLocker(LockerId id, int lockerNumber)
    {
        if (_lockers.Any(l => l.LockerNumber == lockerNumber))
            throw new InvalidOperationException($"A locker with locker number {lockerNumber} already exists in this station.");

        var locker = Locker.Create(id, Id, lockerNumber);
        _lockers.Add(locker);
        return locker;
    }
}
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~Lockers.PickupStationTests|FullyQualifiedName~Lockers.LockerTests"`
Expected: PASS (7 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Lockers backend/ePrevzem.Tests/Domain/Lockers/PickupStationTests.cs backend/ePrevzem.Tests/Domain/Lockers/LockerTests.cs
git commit -m "feat(domain): add PickupStation aggregate with Locker child entity"
```

---

## Task 4: Lockers — StationClaim aggregate

**Files:**
- Create: `backend/ePrevzem.Domain/Lockers/StationClaimId.cs`
- Create: `backend/ePrevzem.Domain/Lockers/StationClaim.cs`
- Create: `backend/ePrevzem.Domain/Lockers/Events/StationClaimed.cs`
- Create: `backend/ePrevzem.Domain/Lockers/Events/StationReleased.cs`
- Test: `backend/ePrevzem.Tests/Domain/Lockers/StationClaimTests.cs`

`StationClaim` is its own aggregate. The "at most one active claim per station" invariant must be enforced at the persistence-layer / write-side service later — Domain only guards the per-claim lifecycle (claim → release).

- [ ] **Step 1: Write the failing StationClaim tests**

`backend/ePrevzem.Tests/Domain/Lockers/StationClaimTests.cs`:
```csharp
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Lockers.Events;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Lockers;

public class StationClaimTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Claim_creates_active_claim_and_raises_event()
    {
        var claim = StationClaim.Claim(
            StationClaimId.New(),
            PickupStationId.New(),
            OrganizationId.New(),
            Now);

        claim.ClaimedAt.Should().Be(Now);
        claim.ReleasedAt.Should().BeNull();
        claim.IsActive.Should().BeTrue();
        claim.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StationClaimed>();
    }

    [Fact]
    public void Release_sets_ReleasedAt_and_raises_event()
    {
        var claim = StationClaim.Claim(StationClaimId.New(), PickupStationId.New(), OrganizationId.New(), Now);
        var later = Now.AddDays(1);

        claim.Release(later);

        claim.ReleasedAt.Should().Be(later);
        claim.IsActive.Should().BeFalse();
        claim.DomainEvents.OfType<StationReleased>().Should().ContainSingle();
    }

    [Fact]
    public void Release_when_already_released_throws()
    {
        var claim = StationClaim.Claim(StationClaimId.New(), PickupStationId.New(), OrganizationId.New(), Now);
        claim.Release(Now.AddDays(1));

        var act = () => claim.Release(Now.AddDays(2));
        act.Should().Throw<InvalidOperationException>().WithMessage("*already released*");
    }

    [Fact]
    public void Release_before_ClaimedAt_throws()
    {
        var claim = StationClaim.Claim(StationClaimId.New(), PickupStationId.New(), OrganizationId.New(), Now);
        var act = () => claim.Release(Now.AddSeconds(-1));
        act.Should().Throw<ArgumentException>().WithParameterName("releasedAt");
    }
}
```

- [ ] **Step 2: Run tests, confirm failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~StationClaimTests"`
Expected: compile error.

- [ ] **Step 3: Implement StationClaim**

`backend/ePrevzem.Domain/Lockers/StationClaimId.cs`:
```csharp
namespace ePrevzem.Domain.Lockers;

public readonly record struct StationClaimId(Guid Value)
{
    public static StationClaimId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Lockers/Events/StationClaimed.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Lockers.Events;

public sealed record StationClaimed(
    StationClaimId StationClaimId,
    PickupStationId PickupStationId,
    OrganizationId OrganizationId,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Lockers/Events/StationReleased.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Lockers.Events;

public sealed record StationReleased(
    StationClaimId StationClaimId,
    PickupStationId PickupStationId,
    OrganizationId OrganizationId,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Lockers/StationClaim.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Lockers.Events;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Lockers;

public sealed class StationClaim : AggregateRoot<StationClaimId>
{
    public PickupStationId PickupStationId { get; private set; }
    public OrganizationId OrganizationId { get; private set; }
    public DateTimeOffset ClaimedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }

    public bool IsActive => ReleasedAt is null;

    private StationClaim() { }

    public static StationClaim Claim(
        StationClaimId id,
        PickupStationId stationId,
        OrganizationId organizationId,
        DateTimeOffset now)
    {
        var claim = new StationClaim
        {
            Id = id,
            PickupStationId = stationId,
            OrganizationId = organizationId,
            ClaimedAt = now
        };
        claim.Raise(new StationClaimed(id, stationId, organizationId, now));
        return claim;
    }

    public void Release(DateTimeOffset releasedAt)
    {
        if (ReleasedAt is not null)
            throw new InvalidOperationException("Station claim has already been released.");
        if (releasedAt < ClaimedAt)
            throw new ArgumentException("Released-at timestamp must be on or after claimed-at.", nameof(releasedAt));

        ReleasedAt = releasedAt;
        Raise(new StationReleased(Id, PickupStationId, OrganizationId, releasedAt));
    }
}
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~StationClaimTests"`
Expected: PASS (4 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Lockers/StationClaim.cs backend/ePrevzem.Domain/Lockers/StationClaimId.cs backend/ePrevzem.Domain/Lockers/Events backend/ePrevzem.Tests/Domain/Lockers/StationClaimTests.cs
git commit -m "feat(domain): add StationClaim aggregate"
```

---

## Task 5: Identity — CitizenUser aggregate (with CitizenDevice child)

**Files:**
- Create: `backend/ePrevzem.Domain/Identity/CitizenUserId.cs`
- Create: `backend/ePrevzem.Domain/Identity/CitizenDeviceId.cs`
- Create: `backend/ePrevzem.Domain/Identity/CitizenDevice.cs`
- Create: `backend/ePrevzem.Domain/Identity/CitizenUser.cs`
- Create: `backend/ePrevzem.Domain/Identity/Events/CitizenOnboarded.cs`
- Create: `backend/ePrevzem.Domain/Identity/Events/CitizenDeviceRegistered.cs`
- Create: `backend/ePrevzem.Domain/Identity/Events/CitizenDeviceRevoked.cs`
- Test: `backend/ePrevzem.Tests/Domain/Identity/CitizenUserTests.cs`

- [ ] **Step 1: Write the failing CitizenUser tests**

`backend/ePrevzem.Tests/Domain/Identity/CitizenUserTests.cs`:
```csharp
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Identity;

public class CitizenUserTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly byte[] AnyPublicKey = new byte[] { 1, 2, 3, 4 };

    [Fact]
    public void Onboard_creates_citizen_and_raises_event()
    {
        var id = CitizenUserId.New();
        var citizen = CitizenUser.Onboard(
            id,
            firstName: "Janez",
            lastName: "Novak",
            emso: "0101000500001",
            email: "janez@example.com",
            phoneNumber: "+38640123456",
            now: Now);

        citizen.Id.Should().Be(id);
        citizen.FirstName.Should().Be("Janez");
        citizen.LastName.Should().Be("Novak");
        citizen.Emso.Should().Be("0101000500001");
        citizen.Email.Should().Be("janez@example.com");
        citizen.PhoneNumber.Should().Be("+38640123456");
        citizen.OnboardedAt.Should().Be(Now);
        citizen.Devices.Should().BeEmpty();
        citizen.DomainEvents.OfType<CitizenOnboarded>().Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Onboard_with_blank_first_name_throws(string firstName)
    {
        var act = () => CitizenUser.Onboard(CitizenUserId.New(), firstName, "n", "0101000500001", null, null, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("firstName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Onboard_with_blank_last_name_throws(string lastName)
    {
        var act = () => CitizenUser.Onboard(CitizenUserId.New(), "n", lastName, "0101000500001", null, null, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("lastName");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123")]
    [InlineData("01010005000010")]
    [InlineData("0101000500a01")]
    public void Onboard_with_invalid_emso_throws(string emso)
    {
        var act = () => CitizenUser.Onboard(CitizenUserId.New(), "n", "l", emso, null, null, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("emso");
    }

    [Fact]
    public void RegisterDevice_appends_active_device_and_raises_event()
    {
        var citizen = ValidCitizen();
        var deviceId = CitizenDeviceId.New();

        var device = citizen.RegisterDevice(deviceId, AnyPublicKey, "fp", "iPhone 14", Now.AddMinutes(1));

        device.Id.Should().Be(deviceId);
        device.CitizenUserId.Should().Be(citizen.Id);
        device.PublicKey.Should().BeEquivalentTo(AnyPublicKey);
        device.DeviceFingerprint.Should().Be("fp");
        device.Label.Should().Be("iPhone 14");
        device.RegisteredAt.Should().Be(Now.AddMinutes(1));
        device.RevokedAt.Should().BeNull();
        device.IsActive.Should().BeTrue();

        citizen.Devices.Should().ContainSingle().Which.Should().BeSameAs(device);
        citizen.DomainEvents.OfType<CitizenDeviceRegistered>().Should().ContainSingle();
    }

    [Fact]
    public void RegisterDevice_allows_multiple_active_devices()
    {
        var citizen = ValidCitizen();
        citizen.RegisterDevice(CitizenDeviceId.New(), AnyPublicKey, "fp1", null, Now);
        citizen.RegisterDevice(CitizenDeviceId.New(), AnyPublicKey, "fp2", null, Now);

        citizen.Devices.Should().HaveCount(2).And.OnlyContain(d => d.IsActive);
    }

    [Fact]
    public void RegisterDevice_with_empty_public_key_throws()
    {
        var citizen = ValidCitizen();
        var act = () => citizen.RegisterDevice(CitizenDeviceId.New(), Array.Empty<byte>(), "fp", null, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("publicKey");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterDevice_with_blank_fingerprint_throws(string fingerprint)
    {
        var citizen = ValidCitizen();
        var act = () => citizen.RegisterDevice(CitizenDeviceId.New(), AnyPublicKey, fingerprint, null, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("deviceFingerprint");
    }

    [Fact]
    public void RevokeDevice_sets_RevokedAt_and_raises_event()
    {
        var citizen = ValidCitizen();
        var deviceId = CitizenDeviceId.New();
        citizen.RegisterDevice(deviceId, AnyPublicKey, "fp", null, Now);

        citizen.RevokeDevice(deviceId, Now.AddDays(1));

        var device = citizen.Devices.Single();
        device.RevokedAt.Should().Be(Now.AddDays(1));
        device.IsActive.Should().BeFalse();
        citizen.DomainEvents.OfType<CitizenDeviceRevoked>().Should().ContainSingle();
    }

    [Fact]
    public void RevokeDevice_unknown_id_throws()
    {
        var citizen = ValidCitizen();
        var act = () => citizen.RevokeDevice(CitizenDeviceId.New(), Now);
        act.Should().Throw<InvalidOperationException>().WithMessage("*device not found*");
    }

    [Fact]
    public void RevokeDevice_already_revoked_throws()
    {
        var citizen = ValidCitizen();
        var id = CitizenDeviceId.New();
        citizen.RegisterDevice(id, AnyPublicKey, "fp", null, Now);
        citizen.RevokeDevice(id, Now.AddDays(1));

        var act = () => citizen.RevokeDevice(id, Now.AddDays(2));
        act.Should().Throw<InvalidOperationException>().WithMessage("*already revoked*");
    }

    private static CitizenUser ValidCitizen() =>
        CitizenUser.Onboard(CitizenUserId.New(), "Janez", "Novak", "0101000500001", null, null, Now);
}
```

- [ ] **Step 2: Run tests, confirm failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~CitizenUserTests"`
Expected: compile error.

- [ ] **Step 3: Implement Ids, CitizenDevice, CitizenUser, events**

`backend/ePrevzem.Domain/Identity/CitizenUserId.cs`:
```csharp
namespace ePrevzem.Domain.Identity;

public readonly record struct CitizenUserId(Guid Value)
{
    public static CitizenUserId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Identity/CitizenDeviceId.cs`:
```csharp
namespace ePrevzem.Domain.Identity;

public readonly record struct CitizenDeviceId(Guid Value)
{
    public static CitizenDeviceId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Identity/CitizenDevice.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity;

public sealed class CitizenDevice : Entity<CitizenDeviceId>
{
    public CitizenUserId CitizenUserId { get; private set; }
    public byte[] PublicKey { get; private set; } = default!;
    public string DeviceFingerprint { get; private set; } = default!;
    public string? Label { get; private set; }
    public DateTimeOffset RegisteredAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    private CitizenDevice() { }

    internal static CitizenDevice Register(
        CitizenDeviceId id,
        CitizenUserId citizenUserId,
        byte[] publicKey,
        string deviceFingerprint,
        string? label,
        DateTimeOffset registeredAt)
    {
        if (publicKey is null || publicKey.Length == 0)
            throw new ArgumentException("Public key is required.", nameof(publicKey));
        if (string.IsNullOrWhiteSpace(deviceFingerprint))
            throw new ArgumentException("Device fingerprint is required.", nameof(deviceFingerprint));

        return new CitizenDevice
        {
            Id = id,
            CitizenUserId = citizenUserId,
            PublicKey = publicKey,
            DeviceFingerprint = deviceFingerprint,
            Label = label,
            RegisteredAt = registeredAt
        };
    }

    internal void Revoke(DateTimeOffset revokedAt)
    {
        if (RevokedAt is not null)
            throw new InvalidOperationException("Device is already revoked.");
        RevokedAt = revokedAt;
    }
}
```

`backend/ePrevzem.Domain/Identity/Events/CitizenOnboarded.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record CitizenOnboarded(CitizenUserId CitizenUserId, DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Identity/Events/CitizenDeviceRegistered.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record CitizenDeviceRegistered(
    CitizenUserId CitizenUserId,
    CitizenDeviceId CitizenDeviceId,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Identity/Events/CitizenDeviceRevoked.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record CitizenDeviceRevoked(
    CitizenUserId CitizenUserId,
    CitizenDeviceId CitizenDeviceId,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Identity/CitizenUser.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity.Events;

namespace ePrevzem.Domain.Identity;

public sealed class CitizenUser : AggregateRoot<CitizenUserId>
{
    private readonly List<CitizenDevice> _devices = new();

    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Emso { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public DateTimeOffset OnboardedAt { get; private set; }
    public IReadOnlyCollection<CitizenDevice> Devices => _devices.AsReadOnly();

    private CitizenUser() { }

    public static CitizenUser Onboard(
        CitizenUserId id,
        string firstName,
        string lastName,
        string emso,
        string? email,
        string? phoneNumber,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (!IsValidEmso(emso))
            throw new ArgumentException("EMSO must be 13 digits.", nameof(emso));

        var user = new CitizenUser
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Emso = emso,
            Email = email,
            PhoneNumber = phoneNumber,
            OnboardedAt = now
        };
        user.Raise(new CitizenOnboarded(id, now));
        return user;
    }

    public CitizenDevice RegisterDevice(
        CitizenDeviceId id,
        byte[] publicKey,
        string deviceFingerprint,
        string? label,
        DateTimeOffset now)
    {
        var device = CitizenDevice.Register(id, Id, publicKey, deviceFingerprint, label, now);
        _devices.Add(device);
        Raise(new CitizenDeviceRegistered(Id, id, now));
        return device;
    }

    public void RevokeDevice(CitizenDeviceId deviceId, DateTimeOffset now)
    {
        var device = _devices.SingleOrDefault(d => d.Id == deviceId)
            ?? throw new InvalidOperationException("Citizen device not found on this user.");
        device.Revoke(now);
        Raise(new CitizenDeviceRevoked(Id, deviceId, now));
    }

    private static bool IsValidEmso(string? emso)
        => !string.IsNullOrWhiteSpace(emso) && emso.Length == 13 && emso.All(char.IsDigit);
}
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~CitizenUserTests"`
Expected: PASS (12 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Identity/CitizenUser.cs backend/ePrevzem.Domain/Identity/CitizenUserId.cs backend/ePrevzem.Domain/Identity/CitizenDevice.cs backend/ePrevzem.Domain/Identity/CitizenDeviceId.cs backend/ePrevzem.Domain/Identity/Events/Citizen* backend/ePrevzem.Tests/Domain/Identity/CitizenUserTests.cs
git commit -m "feat(domain): add CitizenUser aggregate with CitizenDevice child entity"
```

---

## Task 6: Identity — ProvisioningCode aggregate

**Files:**
- Create: `backend/ePrevzem.Domain/Identity/ProvisioningCodeId.cs`
- Create: `backend/ePrevzem.Domain/Identity/EmployeeAccountId.cs` *(also used by Task 7; introduced here)*
- Create: `backend/ePrevzem.Domain/Identity/EmployeeAccountRole.cs` (enum)
- Create: `backend/ePrevzem.Domain/Identity/ProvisioningCode.cs`
- Create: `backend/ePrevzem.Domain/Identity/Events/ProvisioningCodeIssued.cs`
- Create: `backend/ePrevzem.Domain/Identity/Events/ProvisioningCodeRedeemed.cs`
- Test: `backend/ePrevzem.Tests/Domain/Identity/ProvisioningCodeTests.cs`

- [ ] **Step 1: Write the failing ProvisioningCode tests**

`backend/ePrevzem.Tests/Domain/Identity/ProvisioningCodeTests.cs`:
```csharp
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Identity;

public class ProvisioningCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Issue_creates_unredeemed_code_with_event()
    {
        var id = ProvisioningCodeId.New();
        var orgId = OrganizationId.New();
        var stationIds = new[] { PickupStationId.New() };
        var roles = new[] { EmployeeAccountRole.Operator };
        var creator = EmployeeAccountId.New();

        var code = ProvisioningCode.Issue(
            id,
            orgId,
            code: "ABCD-1234",
            preFilledFirstName: "Ana",
            preFilledLastName: "Kovač",
            preFilledEmail: "ana@example.com",
            roles: roles,
            stationAccess: stationIds,
            createdBy: creator,
            now: Now,
            expiresAt: Now.AddHours(24),
            isReprovisioningOf: null);

        code.Id.Should().Be(id);
        code.OrganizationId.Should().Be(orgId);
        code.Code.Should().Be("ABCD-1234");
        code.PreFilledFirstName.Should().Be("Ana");
        code.PreFilledLastName.Should().Be("Kovač");
        code.PreFilledEmail.Should().Be("ana@example.com");
        code.Roles.Should().BeEquivalentTo(roles);
        code.StationAccess.Should().BeEquivalentTo(stationIds);
        code.CreatedByEmployeeAccountId.Should().Be(creator);
        code.CreatedAt.Should().Be(Now);
        code.ExpiresAt.Should().Be(Now.AddHours(24));
        code.RedeemedAt.Should().BeNull();
        code.IsReprovisioningOfEmployeeAccountId.Should().BeNull();
        code.IsRedeemable(at: Now).Should().BeTrue();
        code.DomainEvents.OfType<ProvisioningCodeIssued>().Should().ContainSingle();
    }

    [Fact]
    public void Issue_with_empty_roles_throws()
    {
        var act = () => ProvisioningCode.Issue(
            ProvisioningCodeId.New(), OrganizationId.New(), "C", "F", "L", null,
            Array.Empty<EmployeeAccountRole>(), Array.Empty<PickupStationId>(),
            EmployeeAccountId.New(), Now, Now.AddHours(1), null);
        act.Should().Throw<ArgumentException>().WithParameterName("roles");
    }

    [Fact]
    public void Issue_with_expiration_in_past_throws()
    {
        var act = () => ProvisioningCode.Issue(
            ProvisioningCodeId.New(), OrganizationId.New(), "C", "F", "L", null,
            new[] { EmployeeAccountRole.Operator }, Array.Empty<PickupStationId>(),
            EmployeeAccountId.New(), Now, Now.AddMinutes(-1), null);
        act.Should().Throw<ArgumentException>().WithParameterName("expiresAt");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Issue_with_blank_code_throws(string code)
    {
        var act = () => ProvisioningCode.Issue(
            ProvisioningCodeId.New(), OrganizationId.New(), code, "F", "L", null,
            new[] { EmployeeAccountRole.Operator }, Array.Empty<PickupStationId>(),
            EmployeeAccountId.New(), Now, Now.AddHours(1), null);
        act.Should().Throw<ArgumentException>().WithParameterName("code");
    }

    [Fact]
    public void Redeem_sets_redemption_state_and_raises_event()
    {
        var code = Issue();
        var newAccount = EmployeeAccountId.New();

        code.Redeem(redeemedAt: Now.AddMinutes(5), redeemedIntoEmployeeAccountId: newAccount);

        code.RedeemedAt.Should().Be(Now.AddMinutes(5));
        code.RedeemedIntoEmployeeAccountId.Should().Be(newAccount);
        code.IsRedeemable(at: Now.AddMinutes(5)).Should().BeFalse();
        code.DomainEvents.OfType<ProvisioningCodeRedeemed>().Should().ContainSingle();
    }

    [Fact]
    public void Redeem_twice_throws()
    {
        var code = Issue();
        code.Redeem(Now.AddMinutes(1), EmployeeAccountId.New());

        var act = () => code.Redeem(Now.AddMinutes(2), EmployeeAccountId.New());
        act.Should().Throw<InvalidOperationException>().WithMessage("*already redeemed*");
    }

    [Fact]
    public void Redeem_after_expiry_throws()
    {
        var code = Issue();
        var act = () => code.Redeem(Now.AddHours(2), EmployeeAccountId.New());
        act.Should().Throw<InvalidOperationException>().WithMessage("*expired*");
    }

    [Fact]
    public void IsRedeemable_is_false_after_expiry()
    {
        var code = Issue();
        code.IsRedeemable(Now.AddHours(2)).Should().BeFalse();
    }

    private static ProvisioningCode Issue(
        EmployeeAccountId? reprovisioningOf = null)
        => ProvisioningCode.Issue(
            ProvisioningCodeId.New(),
            OrganizationId.New(),
            code: "ABCD-1234",
            preFilledFirstName: "Ana",
            preFilledLastName: "Kovač",
            preFilledEmail: null,
            roles: new[] { EmployeeAccountRole.Operator },
            stationAccess: Array.Empty<PickupStationId>(),
            createdBy: EmployeeAccountId.New(),
            now: Now,
            expiresAt: Now.AddHours(1),
            isReprovisioningOf: reprovisioningOf);
}
```

- [ ] **Step 2: Run tests, confirm failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~ProvisioningCodeTests"`
Expected: compile error.

- [ ] **Step 3: Implement Ids, role enum, ProvisioningCode, events**

`backend/ePrevzem.Domain/Identity/ProvisioningCodeId.cs`:
```csharp
namespace ePrevzem.Domain.Identity;

public readonly record struct ProvisioningCodeId(Guid Value)
{
    public static ProvisioningCodeId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Identity/EmployeeAccountId.cs`:
```csharp
namespace ePrevzem.Domain.Identity;

public readonly record struct EmployeeAccountId(Guid Value)
{
    public static EmployeeAccountId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Identity/EmployeeAccountRole.cs`:
```csharp
namespace ePrevzem.Domain.Identity;

public enum EmployeeAccountRole
{
    OrganizationAdmin,
    RecordManager,
    Operator
}
```

`backend/ePrevzem.Domain/Identity/Events/ProvisioningCodeIssued.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record ProvisioningCodeIssued(ProvisioningCodeId ProvisioningCodeId, DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Identity/Events/ProvisioningCodeRedeemed.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record ProvisioningCodeRedeemed(
    ProvisioningCodeId ProvisioningCodeId,
    EmployeeAccountId RedeemedIntoEmployeeAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Identity/ProvisioningCode.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Identity;

public sealed class ProvisioningCode : AggregateRoot<ProvisioningCodeId>
{
    private readonly List<EmployeeAccountRole> _roles = new();
    private readonly List<PickupStationId> _stationAccess = new();

    public OrganizationId OrganizationId { get; private set; }
    public string Code { get; private set; } = default!;
    public string PreFilledFirstName { get; private set; } = default!;
    public string PreFilledLastName { get; private set; } = default!;
    public string? PreFilledEmail { get; private set; }
    public IReadOnlyCollection<EmployeeAccountRole> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<PickupStationId> StationAccess => _stationAccess.AsReadOnly();
    public EmployeeAccountId CreatedByEmployeeAccountId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RedeemedAt { get; private set; }
    public EmployeeAccountId? RedeemedIntoEmployeeAccountId { get; private set; }
    public EmployeeAccountId? IsReprovisioningOfEmployeeAccountId { get; private set; }

    private ProvisioningCode() { }

    public static ProvisioningCode Issue(
        ProvisioningCodeId id,
        OrganizationId organizationId,
        string code,
        string preFilledFirstName,
        string preFilledLastName,
        string? preFilledEmail,
        IReadOnlyCollection<EmployeeAccountRole> roles,
        IReadOnlyCollection<PickupStationId> stationAccess,
        EmployeeAccountId createdBy,
        DateTimeOffset now,
        DateTimeOffset expiresAt,
        EmployeeAccountId? isReprovisioningOf)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(preFilledFirstName))
            throw new ArgumentException("First name is required.", nameof(preFilledFirstName));
        if (string.IsNullOrWhiteSpace(preFilledLastName))
            throw new ArgumentException("Last name is required.", nameof(preFilledLastName));
        if (roles is null || roles.Count == 0)
            throw new ArgumentException("At least one role must be granted.", nameof(roles));
        if (expiresAt <= now)
            throw new ArgumentException("Expiration must be in the future.", nameof(expiresAt));

        var pc = new ProvisioningCode
        {
            Id = id,
            OrganizationId = organizationId,
            Code = code,
            PreFilledFirstName = preFilledFirstName,
            PreFilledLastName = preFilledLastName,
            PreFilledEmail = preFilledEmail,
            CreatedByEmployeeAccountId = createdBy,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            IsReprovisioningOfEmployeeAccountId = isReprovisioningOf
        };
        pc._roles.AddRange(roles.Distinct());
        pc._stationAccess.AddRange(stationAccess?.Distinct() ?? Array.Empty<PickupStationId>());
        pc.Raise(new ProvisioningCodeIssued(id, now));
        return pc;
    }

    public bool IsRedeemable(DateTimeOffset at) => RedeemedAt is null && at < ExpiresAt;

    public void Redeem(DateTimeOffset redeemedAt, EmployeeAccountId redeemedIntoEmployeeAccountId)
    {
        if (RedeemedAt is not null)
            throw new InvalidOperationException("Provisioning code has already been redeemed.");
        if (redeemedAt >= ExpiresAt)
            throw new InvalidOperationException("Provisioning code has expired.");

        RedeemedAt = redeemedAt;
        RedeemedIntoEmployeeAccountId = redeemedIntoEmployeeAccountId;
        Raise(new ProvisioningCodeRedeemed(Id, redeemedIntoEmployeeAccountId, redeemedAt));
    }
}
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~ProvisioningCodeTests"`
Expected: PASS (8 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Identity/ProvisioningCode.cs backend/ePrevzem.Domain/Identity/ProvisioningCodeId.cs backend/ePrevzem.Domain/Identity/EmployeeAccountId.cs backend/ePrevzem.Domain/Identity/EmployeeAccountRole.cs backend/ePrevzem.Domain/Identity/Events/Provisioning* backend/ePrevzem.Tests/Domain/Identity/ProvisioningCodeTests.cs
git commit -m "feat(domain): add ProvisioningCode aggregate"
```

---

## Task 7: Identity — EmployeeAccount aggregate (with EmployeeDevice, roles, station access)

**Files:**
- Create: `backend/ePrevzem.Domain/Identity/EmployeeAccountStatus.cs` (enum)
- Create: `backend/ePrevzem.Domain/Identity/EmployeeDeviceId.cs`
- Create: `backend/ePrevzem.Domain/Identity/EmployeeDevice.cs`
- Create: `backend/ePrevzem.Domain/Identity/EmployeeAccount.cs`
- Create: `backend/ePrevzem.Domain/Identity/Events/EmployeeAccountCreated.cs`
- Create: `backend/ePrevzem.Domain/Identity/Events/EmployeeAccountDisabled.cs`
- Create: `backend/ePrevzem.Domain/Identity/Events/EmployeeAccountReenabled.cs`
- Create: `backend/ePrevzem.Domain/Identity/Events/EmployeeDeviceRegistered.cs`
- Create: `backend/ePrevzem.Domain/Identity/Events/EmployeeDeviceRevoked.cs`
- Test: `backend/ePrevzem.Tests/Domain/Identity/EmployeeAccountTests.cs`

The aggregate enforces:
- Single active device (registering a new one auto-revokes the current active one).
- Role set is unique.
- `OrganizationAdmin` has implicit permission helpers (`CanManageRecords`, `CanOperateLockers`).
- Cannot register/revoke devices, change roles, or change station access while `Status == Disabled`.

- [ ] **Step 1: Write the failing EmployeeAccount tests**

`backend/ePrevzem.Tests/Domain/Identity/EmployeeAccountTests.cs`:
```csharp
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Identity;

public class EmployeeAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly byte[] AnyKey = new byte[] { 9, 9, 9 };

    [Fact]
    public void Create_constructs_account_with_roles_and_station_access()
    {
        var id = EmployeeAccountId.New();
        var orgId = OrganizationId.New();
        var stationId = PickupStationId.New();
        var codeId = ProvisioningCodeId.New();

        var acc = EmployeeAccount.Create(
            id,
            orgId,
            "Ana",
            "Kovač",
            "ana@example.com",
            new[] { EmployeeAccountRole.Operator, EmployeeAccountRole.RecordManager },
            new[] { stationId },
            codeId,
            Now);

        acc.Id.Should().Be(id);
        acc.OrganizationId.Should().Be(orgId);
        acc.FirstName.Should().Be("Ana");
        acc.LastName.Should().Be("Kovač");
        acc.Email.Should().Be("ana@example.com");
        acc.Status.Should().Be(EmployeeAccountStatus.Active);
        acc.Roles.Should().BeEquivalentTo(new[] { EmployeeAccountRole.Operator, EmployeeAccountRole.RecordManager });
        acc.StationAccess.Should().BeEquivalentTo(new[] { stationId });
        acc.CreatedFromProvisioningCodeId.Should().Be(codeId);
        acc.CreatedAt.Should().Be(Now);
        acc.Devices.Should().BeEmpty();
        acc.DomainEvents.OfType<EmployeeAccountCreated>().Should().ContainSingle();
    }

    [Fact]
    public void Create_with_empty_roles_throws()
    {
        var act = () => EmployeeAccount.Create(
            EmployeeAccountId.New(), OrganizationId.New(), "F", "L", null,
            Array.Empty<EmployeeAccountRole>(), Array.Empty<PickupStationId>(),
            ProvisioningCodeId.New(), Now);
        act.Should().Throw<ArgumentException>().WithParameterName("roles");
    }

    [Fact]
    public void OrganizationAdmin_implies_record_and_operator_permissions()
    {
        var acc = Account(new[] { EmployeeAccountRole.OrganizationAdmin });
        acc.CanManageRecords.Should().BeTrue();
        acc.CanOperateLockers.Should().BeTrue();
        acc.CanManageOrgAndEmployees.Should().BeTrue();
    }

    [Fact]
    public void RecordManager_only_grants_record_permissions()
    {
        var acc = Account(new[] { EmployeeAccountRole.RecordManager });
        acc.CanManageRecords.Should().BeTrue();
        acc.CanOperateLockers.Should().BeFalse();
        acc.CanManageOrgAndEmployees.Should().BeFalse();
    }

    [Fact]
    public void Operator_only_grants_operator_permissions()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        acc.CanManageRecords.Should().BeFalse();
        acc.CanOperateLockers.Should().BeTrue();
        acc.CanManageOrgAndEmployees.Should().BeFalse();
    }

    [Fact]
    public void GrantRole_adds_role_idempotently()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        acc.GrantRole(EmployeeAccountRole.RecordManager);
        acc.GrantRole(EmployeeAccountRole.RecordManager);
        acc.Roles.Should().BeEquivalentTo(new[] { EmployeeAccountRole.Operator, EmployeeAccountRole.RecordManager });
    }

    [Fact]
    public void RevokeRole_removes_role()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator, EmployeeAccountRole.RecordManager });
        acc.RevokeRole(EmployeeAccountRole.RecordManager);
        acc.Roles.Should().BeEquivalentTo(new[] { EmployeeAccountRole.Operator });
    }

    [Fact]
    public void RevokeRole_below_minimum_throws()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var act = () => acc.RevokeRole(EmployeeAccountRole.Operator);
        act.Should().Throw<InvalidOperationException>().WithMessage("*at least one role*");
    }

    [Fact]
    public void GrantStationAccess_adds_idempotently()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var s = PickupStationId.New();
        acc.GrantStationAccess(s);
        acc.GrantStationAccess(s);
        acc.StationAccess.Should().ContainSingle(x => x == s);
    }

    [Fact]
    public void RevokeStationAccess_removes_entry()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var s = PickupStationId.New();
        acc.GrantStationAccess(s);
        acc.RevokeStationAccess(s);
        acc.StationAccess.Should().BeEmpty();
    }

    [Fact]
    public void RegisterDevice_adds_active_device_when_none_exists()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var d = acc.RegisterDevice(EmployeeDeviceId.New(), AnyKey, "fp", null, Now);

        d.IsActive.Should().BeTrue();
        acc.ActiveDevice.Should().BeSameAs(d);
        acc.DomainEvents.OfType<EmployeeDeviceRegistered>().Should().ContainSingle();
    }

    [Fact]
    public void RegisterDevice_auto_revokes_previous_active_device()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var old = acc.RegisterDevice(EmployeeDeviceId.New(), AnyKey, "fp1", null, Now);
        var fresh = acc.RegisterDevice(EmployeeDeviceId.New(), AnyKey, "fp2", null, Now.AddDays(1));

        old.IsActive.Should().BeFalse();
        old.RevokedAt.Should().Be(Now.AddDays(1));
        fresh.IsActive.Should().BeTrue();
        acc.ActiveDevice.Should().BeSameAs(fresh);
        acc.DomainEvents.OfType<EmployeeDeviceRegistered>().Should().HaveCount(2);
        acc.DomainEvents.OfType<EmployeeDeviceRevoked>().Should().ContainSingle();
    }

    [Fact]
    public void RevokeDevice_marks_revoked_and_clears_active_device()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var d = acc.RegisterDevice(EmployeeDeviceId.New(), AnyKey, "fp", null, Now);
        acc.RevokeDevice(d.Id, Now.AddDays(1));

        d.IsActive.Should().BeFalse();
        acc.ActiveDevice.Should().BeNull();
    }

    [Fact]
    public void RevokeDevice_unknown_id_throws()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        var act = () => acc.RevokeDevice(EmployeeDeviceId.New(), Now);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Disable_then_Reenable_toggles_status_and_raises_events()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        acc.Disable(Now.AddDays(1));
        acc.Status.Should().Be(EmployeeAccountStatus.Disabled);
        acc.Reenable(Now.AddDays(2));
        acc.Status.Should().Be(EmployeeAccountStatus.Active);

        acc.DomainEvents.OfType<EmployeeAccountDisabled>().Should().ContainSingle();
        acc.DomainEvents.OfType<EmployeeAccountReenabled>().Should().ContainSingle();
    }

    [Fact]
    public void Disable_when_already_disabled_throws()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        acc.Disable(Now.AddDays(1));
        var act = () => acc.Disable(Now.AddDays(2));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Operations_on_disabled_account_throw()
    {
        var acc = Account(new[] { EmployeeAccountRole.Operator });
        acc.Disable(Now.AddDays(1));

        var grantRole = () => acc.GrantRole(EmployeeAccountRole.RecordManager);
        var revokeRole = () => acc.RevokeRole(EmployeeAccountRole.Operator);
        var grantStation = () => acc.GrantStationAccess(PickupStationId.New());
        var revokeStation = () => acc.RevokeStationAccess(PickupStationId.New());
        var registerDevice = () => acc.RegisterDevice(EmployeeDeviceId.New(), AnyKey, "fp", null, Now.AddDays(2));

        grantRole.Should().Throw<InvalidOperationException>();
        revokeRole.Should().Throw<InvalidOperationException>();
        grantStation.Should().Throw<InvalidOperationException>();
        revokeStation.Should().Throw<InvalidOperationException>();
        registerDevice.Should().Throw<InvalidOperationException>();
    }

    private static EmployeeAccount Account(IReadOnlyCollection<EmployeeAccountRole> roles)
        => EmployeeAccount.Create(
            EmployeeAccountId.New(), OrganizationId.New(), "Ana", "Kovač", null,
            roles, Array.Empty<PickupStationId>(),
            ProvisioningCodeId.New(), Now);
}
```

- [ ] **Step 2: Run tests, confirm failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~EmployeeAccountTests"`
Expected: compile error.

- [ ] **Step 3: Implement status enum, device, account, and events**

`backend/ePrevzem.Domain/Identity/EmployeeAccountStatus.cs`:
```csharp
namespace ePrevzem.Domain.Identity;

public enum EmployeeAccountStatus
{
    Active,
    Disabled
}
```

`backend/ePrevzem.Domain/Identity/EmployeeDeviceId.cs`:
```csharp
namespace ePrevzem.Domain.Identity;

public readonly record struct EmployeeDeviceId(Guid Value)
{
    public static EmployeeDeviceId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Identity/EmployeeDevice.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity;

public sealed class EmployeeDevice : Entity<EmployeeDeviceId>
{
    public EmployeeAccountId EmployeeAccountId { get; private set; }
    public byte[] PublicKey { get; private set; } = default!;
    public string DeviceFingerprint { get; private set; } = default!;
    public string? Label { get; private set; }
    public DateTimeOffset ProvisionedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    private EmployeeDevice() { }

    internal static EmployeeDevice Register(
        EmployeeDeviceId id,
        EmployeeAccountId accountId,
        byte[] publicKey,
        string deviceFingerprint,
        string? label,
        DateTimeOffset provisionedAt)
    {
        if (publicKey is null || publicKey.Length == 0)
            throw new ArgumentException("Public key is required.", nameof(publicKey));
        if (string.IsNullOrWhiteSpace(deviceFingerprint))
            throw new ArgumentException("Device fingerprint is required.", nameof(deviceFingerprint));

        return new EmployeeDevice
        {
            Id = id,
            EmployeeAccountId = accountId,
            PublicKey = publicKey,
            DeviceFingerprint = deviceFingerprint,
            Label = label,
            ProvisionedAt = provisionedAt
        };
    }

    internal void Revoke(DateTimeOffset revokedAt)
    {
        if (RevokedAt is not null)
            throw new InvalidOperationException("Device is already revoked.");
        RevokedAt = revokedAt;
    }
}
```

Events (all five files):

`backend/ePrevzem.Domain/Identity/Events/EmployeeAccountCreated.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeeAccountCreated(EmployeeAccountId EmployeeAccountId, DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Identity/Events/EmployeeAccountDisabled.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeeAccountDisabled(EmployeeAccountId EmployeeAccountId, DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Identity/Events/EmployeeAccountReenabled.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeeAccountReenabled(EmployeeAccountId EmployeeAccountId, DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Identity/Events/EmployeeDeviceRegistered.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeeDeviceRegistered(
    EmployeeAccountId EmployeeAccountId,
    EmployeeDeviceId EmployeeDeviceId,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Identity/Events/EmployeeDeviceRevoked.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity.Events;

public sealed record EmployeeDeviceRevoked(
    EmployeeAccountId EmployeeAccountId,
    EmployeeDeviceId EmployeeDeviceId,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Identity/EmployeeAccount.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity.Events;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Identity;

public sealed class EmployeeAccount : AggregateRoot<EmployeeAccountId>
{
    private readonly List<EmployeeAccountRole> _roles = new();
    private readonly List<PickupStationId> _stationAccess = new();
    private readonly List<EmployeeDevice> _devices = new();

    public OrganizationId OrganizationId { get; private set; }
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? Email { get; private set; }
    public EmployeeAccountStatus Status { get; private set; }
    public ProvisioningCodeId CreatedFromProvisioningCodeId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<EmployeeAccountRole> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<PickupStationId> StationAccess => _stationAccess.AsReadOnly();
    public IReadOnlyCollection<EmployeeDevice> Devices => _devices.AsReadOnly();
    public EmployeeDevice? ActiveDevice => _devices.SingleOrDefault(d => d.IsActive);

    public bool CanManageOrgAndEmployees => _roles.Contains(EmployeeAccountRole.OrganizationAdmin);
    public bool CanManageRecords =>
        _roles.Contains(EmployeeAccountRole.OrganizationAdmin) || _roles.Contains(EmployeeAccountRole.RecordManager);
    public bool CanOperateLockers =>
        _roles.Contains(EmployeeAccountRole.OrganizationAdmin) || _roles.Contains(EmployeeAccountRole.Operator);

    private EmployeeAccount() { }

    public static EmployeeAccount Create(
        EmployeeAccountId id,
        OrganizationId organizationId,
        string firstName,
        string lastName,
        string? email,
        IReadOnlyCollection<EmployeeAccountRole> roles,
        IReadOnlyCollection<PickupStationId> stationAccess,
        ProvisioningCodeId createdFromProvisioningCodeId,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (roles is null || roles.Count == 0)
            throw new ArgumentException("At least one role must be granted.", nameof(roles));

        var acc = new EmployeeAccount
        {
            Id = id,
            OrganizationId = organizationId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Status = EmployeeAccountStatus.Active,
            CreatedFromProvisioningCodeId = createdFromProvisioningCodeId,
            CreatedAt = now
        };
        acc._roles.AddRange(roles.Distinct());
        acc._stationAccess.AddRange((stationAccess ?? Array.Empty<PickupStationId>()).Distinct());
        acc.Raise(new EmployeeAccountCreated(id, now));
        return acc;
    }

    public void GrantRole(EmployeeAccountRole role)
    {
        EnsureActive();
        if (_roles.Contains(role)) return;
        _roles.Add(role);
    }

    public void RevokeRole(EmployeeAccountRole role)
    {
        EnsureActive();
        if (!_roles.Contains(role)) return;
        if (_roles.Count == 1)
            throw new InvalidOperationException("Cannot revoke the last role; an account must have at least one role.");
        _roles.Remove(role);
    }

    public void GrantStationAccess(PickupStationId stationId)
    {
        EnsureActive();
        if (_stationAccess.Contains(stationId)) return;
        _stationAccess.Add(stationId);
    }

    public void RevokeStationAccess(PickupStationId stationId)
    {
        EnsureActive();
        _stationAccess.Remove(stationId);
    }

    public EmployeeDevice RegisterDevice(
        EmployeeDeviceId id,
        byte[] publicKey,
        string deviceFingerprint,
        string? label,
        DateTimeOffset now)
    {
        EnsureActive();

        var existing = ActiveDevice;
        if (existing is not null)
        {
            existing.Revoke(now);
            Raise(new EmployeeDeviceRevoked(Id, existing.Id, now));
        }

        var device = EmployeeDevice.Register(id, Id, publicKey, deviceFingerprint, label, now);
        _devices.Add(device);
        Raise(new EmployeeDeviceRegistered(Id, id, now));
        return device;
    }

    public void RevokeDevice(EmployeeDeviceId deviceId, DateTimeOffset now)
    {
        var device = _devices.SingleOrDefault(d => d.Id == deviceId)
            ?? throw new InvalidOperationException("Employee device not found on this account.");
        device.Revoke(now);
        Raise(new EmployeeDeviceRevoked(Id, deviceId, now));
    }

    public void Disable(DateTimeOffset now)
    {
        if (Status == EmployeeAccountStatus.Disabled)
            throw new InvalidOperationException("Account is already disabled.");
        Status = EmployeeAccountStatus.Disabled;
        Raise(new EmployeeAccountDisabled(Id, now));
    }

    public void Reenable(DateTimeOffset now)
    {
        if (Status == EmployeeAccountStatus.Active)
            throw new InvalidOperationException("Account is already active.");
        Status = EmployeeAccountStatus.Active;
        Raise(new EmployeeAccountReenabled(Id, now));
    }

    private void EnsureActive()
    {
        if (Status == EmployeeAccountStatus.Disabled)
            throw new InvalidOperationException("Cannot modify a disabled employee account.");
    }
}
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~EmployeeAccountTests"`
Expected: PASS (17 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Identity backend/ePrevzem.Tests/Domain/Identity/EmployeeAccountTests.cs
git commit -m "feat(domain): add EmployeeAccount aggregate with device, role, station-access management"
```

---

## Task 8: Identity — SystemAdmin aggregate

**Files:**
- Create: `backend/ePrevzem.Domain/Identity/SystemAdminId.cs`
- Create: `backend/ePrevzem.Domain/Identity/SystemAdmin.cs`
- Test: `backend/ePrevzem.Tests/Domain/Identity/SystemAdminTests.cs`

Minimal aggregate; password/credential mechanics are out of scope.

- [ ] **Step 1: Write the failing SystemAdmin tests**

`backend/ePrevzem.Tests/Domain/Identity/SystemAdminTests.cs`:
```csharp
using ePrevzem.Domain.Identity;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Identity;

public class SystemAdminTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_constructs_system_admin()
    {
        var id = SystemAdminId.New();
        var admin = SystemAdmin.Create(id, "ops-jane", Now);

        admin.Id.Should().Be(id);
        admin.Username.Should().Be("ops-jane");
        admin.CreatedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_username_throws(string username)
    {
        var act = () => SystemAdmin.Create(SystemAdminId.New(), username, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("username");
    }
}
```

- [ ] **Step 2: Run tests, confirm failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~SystemAdminTests"`
Expected: compile error.

- [ ] **Step 3: Implement SystemAdminId and SystemAdmin**

`backend/ePrevzem.Domain/Identity/SystemAdminId.cs`:
```csharp
namespace ePrevzem.Domain.Identity;

public readonly record struct SystemAdminId(Guid Value)
{
    public static SystemAdminId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Identity/SystemAdmin.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Identity;

public sealed class SystemAdmin : AggregateRoot<SystemAdminId>
{
    public string Username { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private SystemAdmin() { }

    public static SystemAdmin Create(SystemAdminId id, string username, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        return new SystemAdmin { Id = id, Username = username, CreatedAt = now };
    }
}
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~SystemAdminTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Identity/SystemAdmin.cs backend/ePrevzem.Domain/Identity/SystemAdminId.cs backend/ePrevzem.Tests/Domain/Identity/SystemAdminTests.cs
git commit -m "feat(domain): add SystemAdmin aggregate"
```

---

## Task 9: Pickups — Package aggregate with Placement child entity and state machine

This is the biggest task. It is split into TDD sub-cycles, one per state transition area. Each sub-cycle ends with its own commit so reviewers can step through the state machine.

**Files (created across sub-cycles, all listed once for clarity):**
- Create: `backend/ePrevzem.Domain/Pickups/PackageId.cs`
- Create: `backend/ePrevzem.Domain/Pickups/PackageStatus.cs`
- Create: `backend/ePrevzem.Domain/Pickups/PlacementId.cs`
- Create: `backend/ePrevzem.Domain/Pickups/PlacementEndReason.cs`
- Create: `backend/ePrevzem.Domain/Pickups/Placement.cs`
- Create: `backend/ePrevzem.Domain/Pickups/Package.cs`
- Create: `backend/ePrevzem.Domain/Pickups/Events/PackageCreated.cs`
- Create: `backend/ePrevzem.Domain/Pickups/Events/PackagePlaced.cs`
- Create: `backend/ePrevzem.Domain/Pickups/Events/PackagePickedUpByCitizen.cs`
- Create: `backend/ePrevzem.Domain/Pickups/Events/PackageRemovedByEmployee.cs`
- Create: `backend/ePrevzem.Domain/Pickups/Events/PackageExpired.cs`
- Create: `backend/ePrevzem.Domain/Pickups/Events/PackageRetrievedAfterExpiry.cs`
- Create: `backend/ePrevzem.Domain/Pickups/Events/PackageMarkedPickedUpManually.cs`
- Create: `backend/ePrevzem.Domain/Pickups/Events/PackageCancelled.cs`
- Test: `backend/ePrevzem.Tests/Domain/Pickups/PackageCreationTests.cs`
- Test: `backend/ePrevzem.Tests/Domain/Pickups/PackagePlacementTests.cs`
- Test: `backend/ePrevzem.Tests/Domain/Pickups/PackagePickupTests.cs`
- Test: `backend/ePrevzem.Tests/Domain/Pickups/PackageRemovalTests.cs`
- Test: `backend/ePrevzem.Tests/Domain/Pickups/PackageExpiryTests.cs`
- Test: `backend/ePrevzem.Tests/Domain/Pickups/PackageCancellationTests.cs`

### 9A — Sub-cycle: Package creation (AwaitingPlacement)

- [ ] **Step 1: Write the failing creation tests**

`backend/ePrevzem.Tests/Domain/Pickups/PackageCreationTests.cs`:
```csharp
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Domain.Pickups.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Pickups;

public class PackageCreationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_initialises_package_in_AwaitingPlacement_with_no_deadline()
    {
        var id = PackageId.New();
        var orgId = OrganizationId.New();
        var recipient = CitizenUserId.New();
        var createdBy = EmployeeAccountId.New();
        var station = PickupStationId.New();

        var pkg = Package.Create(id, orgId, recipient, createdBy, station, "Vabilo na sodišče", Now);

        pkg.Id.Should().Be(id);
        pkg.OrganizationId.Should().Be(orgId);
        pkg.RecipientCitizenUserId.Should().Be(recipient);
        pkg.CreatedByEmployeeAccountId.Should().Be(createdBy);
        pkg.TargetPickupStationId.Should().Be(station);
        pkg.Description.Should().Be("Vabilo na sodišče");
        pkg.Status.Should().Be(PackageStatus.AwaitingPlacement);
        pkg.DeadlineAt.Should().BeNull();
        pkg.CreatedAt.Should().Be(Now);
        pkg.FinalizedAt.Should().BeNull();
        pkg.Placements.Should().BeEmpty();
        pkg.ActivePlacement.Should().BeNull();
        pkg.DomainEvents.OfType<PackageCreated>().Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_description_throws(string description)
    {
        var act = () => Package.Create(
            PackageId.New(), OrganizationId.New(), CitizenUserId.New(), EmployeeAccountId.New(),
            PickupStationId.New(), description, Now);
        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }
}
```

- [ ] **Step 2: Run tests, expect compile failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~PackageCreationTests"`
Expected: compile error.

- [ ] **Step 3: Implement Ids, enums, Placement, Package skeleton, PackageCreated event**

`backend/ePrevzem.Domain/Pickups/PackageId.cs`:
```csharp
namespace ePrevzem.Domain.Pickups;

public readonly record struct PackageId(Guid Value)
{
    public static PackageId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Pickups/PlacementId.cs`:
```csharp
namespace ePrevzem.Domain.Pickups;

public readonly record struct PlacementId(Guid Value)
{
    public static PlacementId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Pickups/PackageStatus.cs`:
```csharp
namespace ePrevzem.Domain.Pickups;

public enum PackageStatus
{
    AwaitingPlacement,
    InLocker,
    PickedUp,
    NotPickedUp,
    AwaitingPersonalPickup,
    Cancelled
}
```

`backend/ePrevzem.Domain/Pickups/PlacementEndReason.cs`:
```csharp
namespace ePrevzem.Domain.Pickups;

public enum PlacementEndReason
{
    PickedUpByCitizen,
    RemovedByEmployee,
    RetrievedAfterExpiry
}
```

`backend/ePrevzem.Domain/Pickups/Placement.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;

namespace ePrevzem.Domain.Pickups;

public sealed class Placement : Entity<PlacementId>
{
    public PackageId PackageId { get; private set; }
    public LockerId LockerId { get; private set; }
    public EmployeeAccountId OpenedByEmployeeAccountId { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }
    public PlacementEndReason? EndReason { get; private set; }
    public CitizenUserId? EndedByCitizenUserId { get; private set; }
    public EmployeeAccountId? EndedByEmployeeAccountId { get; private set; }

    public bool IsOpen => EndedAt is null;

    private Placement() { }

    internal static Placement Open(
        PlacementId id,
        PackageId packageId,
        LockerId lockerId,
        EmployeeAccountId openedBy,
        DateTimeOffset openedAt)
    {
        return new Placement
        {
            Id = id,
            PackageId = packageId,
            LockerId = lockerId,
            OpenedByEmployeeAccountId = openedBy,
            OpenedAt = openedAt
        };
    }

    internal void CloseByCitizen(CitizenUserId endedBy, DateTimeOffset endedAt)
    {
        EnsureOpen(endedAt);
        EndedAt = endedAt;
        EndReason = PlacementEndReason.PickedUpByCitizen;
        EndedByCitizenUserId = endedBy;
    }

    internal void CloseByEmployeeRemoval(EmployeeAccountId endedBy, DateTimeOffset endedAt)
    {
        EnsureOpen(endedAt);
        EndedAt = endedAt;
        EndReason = PlacementEndReason.RemovedByEmployee;
        EndedByEmployeeAccountId = endedBy;
    }

    internal void CloseByExpiryRetrieval(EmployeeAccountId endedBy, DateTimeOffset endedAt)
    {
        EnsureOpen(endedAt);
        EndedAt = endedAt;
        EndReason = PlacementEndReason.RetrievedAfterExpiry;
        EndedByEmployeeAccountId = endedBy;
    }

    private void EnsureOpen(DateTimeOffset endedAt)
    {
        if (!IsOpen)
            throw new InvalidOperationException("Placement is already closed.");
        if (endedAt < OpenedAt)
            throw new ArgumentException("Ended-at must be on or after opened-at.", nameof(endedAt));
    }
}
```

`backend/ePrevzem.Domain/Pickups/Events/PackageCreated.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackageCreated(PackageId PackageId, DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Pickups/Package.cs` (initial skeleton — subsequent sub-cycles add methods to this same file):
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups.Events;

namespace ePrevzem.Domain.Pickups;

public sealed class Package : AggregateRoot<PackageId>
{
    private readonly List<Placement> _placements = new();

    public OrganizationId OrganizationId { get; private set; }
    public CitizenUserId RecipientCitizenUserId { get; private set; }
    public EmployeeAccountId CreatedByEmployeeAccountId { get; private set; }
    public PickupStationId TargetPickupStationId { get; private set; }
    public string Description { get; private set; } = default!;
    public PackageStatus Status { get; private set; }
    public DateTimeOffset? DeadlineAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }

    public IReadOnlyCollection<Placement> Placements => _placements.AsReadOnly();
    public Placement? ActivePlacement => _placements.SingleOrDefault(p => p.IsOpen);

    private Package() { }

    public static Package Create(
        PackageId id,
        OrganizationId organizationId,
        CitizenUserId recipientCitizenUserId,
        EmployeeAccountId createdBy,
        PickupStationId targetPickupStationId,
        string description,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        var pkg = new Package
        {
            Id = id,
            OrganizationId = organizationId,
            RecipientCitizenUserId = recipientCitizenUserId,
            CreatedByEmployeeAccountId = createdBy,
            TargetPickupStationId = targetPickupStationId,
            Description = description,
            Status = PackageStatus.AwaitingPlacement,
            CreatedAt = now
        };
        pkg.Raise(new PackageCreated(id, now));
        return pkg;
    }
}
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~PackageCreationTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Pickups backend/ePrevzem.Tests/Domain/Pickups/PackageCreationTests.cs
git commit -m "feat(domain): add Package aggregate (creation only)"
```

### 9B — Sub-cycle: Place (AwaitingPlacement → InLocker)

- [ ] **Step 1: Write the failing placement tests**

`backend/ePrevzem.Tests/Domain/Pickups/PackagePlacementTests.cs`:
```csharp
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Domain.Pickups.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Pickups;

public class PackagePlacementTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan FiveDays = TimeSpan.FromDays(5);

    [Fact]
    public void Place_opens_placement_sets_status_InLocker_and_computes_deadline()
    {
        var pkg = NewPackage();
        var placementId = PlacementId.New();
        var lockerId = LockerId.New();
        var employee = EmployeeAccountId.New();

        var placement = pkg.Place(placementId, lockerId, employee, FiveDays, Now.AddMinutes(1));

        pkg.Status.Should().Be(PackageStatus.InLocker);
        pkg.DeadlineAt.Should().Be(Now.AddMinutes(1) + FiveDays);
        placement.LockerId.Should().Be(lockerId);
        placement.OpenedByEmployeeAccountId.Should().Be(employee);
        placement.OpenedAt.Should().Be(Now.AddMinutes(1));
        placement.IsOpen.Should().BeTrue();
        pkg.ActivePlacement.Should().BeSameAs(placement);
        pkg.DomainEvents.OfType<PackagePlaced>().Should().ContainSingle();
    }

    [Fact]
    public void Place_when_not_AwaitingPlacement_throws()
    {
        var pkg = NewPackage();
        pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), FiveDays, Now);

        var act = () => pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), FiveDays, Now.AddMinutes(1));
        act.Should().Throw<InvalidOperationException>().WithMessage("*AwaitingPlacement*");
    }

    [Fact]
    public void Place_with_non_positive_duration_throws()
    {
        var pkg = NewPackage();
        var act = () => pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), TimeSpan.Zero, Now);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("pickupDuration");
    }

    private static Package NewPackage() => Package.Create(
        PackageId.New(), OrganizationId.New(), CitizenUserId.New(), EmployeeAccountId.New(),
        PickupStationId.New(), "desc", Now);
}
```

- [ ] **Step 2: Run tests, expect compile failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~PackagePlacementTests"`
Expected: compile error.

- [ ] **Step 3: Add `Place` method and `PackagePlaced` event**

Add `backend/ePrevzem.Domain/Pickups/Events/PackagePlaced.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackagePlaced(
    PackageId PackageId,
    PlacementId PlacementId,
    LockerId LockerId,
    EmployeeAccountId OpenedByEmployeeAccountId,
    DateTimeOffset DeadlineAt,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

Append to `backend/ePrevzem.Domain/Pickups/Package.cs` (inside the `Package` class, after `Create`):
```csharp
    public Placement Place(
        PlacementId placementId,
        LockerId lockerId,
        EmployeeAccountId openedBy,
        TimeSpan pickupDuration,
        DateTimeOffset now)
    {
        if (Status != PackageStatus.AwaitingPlacement)
            throw new InvalidOperationException($"Package can only be placed while in AwaitingPlacement (current: {Status}).");
        if (pickupDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(pickupDuration), pickupDuration, "Pickup duration must be positive.");

        var placement = Placement.Open(placementId, Id, lockerId, openedBy, now);
        _placements.Add(placement);
        Status = PackageStatus.InLocker;
        DeadlineAt = now + pickupDuration;

        Raise(new PackagePlaced(Id, placementId, lockerId, openedBy, DeadlineAt.Value, now));
        return placement;
    }
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~PackagePlacementTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Pickups/Package.cs backend/ePrevzem.Domain/Pickups/Events/PackagePlaced.cs backend/ePrevzem.Tests/Domain/Pickups/PackagePlacementTests.cs
git commit -m "feat(domain): Package.Place transitions to InLocker and computes deadline"
```

### 9C — Sub-cycle: Pickup by citizen (InLocker → PickedUp)

- [ ] **Step 1: Write the failing pickup tests**

`backend/ePrevzem.Tests/Domain/Pickups/PackagePickupTests.cs`:
```csharp
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Domain.Pickups.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Pickups;

public class PackagePickupTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PickUpByCitizen_closes_placement_finalises_package_and_raises_event()
    {
        var pkg = PlacedPackage(out var recipient);
        var pickedUpAt = Now.AddDays(1);

        pkg.PickUpByCitizen(recipient, pickedUpAt);

        pkg.Status.Should().Be(PackageStatus.PickedUp);
        pkg.FinalizedAt.Should().Be(pickedUpAt);
        pkg.ActivePlacement.Should().BeNull();
        pkg.Placements.Should().ContainSingle()
            .Which.EndReason.Should().Be(PlacementEndReason.PickedUpByCitizen);
        pkg.DomainEvents.OfType<PackagePickedUpByCitizen>().Should().ContainSingle();
    }

    [Fact]
    public void PickUpByCitizen_when_not_InLocker_throws()
    {
        var pkg = NewPackage(out _);  // AwaitingPlacement
        var act = () => pkg.PickUpByCitizen(CitizenUserId.New(), Now);
        act.Should().Throw<InvalidOperationException>().WithMessage("*InLocker*");
    }

    private static Package NewPackage(out CitizenUserId recipient)
    {
        recipient = CitizenUserId.New();
        return Package.Create(
            PackageId.New(), OrganizationId.New(), recipient, EmployeeAccountId.New(),
            PickupStationId.New(), "desc", Now);
    }

    private static Package PlacedPackage(out CitizenUserId recipient)
    {
        var pkg = NewPackage(out recipient);
        pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), TimeSpan.FromDays(5), Now);
        pkg.ClearDomainEvents();
        return pkg;
    }
}
```

- [ ] **Step 2: Run tests, expect compile failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~PackagePickupTests"`
Expected: compile error.

- [ ] **Step 3: Add `PickUpByCitizen` method and event**

Add `backend/ePrevzem.Domain/Pickups/Events/PackagePickedUpByCitizen.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackagePickedUpByCitizen(
    PackageId PackageId,
    PlacementId PlacementId,
    CitizenUserId PickedUpByCitizenUserId,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

Append to `Package.cs`:
```csharp
    public void PickUpByCitizen(CitizenUserId pickedUpBy, DateTimeOffset now)
    {
        if (Status != PackageStatus.InLocker)
            throw new InvalidOperationException($"Pickup is only allowed from InLocker (current: {Status}).");

        var placement = ActivePlacement
            ?? throw new InvalidOperationException("No active placement to close.");
        placement.CloseByCitizen(pickedUpBy, now);

        Status = PackageStatus.PickedUp;
        FinalizedAt = now;
        Raise(new PackagePickedUpByCitizen(Id, placement.Id, pickedUpBy, now));
    }
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~PackagePickupTests"`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Pickups/Package.cs backend/ePrevzem.Domain/Pickups/Events/PackagePickedUpByCitizen.cs backend/ePrevzem.Tests/Domain/Pickups/PackagePickupTests.cs
git commit -m "feat(domain): Package.PickUpByCitizen finalises pickup via citizen"
```

### 9D — Sub-cycle: Employee removes from locker (InLocker → AwaitingPlacement)

- [ ] **Step 1: Write the failing removal tests**

`backend/ePrevzem.Tests/Domain/Pickups/PackageRemovalTests.cs`:
```csharp
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Domain.Pickups.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Pickups;

public class PackageRemovalTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RemoveByEmployee_closes_placement_clears_deadline_and_returns_to_AwaitingPlacement()
    {
        var pkg = PlacedPackage();
        var employee = EmployeeAccountId.New();
        var removedAt = Now.AddHours(1);

        pkg.RemoveByEmployee(employee, removedAt);

        pkg.Status.Should().Be(PackageStatus.AwaitingPlacement);
        pkg.DeadlineAt.Should().BeNull();
        pkg.ActivePlacement.Should().BeNull();
        pkg.Placements.Should().ContainSingle()
            .Which.EndReason.Should().Be(PlacementEndReason.RemovedByEmployee);
        pkg.DomainEvents.OfType<PackageRemovedByEmployee>().Should().ContainSingle();
    }

    [Fact]
    public void RemoveByEmployee_then_Place_again_starts_new_placement_with_fresh_deadline()
    {
        var pkg = PlacedPackage();
        pkg.RemoveByEmployee(EmployeeAccountId.New(), Now.AddHours(1));
        pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), TimeSpan.FromDays(5), Now.AddHours(2));

        pkg.Status.Should().Be(PackageStatus.InLocker);
        pkg.Placements.Should().HaveCount(2);
        pkg.ActivePlacement.Should().NotBeNull();
        pkg.DeadlineAt.Should().Be(Now.AddHours(2) + TimeSpan.FromDays(5));
    }

    [Fact]
    public void RemoveByEmployee_when_not_InLocker_throws()
    {
        var pkg = NewPackage();  // AwaitingPlacement
        var act = () => pkg.RemoveByEmployee(EmployeeAccountId.New(), Now);
        act.Should().Throw<InvalidOperationException>().WithMessage("*InLocker*");
    }

    private static Package NewPackage() => Package.Create(
        PackageId.New(), OrganizationId.New(), CitizenUserId.New(), EmployeeAccountId.New(),
        PickupStationId.New(), "desc", Now);

    private static Package PlacedPackage()
    {
        var pkg = NewPackage();
        pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), TimeSpan.FromDays(5), Now);
        pkg.ClearDomainEvents();
        return pkg;
    }
}
```

- [ ] **Step 2: Run tests, expect compile failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~PackageRemovalTests"`
Expected: compile error.

- [ ] **Step 3: Add `RemoveByEmployee` method and event**

Add `backend/ePrevzem.Domain/Pickups/Events/PackageRemovedByEmployee.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackageRemovedByEmployee(
    PackageId PackageId,
    PlacementId PlacementId,
    EmployeeAccountId RemovedByEmployeeAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

Append to `Package.cs`:
```csharp
    public void RemoveByEmployee(EmployeeAccountId removedBy, DateTimeOffset now)
    {
        if (Status != PackageStatus.InLocker)
            throw new InvalidOperationException($"Removal is only allowed from InLocker (current: {Status}).");

        var placement = ActivePlacement
            ?? throw new InvalidOperationException("No active placement to close.");
        placement.CloseByEmployeeRemoval(removedBy, now);

        Status = PackageStatus.AwaitingPlacement;
        DeadlineAt = null;
        Raise(new PackageRemovedByEmployee(Id, placement.Id, removedBy, now));
    }
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~PackageRemovalTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Pickups/Package.cs backend/ePrevzem.Domain/Pickups/Events/PackageRemovedByEmployee.cs backend/ePrevzem.Tests/Domain/Pickups/PackageRemovalTests.cs
git commit -m "feat(domain): Package.RemoveByEmployee clears deadline and returns to AwaitingPlacement"
```

### 9E — Sub-cycle: Expiry and retrieval (InLocker → NotPickedUp → AwaitingPersonalPickup → PickedUp)

- [ ] **Step 1: Write the failing expiry tests**

`backend/ePrevzem.Tests/Domain/Pickups/PackageExpiryTests.cs`:
```csharp
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Domain.Pickups.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Pickups;

public class PackageExpiryTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MarkExpired_sets_NotPickedUp_when_deadline_passed_and_still_in_locker()
    {
        var pkg = PlacedPackage(deadlineIn: TimeSpan.FromHours(1));
        var observedAt = Now.AddHours(2);

        pkg.MarkExpired(observedAt);

        pkg.Status.Should().Be(PackageStatus.NotPickedUp);
        pkg.ActivePlacement.Should().NotBeNull();
        pkg.DomainEvents.OfType<PackageExpired>().Should().ContainSingle();
    }

    [Fact]
    public void MarkExpired_before_deadline_throws()
    {
        var pkg = PlacedPackage(deadlineIn: TimeSpan.FromDays(5));
        var act = () => pkg.MarkExpired(Now.AddMinutes(1));
        act.Should().Throw<InvalidOperationException>().WithMessage("*deadline*");
    }

    [Fact]
    public void MarkExpired_when_not_InLocker_throws()
    {
        var pkg = NewPackage();  // AwaitingPlacement
        var act = () => pkg.MarkExpired(Now.AddDays(99));
        act.Should().Throw<InvalidOperationException>().WithMessage("*InLocker*");
    }

    [Fact]
    public void RetrieveAfterExpiry_closes_placement_transitions_to_AwaitingPersonalPickup()
    {
        var pkg = ExpiredPackage();
        var employee = EmployeeAccountId.New();
        var retrievedAt = Now.AddDays(10);

        pkg.RetrieveAfterExpiry(employee, retrievedAt);

        pkg.Status.Should().Be(PackageStatus.AwaitingPersonalPickup);
        pkg.ActivePlacement.Should().BeNull();
        pkg.Placements.Should().ContainSingle()
            .Which.EndReason.Should().Be(PlacementEndReason.RetrievedAfterExpiry);
        pkg.DomainEvents.OfType<PackageRetrievedAfterExpiry>().Should().ContainSingle();
    }

    [Fact]
    public void RetrieveAfterExpiry_when_not_NotPickedUp_throws()
    {
        var pkg = PlacedPackage(deadlineIn: TimeSpan.FromDays(5));
        var act = () => pkg.RetrieveAfterExpiry(EmployeeAccountId.New(), Now.AddDays(1));
        act.Should().Throw<InvalidOperationException>().WithMessage("*NotPickedUp*");
    }

    [Fact]
    public void MarkPickedUpManually_after_personal_pickup_finalises_package()
    {
        var pkg = ExpiredPackage();
        pkg.RetrieveAfterExpiry(EmployeeAccountId.New(), Now.AddDays(10));
        var employee = EmployeeAccountId.New();

        pkg.MarkPickedUpManually(employee, Now.AddDays(11));

        pkg.Status.Should().Be(PackageStatus.PickedUp);
        pkg.FinalizedAt.Should().Be(Now.AddDays(11));
        pkg.DomainEvents.OfType<PackageMarkedPickedUpManually>().Should().ContainSingle();
    }

    [Fact]
    public void MarkPickedUpManually_when_not_AwaitingPersonalPickup_throws()
    {
        var pkg = PlacedPackage(deadlineIn: TimeSpan.FromDays(5));
        var act = () => pkg.MarkPickedUpManually(EmployeeAccountId.New(), Now.AddDays(1));
        act.Should().Throw<InvalidOperationException>().WithMessage("*AwaitingPersonalPickup*");
    }

    private static Package NewPackage() => Package.Create(
        PackageId.New(), OrganizationId.New(), CitizenUserId.New(), EmployeeAccountId.New(),
        PickupStationId.New(), "desc", Now);

    private static Package PlacedPackage(TimeSpan deadlineIn)
    {
        var pkg = NewPackage();
        pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), deadlineIn, Now);
        pkg.ClearDomainEvents();
        return pkg;
    }

    private static Package ExpiredPackage()
    {
        var pkg = PlacedPackage(TimeSpan.FromHours(1));
        pkg.MarkExpired(Now.AddHours(2));
        pkg.ClearDomainEvents();
        return pkg;
    }
}
```

- [ ] **Step 2: Run tests, expect compile failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~PackageExpiryTests"`
Expected: compile error.

- [ ] **Step 3: Add `MarkExpired`, `RetrieveAfterExpiry`, `MarkPickedUpManually` and three events**

Add `backend/ePrevzem.Domain/Pickups/Events/PackageExpired.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackageExpired(PackageId PackageId, DateTimeOffset OccurredOn) : IDomainEvent;
```

Add `backend/ePrevzem.Domain/Pickups/Events/PackageRetrievedAfterExpiry.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackageRetrievedAfterExpiry(
    PackageId PackageId,
    PlacementId PlacementId,
    EmployeeAccountId RetrievedByEmployeeAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

Add `backend/ePrevzem.Domain/Pickups/Events/PackageMarkedPickedUpManually.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackageMarkedPickedUpManually(
    PackageId PackageId,
    EmployeeAccountId MarkedByEmployeeAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

Append to `Package.cs`:
```csharp
    public void MarkExpired(DateTimeOffset now)
    {
        if (Status != PackageStatus.InLocker)
            throw new InvalidOperationException($"Expiry can only be marked from InLocker (current: {Status}).");
        if (DeadlineAt is null || now < DeadlineAt)
            throw new InvalidOperationException("Cannot mark expired before the deadline has passed.");

        Status = PackageStatus.NotPickedUp;
        Raise(new PackageExpired(Id, now));
    }

    public void RetrieveAfterExpiry(EmployeeAccountId retrievedBy, DateTimeOffset now)
    {
        if (Status != PackageStatus.NotPickedUp)
            throw new InvalidOperationException($"Retrieval after expiry is only allowed from NotPickedUp (current: {Status}).");

        var placement = ActivePlacement
            ?? throw new InvalidOperationException("No active placement to close.");
        placement.CloseByExpiryRetrieval(retrievedBy, now);

        Status = PackageStatus.AwaitingPersonalPickup;
        Raise(new PackageRetrievedAfterExpiry(Id, placement.Id, retrievedBy, now));
    }

    public void MarkPickedUpManually(EmployeeAccountId markedBy, DateTimeOffset now)
    {
        if (Status != PackageStatus.AwaitingPersonalPickup)
            throw new InvalidOperationException($"Manual mark-picked-up is only allowed from AwaitingPersonalPickup (current: {Status}).");

        Status = PackageStatus.PickedUp;
        FinalizedAt = now;
        Raise(new PackageMarkedPickedUpManually(Id, markedBy, now));
    }
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~PackageExpiryTests"`
Expected: PASS (7 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Pickups/Package.cs backend/ePrevzem.Domain/Pickups/Events/PackageExpired.cs backend/ePrevzem.Domain/Pickups/Events/PackageRetrievedAfterExpiry.cs backend/ePrevzem.Domain/Pickups/Events/PackageMarkedPickedUpManually.cs backend/ePrevzem.Tests/Domain/Pickups/PackageExpiryTests.cs
git commit -m "feat(domain): Package expiry, retrieval, and manual personal-pickup transitions"
```

### 9F — Sub-cycle: Cancellation (AwaitingPlacement | InLocker | AwaitingPersonalPickup → Cancelled)

The spec allows cancellation from these three states. `InLocker` is a special case: the spec text says "removal + cancel as two distinct operations", which I interpret as: `Cancel` from `InLocker` is **not** a direct transition; the caller must first `RemoveByEmployee`. Therefore in code, `Cancel` is allowed from `AwaitingPlacement` and `AwaitingPersonalPickup` only. This makes the state machine in §5 of the spec exact and avoids a hidden side effect (closing a placement from inside `Cancel`).

- [ ] **Step 1: Write the failing cancellation tests**

`backend/ePrevzem.Tests/Domain/Pickups/PackageCancellationTests.cs`:
```csharp
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using ePrevzem.Domain.Pickups.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Pickups;

public class PackageCancellationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Cancel_from_AwaitingPlacement_finalises_package()
    {
        var pkg = NewPackage();
        var employee = EmployeeAccountId.New();

        pkg.Cancel(employee, Now.AddMinutes(1));

        pkg.Status.Should().Be(PackageStatus.Cancelled);
        pkg.FinalizedAt.Should().Be(Now.AddMinutes(1));
        pkg.DomainEvents.OfType<PackageCancelled>().Should().ContainSingle();
    }

    [Fact]
    public void Cancel_from_AwaitingPersonalPickup_finalises_package()
    {
        var pkg = ExpiredAndRetrievedPackage();
        pkg.Cancel(EmployeeAccountId.New(), Now.AddDays(15));
        pkg.Status.Should().Be(PackageStatus.Cancelled);
        pkg.FinalizedAt.Should().Be(Now.AddDays(15));
    }

    [Fact]
    public void Cancel_from_InLocker_throws_caller_must_remove_first()
    {
        var pkg = PlacedPackage();
        var act = () => pkg.Cancel(EmployeeAccountId.New(), Now.AddDays(1));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*remove*first*");
    }

    [Fact]
    public void Cancel_from_PickedUp_throws()
    {
        var pkg = PlacedPackage();
        pkg.PickUpByCitizen(CitizenUserId.New(), Now.AddDays(1));
        var act = () => pkg.Cancel(EmployeeAccountId.New(), Now.AddDays(2));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_twice_throws()
    {
        var pkg = NewPackage();
        pkg.Cancel(EmployeeAccountId.New(), Now.AddMinutes(1));
        var act = () => pkg.Cancel(EmployeeAccountId.New(), Now.AddMinutes(2));
        act.Should().Throw<InvalidOperationException>();
    }

    private static Package NewPackage() => Package.Create(
        PackageId.New(), OrganizationId.New(), CitizenUserId.New(), EmployeeAccountId.New(),
        PickupStationId.New(), "desc", Now);

    private static Package PlacedPackage()
    {
        var pkg = NewPackage();
        pkg.Place(PlacementId.New(), LockerId.New(), EmployeeAccountId.New(), TimeSpan.FromHours(1), Now);
        pkg.ClearDomainEvents();
        return pkg;
    }

    private static Package ExpiredAndRetrievedPackage()
    {
        var pkg = PlacedPackage();
        pkg.MarkExpired(Now.AddHours(2));
        pkg.RetrieveAfterExpiry(EmployeeAccountId.New(), Now.AddDays(10));
        pkg.ClearDomainEvents();
        return pkg;
    }
}
```

- [ ] **Step 2: Run tests, expect compile failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~PackageCancellationTests"`
Expected: compile error.

- [ ] **Step 3: Add `Cancel` method and event**

Add `backend/ePrevzem.Domain/Pickups/Events/PackageCancelled.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;

namespace ePrevzem.Domain.Pickups.Events;

public sealed record PackageCancelled(
    PackageId PackageId,
    EmployeeAccountId CancelledByEmployeeAccountId,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

Append to `Package.cs`:
```csharp
    public void Cancel(EmployeeAccountId cancelledBy, DateTimeOffset now)
    {
        switch (Status)
        {
            case PackageStatus.AwaitingPlacement:
            case PackageStatus.AwaitingPersonalPickup:
                break;
            case PackageStatus.InLocker:
            case PackageStatus.NotPickedUp:
                throw new InvalidOperationException(
                    "Cancel requires the package not be physically in a locker; remove or retrieve it first.");
            case PackageStatus.PickedUp:
            case PackageStatus.Cancelled:
                throw new InvalidOperationException(
                    $"Cancel is not allowed from terminal state {Status}.");
        }

        Status = PackageStatus.Cancelled;
        FinalizedAt = now;
        Raise(new PackageCancelled(Id, cancelledBy, now));
    }
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~PackageCancellationTests"`
Expected: PASS (5 tests).

- [ ] **Step 5: Run the full Pickups test suite to confirm no regressions**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~Domain.Pickups"`
Expected: PASS (23 tests across the 6 Pickups test files).

- [ ] **Step 6: Commit**

```bash
git add backend/ePrevzem.Domain/Pickups/Package.cs backend/ePrevzem.Domain/Pickups/Events/PackageCancelled.cs backend/ePrevzem.Tests/Domain/Pickups/PackageCancellationTests.cs
git commit -m "feat(domain): Package.Cancel from AwaitingPlacement or AwaitingPersonalPickup"
```

---

## Task 10: Delegations — Delegation aggregate

**Files:**
- Create: `backend/ePrevzem.Domain/Delegations/DelegationId.cs`
- Create: `backend/ePrevzem.Domain/Delegations/Delegation.cs`
- Create: `backend/ePrevzem.Domain/Delegations/Events/DelegationCreated.cs`
- Create: `backend/ePrevzem.Domain/Delegations/Events/DelegationRevoked.cs`
- Test: `backend/ePrevzem.Tests/Domain/Delegations/DelegationTests.cs`

The "delegate must be a CitizenUser" and "delegator must equal package recipient" invariants cross aggregate boundaries; Domain enforces only what is locally checkable (delegator ≠ delegate, IDs present). The cross-aggregate checks belong in the application/handler layer.

- [ ] **Step 1: Write the failing Delegation tests**

`backend/ePrevzem.Tests/Domain/Delegations/DelegationTests.cs`:
```csharp
using ePrevzem.Domain.Delegations;
using ePrevzem.Domain.Delegations.Events;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Pickups;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Delegations;

public class DelegationTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_constructs_active_delegation_and_raises_event()
    {
        var id = DelegationId.New();
        var package = PackageId.New();
        var delegator = CitizenUserId.New();
        var delegate_ = CitizenUserId.New();

        var d = Delegation.Create(id, package, delegator, delegate_, Now);

        d.Id.Should().Be(id);
        d.PackageId.Should().Be(package);
        d.DelegatorCitizenUserId.Should().Be(delegator);
        d.DelegateCitizenUserId.Should().Be(delegate_);
        d.CreatedAt.Should().Be(Now);
        d.RevokedAt.Should().BeNull();
        d.IsRevoked.Should().BeFalse();
        d.DomainEvents.OfType<DelegationCreated>().Should().ContainSingle();
    }

    [Fact]
    public void Create_with_self_delegation_throws()
    {
        var user = CitizenUserId.New();
        var act = () => Delegation.Create(DelegationId.New(), PackageId.New(), user, user, Now);
        act.Should().Throw<ArgumentException>().WithMessage("*cannot delegate to themselves*");
    }

    [Fact]
    public void Revoke_sets_RevokedAt_and_raises_event()
    {
        var d = Delegation.Create(DelegationId.New(), PackageId.New(), CitizenUserId.New(), CitizenUserId.New(), Now);
        d.Revoke(Now.AddDays(1));

        d.RevokedAt.Should().Be(Now.AddDays(1));
        d.IsRevoked.Should().BeTrue();
        d.DomainEvents.OfType<DelegationRevoked>().Should().ContainSingle();
    }

    [Fact]
    public void Revoke_twice_throws()
    {
        var d = Delegation.Create(DelegationId.New(), PackageId.New(), CitizenUserId.New(), CitizenUserId.New(), Now);
        d.Revoke(Now.AddDays(1));
        var act = () => d.Revoke(Now.AddDays(2));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Revoke_before_CreatedAt_throws()
    {
        var d = Delegation.Create(DelegationId.New(), PackageId.New(), CitizenUserId.New(), CitizenUserId.New(), Now);
        var act = () => d.Revoke(Now.AddSeconds(-1));
        act.Should().Throw<ArgumentException>().WithParameterName("revokedAt");
    }
}
```

- [ ] **Step 2: Run tests, expect compile failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~DelegationTests"`
Expected: compile error.

- [ ] **Step 3: Implement Id, Delegation, events**

`backend/ePrevzem.Domain/Delegations/DelegationId.cs`:
```csharp
namespace ePrevzem.Domain.Delegations;

public readonly record struct DelegationId(Guid Value)
{
    public static DelegationId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Delegations/Events/DelegationCreated.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Pickups;

namespace ePrevzem.Domain.Delegations.Events;

public sealed record DelegationCreated(
    DelegationId DelegationId,
    PackageId PackageId,
    CitizenUserId DelegatorCitizenUserId,
    CitizenUserId DelegateCitizenUserId,
    DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Delegations/Events/DelegationRevoked.cs`:
```csharp
using ePrevzem.Domain.Common;

namespace ePrevzem.Domain.Delegations.Events;

public sealed record DelegationRevoked(DelegationId DelegationId, DateTimeOffset OccurredOn) : IDomainEvent;
```

`backend/ePrevzem.Domain/Delegations/Delegation.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Delegations.Events;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Pickups;

namespace ePrevzem.Domain.Delegations;

public sealed class Delegation : AggregateRoot<DelegationId>
{
    public PackageId PackageId { get; private set; }
    public CitizenUserId DelegatorCitizenUserId { get; private set; }
    public CitizenUserId DelegateCitizenUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    private Delegation() { }

    public static Delegation Create(
        DelegationId id,
        PackageId packageId,
        CitizenUserId delegator,
        CitizenUserId @delegate,
        DateTimeOffset now)
    {
        if (delegator == @delegate)
            throw new ArgumentException("A citizen cannot delegate to themselves.", nameof(@delegate));

        var d = new Delegation
        {
            Id = id,
            PackageId = packageId,
            DelegatorCitizenUserId = delegator,
            DelegateCitizenUserId = @delegate,
            CreatedAt = now
        };
        d.Raise(new DelegationCreated(id, packageId, delegator, @delegate, now));
        return d;
    }

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (RevokedAt is not null)
            throw new InvalidOperationException("Delegation is already revoked.");
        if (revokedAt < CreatedAt)
            throw new ArgumentException("Revoked-at must be on or after created-at.", nameof(revokedAt));

        RevokedAt = revokedAt;
        Raise(new DelegationRevoked(Id, revokedAt));
    }
}
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~DelegationTests"`
Expected: PASS (5 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Delegations backend/ePrevzem.Tests/Domain/Delegations
git commit -m "feat(domain): add Delegation aggregate"
```

---

## Task 11: Audit — AuditLogEntry aggregate

**Files:**
- Create: `backend/ePrevzem.Domain/Audit/AuditLogEntryId.cs`
- Create: `backend/ePrevzem.Domain/Audit/AuditAction.cs`
- Create: `backend/ePrevzem.Domain/Audit/AuditActorKind.cs`
- Create: `backend/ePrevzem.Domain/Audit/AuditTargetKind.cs`
- Create: `backend/ePrevzem.Domain/Audit/AuditLogEntry.cs`
- Test: `backend/ePrevzem.Tests/Domain/Audit/AuditLogEntryTests.cs`

`AuditLogEntry` is append-only. The aggregate has only a `Record` factory; no instance methods modify state. DB-level append-only enforcement (REVOKE UPDATE/DELETE) is an Infrastructure concern handled in a later plan.

- [ ] **Step 1: Write the failing AuditLogEntry tests**

`backend/ePrevzem.Tests/Domain/Audit/AuditLogEntryTests.cs`:
```csharp
using ePrevzem.Domain.Audit;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using ePrevzem.Domain.Pickups;
using FluentAssertions;

namespace ePrevzem.Tests.Domain.Audit;

public class AuditLogEntryTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Record_with_employee_actor_constructs_entry()
    {
        var id = AuditLogEntryId.New();
        var employee = EmployeeAccountId.New();
        var package = PackageId.New();
        var org = OrganizationId.New();

        var entry = AuditLogEntry.Record(
            id,
            Now,
            actorKind: AuditActorKind.Employee,
            actorCitizenUserId: null,
            actorEmployeeAccountId: employee,
            actorSystemAdminId: null,
            organizationId: org,
            action: AuditAction.PackagePlaced,
            targetKind: AuditTargetKind.Package,
            targetId: package.Value,
            details: """{"lockerNumber":3}""");

        entry.Id.Should().Be(id);
        entry.OccurredAt.Should().Be(Now);
        entry.ActorKind.Should().Be(AuditActorKind.Employee);
        entry.ActorEmployeeAccountId.Should().Be(employee);
        entry.OrganizationId.Should().Be(org);
        entry.Action.Should().Be(AuditAction.PackagePlaced);
        entry.TargetKind.Should().Be(AuditTargetKind.Package);
        entry.TargetId.Should().Be(package.Value);
        entry.Details.Should().Be("""{"lockerNumber":3}""");
    }

    [Fact]
    public void Record_with_system_actor_allows_no_actor_ids()
    {
        var entry = AuditLogEntry.Record(
            AuditLogEntryId.New(), Now,
            AuditActorKind.System, null, null, null, null,
            AuditAction.PackageExpired, AuditTargetKind.Package, Guid.NewGuid(), null);

        entry.ActorKind.Should().Be(AuditActorKind.System);
        entry.ActorCitizenUserId.Should().BeNull();
        entry.ActorEmployeeAccountId.Should().BeNull();
        entry.ActorSystemAdminId.Should().BeNull();
    }

    [Theory]
    [InlineData(AuditActorKind.Citizen)]
    [InlineData(AuditActorKind.Employee)]
    [InlineData(AuditActorKind.SystemAdmin)]
    public void Record_non_system_actor_without_matching_id_throws(AuditActorKind kind)
    {
        var act = () => AuditLogEntry.Record(
            AuditLogEntryId.New(), Now, kind, null, null, null, null,
            AuditAction.PackagePlaced, AuditTargetKind.Package, Guid.NewGuid(), null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_non_system_actor_with_multiple_actor_ids_throws()
    {
        var act = () => AuditLogEntry.Record(
            AuditLogEntryId.New(), Now, AuditActorKind.Employee,
            actorCitizenUserId: CitizenUserId.New(),
            actorEmployeeAccountId: EmployeeAccountId.New(),
            actorSystemAdminId: null,
            organizationId: null,
            action: AuditAction.PackagePlaced,
            targetKind: AuditTargetKind.Package,
            targetId: Guid.NewGuid(),
            details: null);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_system_actor_with_actor_id_throws()
    {
        var act = () => AuditLogEntry.Record(
            AuditLogEntryId.New(), Now, AuditActorKind.System,
            actorCitizenUserId: CitizenUserId.New(),
            actorEmployeeAccountId: null,
            actorSystemAdminId: null,
            organizationId: null,
            action: AuditAction.PackageExpired,
            targetKind: AuditTargetKind.Package,
            targetId: Guid.NewGuid(),
            details: null);
        act.Should().Throw<ArgumentException>();
    }
}
```

- [ ] **Step 2: Run tests, expect compile failure**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~AuditLogEntryTests"`
Expected: compile error.

- [ ] **Step 3: Implement Id, three enums, and AuditLogEntry**

`backend/ePrevzem.Domain/Audit/AuditLogEntryId.cs`:
```csharp
namespace ePrevzem.Domain.Audit;

public readonly record struct AuditLogEntryId(Guid Value)
{
    public static AuditLogEntryId New() => new(Guid.NewGuid());
}
```

`backend/ePrevzem.Domain/Audit/AuditActorKind.cs`:
```csharp
namespace ePrevzem.Domain.Audit;

public enum AuditActorKind
{
    Citizen,
    Employee,
    SystemAdmin,
    System
}
```

`backend/ePrevzem.Domain/Audit/AuditTargetKind.cs`:
```csharp
namespace ePrevzem.Domain.Audit;

public enum AuditTargetKind
{
    Package,
    Placement,
    Delegation,
    EmployeeAccount,
    EmployeeDevice,
    CitizenUser,
    CitizenDevice,
    Locker,
    Organization,
    PickupStation,
    StationClaim,
    ProvisioningCode
}
```

`backend/ePrevzem.Domain/Audit/AuditAction.cs`:
```csharp
namespace ePrevzem.Domain.Audit;

public enum AuditAction
{
    // Packages & placements
    PackageCreated,
    PackagePlaced,
    PackagePickedUpByCitizen,
    PackageRemovedByEmployee,
    PackageExpired,
    PackageRetrievedAfterExpiry,
    PackageMarkedPickedUpManually,
    PackageCancelled,

    // Delegations
    DelegationCreated,
    DelegationRevoked,
    DelegationUsedAtPickup,

    // Employees, devices, codes
    ProvisioningCodeIssued,
    ProvisioningCodeRedeemed,
    EmployeeAccountCreated,
    EmployeeAccountDisabled,
    EmployeeAccountReenabled,
    EmployeeAccountRoleGranted,
    EmployeeAccountRoleRevoked,
    EmployeeStationAccessGranted,
    EmployeeStationAccessRevoked,
    EmployeeDeviceRegistered,
    EmployeeDeviceRevoked,
    CitizenDeviceRegistered,
    CitizenDeviceRevoked,

    // Citizens
    CitizenOnboarded,

    // Tenancy & infrastructure
    OrganizationCreated,
    StationClaimed,
    StationReleased,
    LockerCreated,
    LockerServiceabilityChanged,
    LockerOpened
}
```

`backend/ePrevzem.Domain/Audit/AuditLogEntry.cs`:
```csharp
using ePrevzem.Domain.Common;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;

namespace ePrevzem.Domain.Audit;

public sealed class AuditLogEntry : AggregateRoot<AuditLogEntryId>
{
    public DateTimeOffset OccurredAt { get; private set; }
    public AuditActorKind ActorKind { get; private set; }
    public CitizenUserId? ActorCitizenUserId { get; private set; }
    public EmployeeAccountId? ActorEmployeeAccountId { get; private set; }
    public SystemAdminId? ActorSystemAdminId { get; private set; }
    public OrganizationId? OrganizationId { get; private set; }
    public AuditAction Action { get; private set; }
    public AuditTargetKind TargetKind { get; private set; }
    public Guid TargetId { get; private set; }
    public string? Details { get; private set; }

    private AuditLogEntry() { }

    public static AuditLogEntry Record(
        AuditLogEntryId id,
        DateTimeOffset occurredAt,
        AuditActorKind actorKind,
        CitizenUserId? actorCitizenUserId,
        EmployeeAccountId? actorEmployeeAccountId,
        SystemAdminId? actorSystemAdminId,
        OrganizationId? organizationId,
        AuditAction action,
        AuditTargetKind targetKind,
        Guid targetId,
        string? details)
    {
        ValidateActor(actorKind, actorCitizenUserId, actorEmployeeAccountId, actorSystemAdminId);

        return new AuditLogEntry
        {
            Id = id,
            OccurredAt = occurredAt,
            ActorKind = actorKind,
            ActorCitizenUserId = actorCitizenUserId,
            ActorEmployeeAccountId = actorEmployeeAccountId,
            ActorSystemAdminId = actorSystemAdminId,
            OrganizationId = organizationId,
            Action = action,
            TargetKind = targetKind,
            TargetId = targetId,
            Details = details
        };
    }

    private static void ValidateActor(
        AuditActorKind kind,
        CitizenUserId? citizenId,
        EmployeeAccountId? employeeId,
        SystemAdminId? adminId)
    {
        var providedCount = (citizenId is null ? 0 : 1) + (employeeId is null ? 0 : 1) + (adminId is null ? 0 : 1);

        if (kind == AuditActorKind.System)
        {
            if (providedCount != 0)
                throw new ArgumentException("System actor must not carry any actor id.");
            return;
        }

        if (providedCount != 1)
            throw new ArgumentException($"Actor kind {kind} requires exactly one matching actor id.");

        switch (kind)
        {
            case AuditActorKind.Citizen when citizenId is null:
                throw new ArgumentException("Citizen actor requires ActorCitizenUserId.");
            case AuditActorKind.Employee when employeeId is null:
                throw new ArgumentException("Employee actor requires ActorEmployeeAccountId.");
            case AuditActorKind.SystemAdmin when adminId is null:
                throw new ArgumentException("SystemAdmin actor requires ActorSystemAdminId.");
        }
    }
}
```

- [ ] **Step 4: Run tests, expect pass**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~AuditLogEntryTests"`
Expected: PASS (7 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/ePrevzem.Domain/Audit backend/ePrevzem.Tests/Domain/Audit
git commit -m "feat(domain): add AuditLogEntry aggregate"
```

---

## Task 12: Whole-domain smoke run

A final verification step to catch test regressions across the entire domain test suite.

- [ ] **Step 1: Run the full domain test suite**

Run: `dotnet test backend/ePrevzem.Tests --filter "FullyQualifiedName~ePrevzem.Tests.Domain"`
Expected: PASS — totals approximately:

| Feature | Tests |
|---------|------:|
| Lockers (Location, PickupStation, Locker, StationClaim) | 17 |
| Organizations | 7 |
| Identity (CitizenUser, ProvisioningCode, EmployeeAccount, SystemAdmin) | 40 |
| Pickups (Package across 6 files) | 23 |
| Delegations | 5 |
| Audit | 7 |
| **Total** | **~99** |

(Numbers are guides; actual counts depend on `[Theory]` row expansion.)

- [ ] **Step 2: Build the whole solution to confirm nothing else has regressed**

Run: `dotnet build ePrevzem.sln`
Expected: build succeeds with no warnings introduced by Domain changes.

- [ ] **Step 3: Final commit if any cleanup**

No code change is expected here; if a stray file got missed or a using is dangling, fix it and commit with `chore(domain): final cleanup after domain layer plan`.

---

## What this plan does NOT do (separate plans needed)

1. **EF Core mapping plan:** owned `Location`, value-converted strongly-typed Ids, string-persisted enums, jsonb `Details`, owned collections (roles, station access), `EmployeeDevice` revocation index, `Placement` open-locker partial unique index, append-only DB-level constraints on `AuditLogEntry`, migrations.
2. **Repositories + UoW plan:** one repository per aggregate, the `IEPrevzemDbContext` port, transactional save semantics.
3. **Domain-event dispatch plan:** MediatR pipeline behavior that publishes raised `IDomainEvent` collections after `SaveChanges`, and the `IAuditLog` audit pipeline behavior that maps domain events to `AuditLogEntry.Record(...)`.
4. **Application use-cases plan:** one MediatR `IRequest`/handler per state-changing operation in §5 of the spec, validators (FluentValidation), authorization checks, station-access enforcement, and the cross-aggregate invariants Domain does not check (e.g. `Delegation.DelegatorCitizenUserId == Package.RecipientCitizenUserId`, `Package.TargetPickupStationId` must currently be claimed by `Package.OrganizationId`).
5. **API plan:** controllers, OpenAPI, JWT issuance, SI-TRUST adapter port wiring, CORS for the React mocks.
6. **Locker hardware plan:** `ILockerGateway`, Direct4.me adapter, `LockerOpened` audit emission per call.
7. **Notifications plan:** citizen notification on package availability, deadline reminders.