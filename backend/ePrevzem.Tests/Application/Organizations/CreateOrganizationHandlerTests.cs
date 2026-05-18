using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Organizations.Create;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Organizations;

public class CreateOrganizationHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    private static CreateOrganizationCommandHandler BuildHandler(
        TestOrganizationRepository? repo = null,
        TestUnitOfWork? uow = null)
    {
        return new CreateOrganizationCommandHandler(
            repo ?? new TestOrganizationRepository(),
            uow ?? new TestUnitOfWork(),
            new TestClock(Now));
    }

    private static CreateOrganizationCommand ValidCommand(
        string name = "Notarska zbornica",
        string taxNumber = "SI12345678",
        string registrationNumber = "1234567000",
        int days = 7)
        => new(name, taxNumber, registrationNumber, days);

    [Fact]
    public async Task Handle_with_valid_command_returns_response_with_correct_fields()
    {
        var handler = BuildHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Name.Should().Be("Notarska zbornica");
        result.TaxNumber.Should().Be("SI12345678");
        result.RegistrationNumber.Should().Be("1234567000");
        result.DefaultPickupDurationDays.Should().Be(7);
        result.CreatedAt.Should().Be(Now);
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_with_valid_command_persists_organization()
    {
        var repo = new TestOrganizationRepository();
        var handler = BuildHandler(repo: repo);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        repo.Items.Should().HaveCount(1);
        repo.Items[0].Id.Value.Should().Be(result.Id);
    }

    [Fact]
    public async Task Handle_with_valid_command_saves_changes()
    {
        var uow = new TestUnitOfWork();
        var handler = BuildHandler(uow: uow);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        uow.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_with_duplicate_tax_number_throws()
    {
        var repo = new TestOrganizationRepository();
        repo.AddExistingTaxNumber("SI12345678");
        var handler = BuildHandler(repo: repo);

        var act = () => handler.Handle(ValidCommand(taxNumber: "SI12345678"), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateTaxNumberException>();
    }

    [Fact]
    public async Task Handle_with_duplicate_registration_number_throws()
    {
        var repo = new TestOrganizationRepository();
        repo.AddExistingRegistrationNumber("1234567000");
        var handler = BuildHandler(repo: repo);

        var act = () => handler.Handle(ValidCommand(registrationNumber: "1234567000"), CancellationToken.None);

        await act.Should().ThrowAsync<DuplicateRegistrationNumberException>();
    }

    [Fact]
    public async Task Handle_with_duplicate_tax_number_does_not_persist()
    {
        var repo = new TestOrganizationRepository();
        repo.AddExistingTaxNumber("SI12345678");
        var handler = BuildHandler(repo: repo);

        try { await handler.Handle(ValidCommand(taxNumber: "SI12345678"), CancellationToken.None); }
        catch (DuplicateTaxNumberException) { }

        repo.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_duration_days_converts_correctly_to_timespan()
    {
        var repo = new TestOrganizationRepository();
        var handler = BuildHandler(repo: repo);

        await handler.Handle(ValidCommand(days: 14), CancellationToken.None);

        repo.Items[0].DefaultPickupDuration.Should().Be(TimeSpan.FromDays(14));
    }
}

public sealed class TestOrganizationRepository : IOrganizationRepository
{
    public List<Organization> Items { get; } = new();
    private readonly HashSet<string> _existingTaxNumbers = new();
    private readonly HashSet<string> _existingRegistrationNumbers = new();

    public void AddExistingTaxNumber(string taxNumber) => _existingTaxNumbers.Add(taxNumber);
    public void AddExistingRegistrationNumber(string registrationNumber) => _existingRegistrationNumbers.Add(registrationNumber);

    public Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

    public Task<bool> ExistsByTaxNumberAsync(string taxNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_existingTaxNumbers.Contains(taxNumber));

    public Task<bool> ExistsByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_existingRegistrationNumbers.Contains(registrationNumber));

    public Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        Items.Add(organization);
        return Task.CompletedTask;
    }
}

public sealed class TestClock : IClock
{
    private readonly DateTimeOffset _now;
    public TestClock(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}

public sealed class TestUnitOfWork : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }
}
