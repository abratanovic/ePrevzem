using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Organizations.GrantEmployeeRole;

public sealed record GrantEmployeeRoleCommand(Guid OrganizationId, Guid EmployeeId, string Role) : IRequest;

public sealed class GrantEmployeeRoleCommandHandler : IRequestHandler<GrantEmployeeRoleCommand>
{
    private readonly IEmployeeAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public GrantEmployeeRoleCommandHandler(IEmployeeAccountRepository repo, IUnitOfWork uow, IClock clock)
    {
        _repo = repo;
        _uow = uow;
        _clock = clock;
    }

    public async Task Handle(GrantEmployeeRoleCommand command, CancellationToken cancellationToken)
    {
        var employee = await _repo.GetByIdAsync(new EmployeeAccountId(command.EmployeeId), cancellationToken)
            ?? throw new EmployeeNotFoundException(command.EmployeeId);

        if (employee.OrganizationId != new OrganizationId(command.OrganizationId))
            throw new EmployeeForbiddenException();

        var role = Enum.Parse<EmployeeAccountRole>(command.Role);
        employee.GrantRole(role, _clock.UtcNow);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
