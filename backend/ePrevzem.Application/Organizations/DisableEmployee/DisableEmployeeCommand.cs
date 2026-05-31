using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Organizations.DisableEmployee;

public sealed record DisableEmployeeCommand(Guid OrganizationId, Guid EmployeeId) : IRequest;

public sealed class DisableEmployeeCommandHandler : IRequestHandler<DisableEmployeeCommand>
{
    private readonly IEmployeeAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public DisableEmployeeCommandHandler(IEmployeeAccountRepository repo, IUnitOfWork uow, IClock clock)
    {
        _repo = repo;
        _uow = uow;
        _clock = clock;
    }

    public async Task Handle(DisableEmployeeCommand command, CancellationToken cancellationToken)
    {
        var employee = await _repo.GetByIdAsync(new EmployeeAccountId(command.EmployeeId), cancellationToken)
            ?? throw new EmployeeNotFoundException(command.EmployeeId);

        if (employee.OrganizationId != new OrganizationId(command.OrganizationId))
            throw new EmployeeForbiddenException();

        employee.Disable(_clock.UtcNow);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
