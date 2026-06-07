using System.Security.Cryptography;
using System.Text;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.DeviceAuth;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.Refresh;
using ePrevzem.Domain.Identity;
using FluentValidation;
using MediatR;

namespace ePrevzem.Application.Identity.DeviceRefresh;

public sealed record RefreshDeviceTokenCommand(string RefreshToken) : IRequest<DeviceSessionResponse>;

public sealed class RefreshDeviceTokenCommandHandler
    : IRequestHandler<RefreshDeviceTokenCommand, DeviceSessionResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly ICitizenUserRepository _citizenUserRepo;
    private readonly IEmployeeAccountRepository _employeeRepo;
    private readonly IOrganizationRepository _orgRepo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ITokenService _tokenService;

    public RefreshDeviceTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepo,
        ICitizenUserRepository citizenUserRepo,
        IEmployeeAccountRepository employeeRepo,
        IOrganizationRepository orgRepo,
        IUnitOfWork uow,
        IClock clock,
        ITokenService tokenService)
    {
        _refreshTokenRepo = refreshTokenRepo;
        _citizenUserRepo = citizenUserRepo;
        _employeeRepo = employeeRepo;
        _orgRepo = orgRepo;
        _uow = uow;
        _clock = clock;
        _tokenService = tokenService;
    }

    public async Task<DeviceSessionResponse> Handle(
        RefreshDeviceTokenCommand command,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var hash = ComputeHash(command.RefreshToken);

        var current = await _refreshTokenRepo.GetByTokenHashAsync(hash, cancellationToken);
        if (current is null)
            throw new InvalidRefreshTokenException("Invalid refresh token.");

        if (current.RevokedAt is not null)
        {
            await RevokeDescendantsAsync(current, now, cancellationToken);
            current.RecordChainRevocation(now);
            await _uow.SaveChangesAsync(cancellationToken);
            throw new InvalidRefreshTokenException("Invalid refresh token.");
        }

        if (!current.IsActive(now))
            throw new InvalidRefreshTokenException("Invalid refresh token.");

        // Determine owner type and rotate tokens
        if (current.CitizenUserId is not null)
        {
            var citizen = await _citizenUserRepo.GetByIdAsync(current.CitizenUserId.Value, cancellationToken);
            if (citizen is null)
                throw new InvalidRefreshTokenException("Invalid refresh token.");

            var accessResult = _tokenService.IssueAccessToken(citizen);
            var refreshResult = _tokenService.IssueRefreshToken(now);
            var replacement = RefreshToken.IssueForCitizen(
                RefreshTokenId.New(),
                citizen.Id,
                refreshResult.Hash,
                refreshResult.ExpiresAt,
                now);

            current.Rotate(replacement.Id, now);
            await _refreshTokenRepo.AddAsync(replacement, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            // Return session with Guid.Empty as deviceId since it's unknown in refresh flow
            return new DeviceSessionResponse(
                "Citizen",
                Guid.Empty,
                accessResult.Token,
                accessResult.ExpiresAt,
                refreshResult.Plaintext,
                refreshResult.ExpiresAt,
                citizen.FirstName,
                citizen.LastName,
                null,
                null,
                Array.Empty<string>());
        }

        if (current.EmployeeAccountId is not null)
        {
            var employee = await _employeeRepo.GetByIdAsync(current.EmployeeAccountId.Value, cancellationToken);
            if (employee is null)
                throw new InvalidRefreshTokenException("Invalid refresh token.");

            var accessResult = _tokenService.IssueAccessToken(employee);
            var refreshResult = _tokenService.IssueRefreshToken(now);
            var replacement = RefreshToken.IssueForEmployee(
                RefreshTokenId.New(),
                employee.Id,
                refreshResult.Hash,
                refreshResult.ExpiresAt,
                now);

            current.Rotate(replacement.Id, now);
            await _refreshTokenRepo.AddAsync(replacement, cancellationToken);

            var org = await _orgRepo.GetByIdAsync(employee.OrganizationId, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            // Return session with Guid.Empty as deviceId since it's unknown in refresh flow
            return new DeviceSessionResponse(
                "Employee",
                Guid.Empty,
                accessResult.Token,
                accessResult.ExpiresAt,
                refreshResult.Plaintext,
                refreshResult.ExpiresAt,
                employee.FirstName,
                employee.LastName,
                employee.OrganizationId.Value,
                org?.Name,
                employee.Roles.Select(r => r.ToString()).ToList());
        }

        throw new InvalidRefreshTokenException("Invalid refresh token.");
    }

    private async Task RevokeDescendantsAsync(
        RefreshToken trigger,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var nextId = trigger.ReplacedByTokenId;
        while (nextId is { } descendantId)
        {
            var descendant = await _refreshTokenRepo.GetByIdAsync(descendantId, cancellationToken);
            if (descendant is null) return;
            if (descendant.IsActive(now)) descendant.Revoke(now);
            nextId = descendant.ReplacedByTokenId;
        }
    }

    private static string ComputeHash(string plaintext)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)));
}

public sealed class RefreshDeviceTokenCommandValidator : AbstractValidator<RefreshDeviceTokenCommand>
{
    public RefreshDeviceTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
