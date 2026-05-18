using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.Login;
using ePrevzem.Domain.Identity;
using MediatR;

namespace ePrevzem.Application.Identity.LoginOrgAdmin;

public sealed record LoginOrganizationAdminCommand(string Email, string Password) : IRequest<OrgAdminTokenResponse>;

public sealed class LoginOrganizationAdminCommandHandler
    : IRequestHandler<LoginOrganizationAdminCommand, OrgAdminTokenResponse>
{
    private const string FakeHashForTiming = "AQAAAAIAAYagAAAAE1AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private readonly IOrganizationAdminAccountRepository _orgAdminRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginOrganizationAdminCommandHandler(
        IOrganizationAdminAccountRepository orgAdminRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _orgAdminRepository = orgAdminRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<OrgAdminTokenResponse> Handle(
        LoginOrganizationAdminCommand command,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        var account = await _orgAdminRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (account is null)
        {
            _passwordHasher.Verify(FakeHashForTiming, command.Password);
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        var verification = _passwordHasher.Verify(account.PasswordHash, command.Password);

        if (verification == PasswordVerification.Failed)
            throw new InvalidCredentialsException("Invalid email or password.");

        if (account.Status == OrganizationAdminAccountStatus.Disabled)
            throw new InvalidCredentialsException("Invalid email or password.");

        if (verification == PasswordVerification.NeedsRehash)
        {
            var newHash = _passwordHasher.Hash(command.Password);
            account.SetPassword(newHash, now);
        }

        account.RecordLogin(now);

        var accessResult = _tokenService.IssueAccessToken(account);
        var refreshResult = _tokenService.IssueRefreshToken(now);

        var refreshToken = RefreshToken.IssueForOrganizationAdmin(
            RefreshTokenId.New(),
            account.Id,
            refreshResult.Hash,
            refreshResult.ExpiresAt,
            now);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrgAdminTokenResponse(
            accessResult.Token,
            accessResult.ExpiresAt,
            refreshResult.Plaintext,
            refreshResult.ExpiresAt,
            account.MustChangePassword);
    }
}
