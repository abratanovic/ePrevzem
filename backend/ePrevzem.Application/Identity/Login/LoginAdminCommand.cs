using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using MediatR;

namespace ePrevzem.Application.Identity.Login;

public sealed record LoginAdminCommand(string Username, string Password) : IRequest<AdminTokenResponse>;

public sealed class LoginAdminCommandHandler : IRequestHandler<LoginAdminCommand, AdminTokenResponse>
{
    private const string FakeHashForTiming = "AQAAAAIAAYagAAAAE1AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private readonly ISystemAdminRepository _systemAdminRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public LoginAdminCommandHandler(
        ISystemAdminRepository systemAdminRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IDomainEventDispatcher domainEventDispatcher)
    {
        _systemAdminRepository = systemAdminRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task<AdminTokenResponse> Handle(LoginAdminCommand command, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var normalizedUsername = command.Username.Trim().ToLowerInvariant();

        var admin = await _systemAdminRepository.GetByUsernameAsync(normalizedUsername, cancellationToken);

        if (admin is null)
        {
            _passwordHasher.Verify(FakeHashForTiming, command.Password);
            await _domainEventDispatcher.DispatchAsync(
                [new SystemAdminLoginFailed(normalizedUsername, now)],
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new InvalidCredentialsException("Invalid username or password.");
        }

        var verificationResult = _passwordHasher.Verify(admin.PasswordHash, command.Password);

        if (verificationResult == PasswordVerification.Failed)
        {
            await _domainEventDispatcher.DispatchAsync(
                [new SystemAdminLoginFailed(normalizedUsername, now)],
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new InvalidCredentialsException("Invalid username or password.");
        }

        if (verificationResult == PasswordVerification.NeedsRehash)
        {
            var newHash = _passwordHasher.Hash(command.Password);
            admin.SetPassword(newHash, now);
        }

        admin.RecordLogin(now);

        var accessResult = _tokenService.IssueAccessToken(admin);
        var refreshResult = _tokenService.IssueRefreshToken(now);

        var refreshToken = RefreshToken.IssueForSystemAdmin(
            RefreshTokenId.New(),
            admin.Id,
            refreshResult.Hash,
            refreshResult.ExpiresAt,
            now);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminTokenResponse(
            accessResult.Token,
            accessResult.ExpiresAt,
            refreshResult.Plaintext,
            refreshResult.ExpiresAt);
    }
}

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException(string message) : base(message)
    {
    }
}
