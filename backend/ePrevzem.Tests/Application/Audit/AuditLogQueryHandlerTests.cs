using ePrevzem.Application.Audit;
using ePrevzem.Application.Audit.Dtos;
using ePrevzem.Application.Audit.Queries;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Audit;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Audit;

public class AuditLogQueryHandlerTests
{
    [Fact]
    public async Task Citizen_handler_scopes_to_citizen_and_citizen_action_whitelist()
    {
        var repo = new CapturingAuditLogRepository();
        var handler = new GetCitizenAuditLogQueryHandler(repo);
        var citizenId = Guid.NewGuid();

        await handler.Handle(
            new GetCitizenAuditLogQuery(citizenId, 200, null, null),
            CancellationToken.None);

        repo.CitizenUserId.Should().Be(new CitizenUserId(citizenId));
        repo.Filter!.Limit.Should().Be(100); // clamped to max
        repo.Filter.ActionsIn.Should().BeEquivalentTo(AuditVisibility.CitizenActions);
        repo.Filter.ActorEmployeeAccountId.Should().BeNull();
    }

    [Fact]
    public async Task Operator_handler_scopes_to_employee_org_and_operator_whitelist()
    {
        var repo = new CapturingAuditLogRepository();
        var handler = new GetOperatorAuditLogQueryHandler(repo);
        var employeeId = Guid.NewGuid();
        var orgId = Guid.NewGuid();

        await handler.Handle(
            new GetOperatorAuditLogQuery(employeeId, orgId, 50, null, null),
            CancellationToken.None);

        repo.OrganizationId.Should().Be(new OrganizationId(orgId));
        repo.Filter!.ActorEmployeeAccountId.Should().Be(new EmployeeAccountId(employeeId));
        repo.Filter.ActionsIn.Should().BeEquivalentTo(AuditVisibility.OperatorActions);
    }

    [Fact]
    public async Task Organization_handler_passes_actor_filter()
    {
        var repo = new CapturingAuditLogRepository();
        var handler = new GetOrganizationAuditLogQueryHandler(repo);
        var orgId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        await handler.Handle(
            new GetOrganizationAuditLogQuery(
                orgId,
                50,
                null,
                null,
                null,
                null,
                AuditActorKind.OrganizationAdmin,
                actorId),
            CancellationToken.None);

        repo.OrganizationId.Should().Be(new OrganizationId(orgId));
        repo.Filter!.ActorKind.Should().Be(AuditActorKind.OrganizationAdmin);
        repo.Filter.ActorId.Should().Be(actorId);
    }

    private sealed class CapturingAuditLogRepository : IAuditLogRepository
    {
        public CitizenUserId? CitizenUserId { get; private set; }
        public OrganizationId? OrganizationId { get; private set; }
        public AuditLogQueryFilter? Filter { get; private set; }

        public Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<AuditLogEntryResponse>> GetForOrganizationAsync(
            OrganizationId organizationId, AuditLogQueryFilter filter, CancellationToken cancellationToken = default)
        {
            OrganizationId = organizationId;
            Filter = filter;
            return Empty();
        }

        public Task<IReadOnlyList<AuditLogEntryResponse>> GetForAdminAsync(
            AuditLogQueryFilter filter, CancellationToken cancellationToken = default)
        {
            Filter = filter;
            return Empty();
        }

        public Task<IReadOnlyList<AuditActorOptionResponse>> GetActorOptionsForOrganizationAsync(
            OrganizationId organizationId, CancellationToken cancellationToken = default)
        {
            OrganizationId = organizationId;
            return Task.FromResult<IReadOnlyList<AuditActorOptionResponse>>([]);
        }

        public Task<IReadOnlyList<AuditLogEntryResponse>> GetForCitizenAsync(
            CitizenUserId citizenUserId, AuditLogQueryFilter filter, CancellationToken cancellationToken = default)
        {
            CitizenUserId = citizenUserId;
            Filter = filter;
            return Empty();
        }

        private static Task<IReadOnlyList<AuditLogEntryResponse>> Empty()
            => Task.FromResult<IReadOnlyList<AuditLogEntryResponse>>([]);
    }
}
