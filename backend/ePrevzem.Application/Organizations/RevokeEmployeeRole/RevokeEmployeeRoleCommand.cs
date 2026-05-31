using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Organizations.RevokeEmployeeRole;

public sealed record RevokeEmployeeRoleCommand(Guid OrganizationId, Guid EmployeeId, string Role) : IRequest;

public sealed class RevokeEmployeeRoleCommandHandler : IRequestHandler<RevokeEmployeeRoleCommand>
{
    private readonly IEmployeeAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public RevokeEmployeeRoleCommandHandler(IEmployeeAccountRepository repo, IUnitOfWork uow, IClock clock)
    {
        _repo = repo;
        _uow = uow;
        _clock = clock;
    }

    public async Task Handle(RevokeEmployeeRoleCommand command, CancellationToken cancellationToken)
    {
        var employee = await _repo.GetByIdAsync(new EmployeeAccountId(command.EmployeeId), cancellationToken)
            ?? throw new EmployeeNotFoundException(command.EmployeeId);

        if (employee.OrganizationId != new OrganizationId(command.OrganizationId))
            throw new EmployeeForbiddenException();

        var role = Enum.Parse<EmployeeAccountRole>(command.Role);
        employee.RevokeRole(role, _clock.UtcNow);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
