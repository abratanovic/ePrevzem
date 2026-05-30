using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.Login;
using ePrevzem.Domain.Identity;
using MediatR;

namespace ePrevzem.Application.Identity.LoginUnified;

public sealed record LoginUnifiedCommand(string Email, string Password) : IRequest<UnifiedLoginResponse>;

public sealed class LoginUnifiedCommandHandler : IRequestHandler<LoginUnifiedCommand, UnifiedLoginResponse>
{
    private const string FakeHashForTiming = "AQAAAAIAAYagAAAAE1AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private readonly IOrganizationAdminAccountRepository _orgAdminRepository;
    private readonly IEmployeeAccountRepository _employeeRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public LoginUnifiedCommandHandler(
        IOrganizationAdminAccountRepository orgAdminRepository,
        IEmployeeAccountRepository employeeRepository,
        IOrganizationRepository organizationRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _orgAdminRepository = orgAdminRepository;
        _employeeRepository = employeeRepository;
        _organizationRepository = organizationRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<UnifiedLoginResponse> Handle(LoginUnifiedCommand command, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        var orgAdmin = await _orgAdminRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (orgAdmin is not null)
            return await HandleOrgAdmin(orgAdmin, command.Password, now, cancellationToken);

        var employee = await _employeeRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (employee is not null)
            return await HandleEmployee(employee, command.Password, now, cancellationToken);

        _passwordHasher.Verify(FakeHashForTiming, command.Password);
        throw new InvalidCredentialsException("Napačen e-poštni naslov ali geslo.");
    }

    private async Task<UnifiedLoginResponse> HandleOrgAdmin(
        OrganizationAdminAccount account,
        string password,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var verification = _passwordHasher.Verify(account.PasswordHash, password);
        if (verification == PasswordVerification.Failed)
            throw new InvalidCredentialsException("Napačen e-poštni naslov ali geslo.");
        if (account.Status == OrganizationAdminAccountStatus.Disabled)
            throw new InvalidCredentialsException("Napačen e-poštni naslov ali geslo.");

        if (verification == PasswordVerification.NeedsRehash)
            account.SetPassword(_passwordHasher.Hash(password), now);

        account.RecordLogin(now);

        var org = await _organizationRepository.GetByIdAsync(account.OrganizationId, cancellationToken);

        var accessResult = _tokenService.IssueAccessToken(account);
        var refreshResult = _tokenService.IssueRefreshToken(now);
        var refreshToken = RefreshToken.IssueForOrganizationAdmin(
            RefreshTokenId.New(), account.Id, refreshResult.Hash, refreshResult.ExpiresAt, now);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UnifiedLoginResponse(
            accessResult.Token, accessResult.ExpiresAt,
            refreshResult.Plaintext, refreshResult.ExpiresAt,
            "OrganizationAdmin", account.MustChangePassword,
            account.FirstName, account.LastName, account.Email,
            account.OrganizationId.Value, org?.Name);
    }

    private async Task<UnifiedLoginResponse> HandleEmployee(
        EmployeeAccount account,
        string password,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (account.PasswordHash is null)
            throw new InvalidCredentialsException("Napačen e-poštni naslov ali geslo.");

        var verification = _passwordHasher.Verify(account.PasswordHash, password);
        if (verification == PasswordVerification.Failed)
            throw new InvalidCredentialsException("Napačen e-poštni naslov ali geslo.");
        if (account.Status == EmployeeAccountStatus.Disabled)
            throw new InvalidCredentialsException("Napačen e-poštni naslov ali geslo.");

        if (verification == PasswordVerification.NeedsRehash)
            account.SetPassword(_passwordHasher.Hash(password), now);

        account.RecordLogin(now);

        var org = await _organizationRepository.GetByIdAsync(account.OrganizationId, cancellationToken);

        var accessResult = _tokenService.IssueAccessToken(account);
        var refreshResult = _tokenService.IssueRefreshToken(now);
        var refreshToken = RefreshToken.IssueForEmployee(
            RefreshTokenId.New(), account.Id, refreshResult.Hash, refreshResult.ExpiresAt, now);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UnifiedLoginResponse(
            accessResult.Token, accessResult.ExpiresAt,
            refreshResult.Plaintext, refreshResult.ExpiresAt,
            "Employee", account.MustChangePassword,
            account.FirstName, account.LastName, account.Email,
            account.OrganizationId.Value, org?.Name);
    }
}
