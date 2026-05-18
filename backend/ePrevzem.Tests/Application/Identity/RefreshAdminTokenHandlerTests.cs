using System.Security.Cryptography;
using System.Text;
using ePrevzem.Application.Identity.Refresh;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Identity.Events;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class RefreshAdminTokenHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_with_unknown_refresh_token_throws_invalid_refresh_token()
    {
        var handler = new RefreshAdminTokenCommandHandler(
            new TestRefreshTokenRepository(),
            new TestSystemAdminRepository(),
            new TestUnitOfWork(),
            new TestClock(Now),
            new TestTokenService());

        var act = () => handler.Handle(new RefreshAdminTokenCommand("missing"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task Handle_with_expired_refresh_token_throws_invalid_refresh_token()
    {
        var admin = SystemAdmin.Create(SystemAdminId.New(), "ops-jane", "hash", Now);
        var systemAdminRepository = new TestSystemAdminRepository();
        systemAdminRepository.Add(admin);

        var refreshTokenRepository = new TestRefreshTokenRepository();
        refreshTokenRepository.Items.Add(RefreshToken.Issue(
            RefreshTokenId.New(),
            admin.Id,
            ComputeTokenHash("expired"),
            Now.AddMinutes(-1),
            Now.AddDays(-1)));

        var handler = new RefreshAdminTokenCommandHandler(
            refreshTokenRepository,
            systemAdminRepository,
            new TestUnitOfWork(),
            new TestClock(Now),
            new TestTokenService());

        var act = () => handler.Handle(new RefreshAdminTokenCommand("expired"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task Handle_with_active_refresh_token_rotates_chain_and_returns_new_pair()
    {
        var admin = SystemAdmin.Create(SystemAdminId.New(), "ops-jane", "hash", Now);
        var systemAdminRepository = new TestSystemAdminRepository();
        systemAdminRepository.Add(admin);

        var existingToken = RefreshToken.Issue(
            RefreshTokenId.New(),
            admin.Id,
            ComputeTokenHash("refresh-token-1"),
            Now.AddDays(14),
            Now);

        var refreshTokenRepository = new TestRefreshTokenRepository();
        refreshTokenRepository.Items.Add(existingToken);
        var unitOfWork = new TestUnitOfWork();

        var handler = new RefreshAdminTokenCommandHandler(
            refreshTokenRepository,
            systemAdminRepository,
            unitOfWork,
            new TestClock(Now.AddMinutes(5)),
            new TestTokenService());

        var response = await handler.Handle(new RefreshAdminTokenCommand("refresh-token-1"), CancellationToken.None);

        response.AccessToken.Should().NotBeEmpty();
        response.RefreshToken.Should().NotBeEmpty();
        refreshTokenRepository.Items.Should().HaveCount(2);
        existingToken.RevokedAt.Should().Be(Now.AddMinutes(5));
        existingToken.ReplacedByTokenId.Should().NotBeNull();
        existingToken.DomainEvents.OfType<RefreshTokenRotated>().Should().ContainSingle();
        unitOfWork.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_with_revoked_refresh_token_revokes_active_descendants_and_raises_chain_event()
    {
        var admin = SystemAdmin.Create(SystemAdminId.New(), "ops-jane", "hash", Now);
        var systemAdminRepository = new TestSystemAdminRepository();
        systemAdminRepository.Add(admin);

        var rootToken = RefreshToken.Issue(
            RefreshTokenId.New(),
            admin.Id,
            ComputeTokenHash("refresh-token-1"),
            Now.AddDays(14),
            Now);
        var descendantToken = RefreshToken.Issue(
            RefreshTokenId.New(),
            admin.Id,
            ComputeTokenHash("refresh-token-2"),
            Now.AddDays(14),
            Now.AddMinutes(1));

        rootToken.Rotate(descendantToken.Id, Now.AddMinutes(2));

        var refreshTokenRepository = new TestRefreshTokenRepository();
        refreshTokenRepository.Items.Add(rootToken);
        refreshTokenRepository.Items.Add(descendantToken);
        var unitOfWork = new TestUnitOfWork();

        var handler = new RefreshAdminTokenCommandHandler(
            refreshTokenRepository,
            systemAdminRepository,
            unitOfWork,
            new TestClock(Now.AddMinutes(3)),
            new TestTokenService());

        var act = () => handler.Handle(new RefreshAdminTokenCommand("refresh-token-1"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
        descendantToken.RevokedAt.Should().Be(Now.AddMinutes(3));
        rootToken.DomainEvents.OfType<RefreshTokenChainRevoked>().Should().ContainSingle()
            .Which.TriggerTokenId.Should().Be(rootToken.Id);
        unitOfWork.SaveChangesCalled.Should().BeTrue();
    }

    private static string ComputeTokenHash(string plaintext)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
