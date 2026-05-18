using System.Security.Cryptography;
using System.Text;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.Dtos;
using MediatR;

namespace ePrevzem.Application.Identity.Refresh;

public sealed record RefreshAdminTokenCommand(string RefreshToken) : IRequest<AdminTokenResponse>;

public sealed class RefreshAdminTokenCommandHandler : IRequestHandler<RefreshAdminTokenCommand, AdminTokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ISystemAdminRepository _systemAdminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ITokenService _tokenService;

    public RefreshAdminTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        ISystemAdminRepository systemAdminRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ITokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _systemAdminRepository = systemAdminRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _tokenService = tokenService;
    }

    public async Task<AdminTokenResponse> Handle(RefreshAdminTokenCommand command, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var presentedTokenHash = ComputeTokenHash(command.RefreshToken);

        var currentToken = await _refreshTokenRepository.GetByTokenHashAsync(presentedTokenHash, cancellationToken);
        if (currentToken is null)
            throw new InvalidRefreshTokenException("Invalid refresh token.");

        if (currentToken.RevokedAt is not null)
        {
            await RevokeDescendantChainAsync(currentToken, now, cancellationToken);
            currentToken.RecordChainRevocation(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidRefreshTokenException("Invalid refresh token.");
        }

        if (!currentToken.IsActive(now))
            throw new InvalidRefreshTokenException("Invalid refresh token.");

        var admin = await _systemAdminRepository.GetByIdAsync(currentToken.SystemAdminId, cancellationToken);
        if (admin is null)
            throw new InvalidRefreshTokenException("Invalid refresh token.");

        var accessToken = _tokenService.IssueAccessToken(admin);
        var refreshToken = _tokenService.IssueRefreshToken(now);
        var replacementToken = Domain.Identity.RefreshToken.Issue(
            Domain.Identity.RefreshTokenId.New(),
            admin.Id,
            refreshToken.Hash,
            refreshToken.ExpiresAt,
            now);

        currentToken.Rotate(replacementToken.Id, now);
        await _refreshTokenRepository.AddAsync(replacementToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AdminTokenResponse(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Plaintext,
            refreshToken.ExpiresAt);
    }

    private async Task RevokeDescendantChainAsync(
        Domain.Identity.RefreshToken triggerToken,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var currentTokenId = triggerToken.ReplacedByTokenId;

        while (currentTokenId is { } descendantId)
        {
            var descendant = await _refreshTokenRepository.GetByIdAsync(descendantId, cancellationToken);
            if (descendant is null)
                return;

            if (descendant.IsActive(now))
                descendant.Revoke(now);

            currentTokenId = descendant.ReplacedByTokenId;
        }
    }

    private static string ComputeTokenHash(string plaintext)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}

public sealed class InvalidRefreshTokenException : Exception
{
    public InvalidRefreshTokenException(string message) : base(message)
    {
    }
}
