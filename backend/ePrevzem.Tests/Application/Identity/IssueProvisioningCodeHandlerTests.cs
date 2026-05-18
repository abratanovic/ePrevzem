using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.IssueProvisioningCode;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class IssueProvisioningCodeHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid OrgAdminId = Guid.NewGuid();
    private static readonly Guid OrgId = Guid.NewGuid();

    private static IssueProvisioningCodeCommandHandler BuildHandler(
        TestProvisioningCodeRepo? repo = null,
        TestUnitOfWorkForProvisioningCode? uow = null)
        => new(
            repo ?? new TestProvisioningCodeRepo(),
            uow ?? new TestUnitOfWorkForProvisioningCode(),
            new TestClockForProvisioningCode(Now));

    private static IssueProvisioningCodeCommand ValidCommand(
        IReadOnlyList<EmployeeAccountRole>? roles = null)
        => new(
            OrgAdminId,
            OrgId,
            "Ana",
            "Kovač",
            "ana@example.com",
            roles ?? new[] { EmployeeAccountRole.Operator },
            24);

    [Fact]
    public async Task Handle_returns_code_and_expires_at()
    {
        var handler = BuildHandler();
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Code.Should().NotBeNullOrWhiteSpace();
        result.Code.Should().HaveLength(8);
        result.ExpiresAt.Should().Be(Now.AddHours(24));
    }

    [Fact]
    public async Task Handle_persists_provisioning_code()
    {
        var repo = new TestProvisioningCodeRepo();
        var handler = BuildHandler(repo: repo);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        repo.Items.Should().HaveCount(1);
        repo.Items[0].Code.Should().Be(result.Code);
        repo.Items[0].CreatedByOrganizationAdminId.Value.Should().Be(OrgAdminId);
        repo.Items[0].OrganizationId.Value.Should().Be(OrgId);
    }

    [Fact]
    public async Task Handle_saves_changes()
    {
        var uow = new TestUnitOfWorkForProvisioningCode();
        var handler = BuildHandler(uow: uow);

        await handler.Handle(ValidCommand(), CancellationToken.None);

        uow.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_sets_correct_roles_and_pre_filled_data()
    {
        var repo = new TestProvisioningCodeRepo();
        var handler = BuildHandler(repo: repo);

        await handler.Handle(ValidCommand(roles: new[] { EmployeeAccountRole.RecordManager }), CancellationToken.None);

        repo.Items[0].Roles.Should().ContainSingle(r => r == EmployeeAccountRole.RecordManager);
        repo.Items[0].PreFilledInfo.FirstName.Should().Be("Ana");
        repo.Items[0].PreFilledInfo.LastName.Should().Be("Kovač");
        repo.Items[0].PreFilledInfo.Email.Should().Be("ana@example.com");
    }

    [Fact]
    public async Task Handle_generates_different_codes_on_each_call()
    {
        var handler = BuildHandler();
        var r1 = await handler.Handle(ValidCommand(), CancellationToken.None);
        var r2 = await handler.Handle(ValidCommand(), CancellationToken.None);

        r1.Code.Should().NotBe(r2.Code);
    }
}

public sealed class TestProvisioningCodeRepo : IProvisioningCodeRepository
{
    public List<ProvisioningCode> Items { get; } = new();

    public Task<ProvisioningCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.Code == code));

    public Task AddAsync(ProvisioningCode provisioningCode, CancellationToken cancellationToken = default)
    {
        Items.Add(provisioningCode);
        return Task.CompletedTask;
    }
}

public sealed class TestClockForProvisioningCode : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForProvisioningCode(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}

public sealed class TestUnitOfWorkForProvisioningCode : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }
}
