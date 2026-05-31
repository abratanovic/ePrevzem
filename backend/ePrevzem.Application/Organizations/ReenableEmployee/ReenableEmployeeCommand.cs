using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Organizations;
using MediatR;

namespace ePrevzem.Application.Organizations.ReenableEmployee;

public sealed record ReenableEmployeeCommand(Guid OrganizationId, Guid EmployeeId) : IRequest;

public sealed class ReenableEmployeeCommandHandler : IRequestHandler<ReenableEmployeeCommand>
{
    private readonly IEmployeeAccountRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public ReenableEmployeeCommandHandler(IEmployeeAccountRepository repo, IUnitOfWork uow, IClock clock)
    {
        _repo = repo;
        _uow = uow;
        _clock = clock;
    }

    public async Task Handle(ReenableEmployeeCommand command, CancellationToken cancellationToken)
    {
        var employee = await _repo.GetByIdAsync(new EmployeeAccountId(command.EmployeeId), cancellationToken)
            ?? throw new EmployeeNotFoundException(command.EmployeeId);

        if (employee.OrganizationId != new OrganizationId(command.OrganizationId))
            throw new EmployeeForbiddenException();

        employee.Reenable(_clock.UtcNow);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
