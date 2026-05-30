using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.ChangeOrgAdminPassword;
using ePrevzem.Domain.Identity;
using MediatR;

namespace ePrevzem.Application.Identity.ChangePasswordUnified;

public sealed record ChangePasswordUnifiedCommand(string CurrentPassword, string NewPassword) : IRequest;

public sealed class ChangePasswordUnifiedCommandHandler : IRequestHandler<ChangePasswordUnifiedCommand>
{
    private readonly IOrganizationAdminAccountRepository _orgAdminRepository;
    private readonly IEmployeeAccountRepository _employeeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public ChangePasswordUnifiedCommandHandler(
        IOrganizationAdminAccountRepository orgAdminRepository,
        IEmployeeAccountRepository employeeRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser)
    {
        _orgAdminRepository = orgAdminRepository;
        _employeeRepository = employeeRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task Handle(ChangePasswordUnifiedCommand command, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new InvalidOperationException("User is not authenticated.");

        var now = _clock.UtcNow;

        if (_currentUser.IsInRole("OrganizationAdmin"))
        {
            var accountId = new OrganizationAdminAccountId(_currentUser.UserId.Value);
            var account = await _orgAdminRepository.GetByIdAsync(accountId, cancellationToken)
                ?? throw new InvalidOperationException("Account not found.");

            var verification = _passwordHasher.Verify(account.PasswordHash, command.CurrentPassword);
            if (verification == PasswordVerification.Failed)
                throw new WrongCurrentPasswordException("Current password is incorrect.");

            account.SetPassword(_passwordHasher.Hash(command.NewPassword), now);
        }
        else
        {
            var accountId = new EmployeeAccountId(_currentUser.UserId.Value);
            var account = await _employeeRepository.GetByIdAsync(accountId, cancellationToken)
                ?? throw new InvalidOperationException("Account not found.");

            if (account.PasswordHash is null)
                throw new WrongCurrentPasswordException("Current password is incorrect.");

            var verification = _passwordHasher.Verify(account.PasswordHash, command.CurrentPassword);
            if (verification == PasswordVerification.Failed)
                throw new WrongCurrentPasswordException("Current password is incorrect.");

            account.SetPassword(_passwordHasher.Hash(command.NewPassword), now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
