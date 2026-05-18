using System.Security.Cryptography;
using System.Text;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.Refresh;
using ePrevzem.Domain.Identity;
using MediatR;

namespace ePrevzem.Application.Identity.RefreshOrgAdminToken;

public sealed record RefreshOrganizationAdminTokenCommand(string RefreshToken) : IRequest<OrgAdminTokenResponse>;

public sealed class RefreshOrganizationAdminTokenCommandHandler
    : IRequestHandler<RefreshOrganizationAdminTokenCommand, OrgAdminTokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IOrganizationAdminAccountRepository _orgAdminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ITokenService _tokenService;

    public RefreshOrganizationAdminTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IOrganizationAdminAccountRepository orgAdminRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ITokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _orgAdminRepository = orgAdminRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _tokenService = tokenService;
    }

    public async Task<OrgAdminTokenResponse> Handle(
        RefreshOrganizationAdminTokenCommand command,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var hash = ComputeHash(command.RefreshToken);

        var current = await _refreshTokenRepository.GetByTokenHashAsync(hash, cancellationToken);
        if (current is null)
            throw new InvalidRefreshTokenException("Invalid refresh token.");

        if (current.OrganizationAdminAccountId is null)
            throw new InvalidRefreshTokenException("Invalid refresh token.");

        if (current.RevokedAt is not null)
        {
            await RevokeDescendantsAsync(current, now, cancellationToken);
            current.RecordChainRevocation(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidRefreshTokenException("Invalid refresh token.");
        }

        if (!current.IsActive(now))
            throw new InvalidRefreshTokenException("Invalid refresh token.");

        var account = await _orgAdminRepository.GetByIdAsync(current.OrganizationAdminAccountId.Value, cancellationToken);
        if (account is null)
            throw new InvalidRefreshTokenException("Invalid refresh token.");

        var accessResult = _tokenService.IssueAccessToken(account);
        var refreshResult = _tokenService.IssueRefreshToken(now);
        var replacement = RefreshToken.IssueForOrganizationAdmin(
            RefreshTokenId.New(),
            account.Id,
            refreshResult.Hash,
            refreshResult.ExpiresAt,
            now);

        current.Rotate(replacement.Id, now);
        await _refreshTokenRepository.AddAsync(replacement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrgAdminTokenResponse(
            accessResult.Token,
            accessResult.ExpiresAt,
            refreshResult.Plaintext,
            refreshResult.ExpiresAt,
            account.MustChangePassword);
    }

    private async Task RevokeDescendantsAsync(
        RefreshToken trigger,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var nextId = trigger.ReplacedByTokenId;
        while (nextId is { } descendantId)
        {
            var descendant = await _refreshTokenRepository.GetByIdAsync(descendantId, cancellationToken);
            if (descendant is null) return;
            if (descendant.IsActive(now)) descendant.Revoke(now);
            nextId = descendant.ReplacedByTokenId;
        }
    }

    private static string ComputeHash(string plaintext)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(plaintext)));
}
