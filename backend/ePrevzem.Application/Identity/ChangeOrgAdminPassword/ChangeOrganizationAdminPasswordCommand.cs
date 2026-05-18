using ePrevzem.Application.Common.Abstractions;
using MediatR;

namespace ePrevzem.Application.Identity.ChangeOrgAdminPassword;

public sealed record ChangeOrganizationAdminPasswordCommand(
    Guid AccountId,
    string CurrentPassword,
    string NewPassword) : IRequest;

public sealed class ChangeOrganizationAdminPasswordCommandHandler
    : IRequestHandler<ChangeOrganizationAdminPasswordCommand>
{
    private readonly IOrganizationAdminAccountRepository _orgAdminRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ChangeOrganizationAdminPasswordCommandHandler(
        IOrganizationAdminAccountRepository orgAdminRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _orgAdminRepository = orgAdminRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task Handle(ChangeOrganizationAdminPasswordCommand command, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var accountId = new Domain.Identity.OrganizationAdminAccountId(command.AccountId);

        var account = await _orgAdminRepository.GetByIdAsync(accountId, cancellationToken)
            ?? throw new InvalidOperationException("Account not found.");

        var verification = _passwordHasher.Verify(account.PasswordHash, command.CurrentPassword);
        if (verification == PasswordVerification.Failed)
            throw new WrongCurrentPasswordException("Current password is incorrect.");

        var newHash = _passwordHasher.Hash(command.NewPassword);
        account.SetPassword(newHash, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class WrongCurrentPasswordException(string message) : Exception(message);
