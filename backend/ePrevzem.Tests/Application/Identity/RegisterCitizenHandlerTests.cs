using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.RegisterCitizen;
using ePrevzem.Domain.Identity;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class RegisterCitizenHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly SiTrustClaims ValidClaims = new(
        FirstName: "Ana",
        LastName: "Novak",
        Emso: "1234567890123",
        Email: "ana@example.com",
        Phone: "+38641000001");

    private static RegisterCitizenCommandHandler BuildHandler(
        FakeSiTrustTokenValidator? validator = null,
        FakeCitizenUserRepo? citizenRepo = null,
        FakeCitizenActivationCodeRepo? codeRepo = null,
        FakeUnitOfWork? uow = null)
        => new(
            validator ?? new FakeSiTrustTokenValidator(ValidClaims),
            citizenRepo ?? new FakeCitizenUserRepo(),
            codeRepo ?? new FakeCitizenActivationCodeRepo(),
            uow ?? new FakeUnitOfWork(),
            new FakeClock(Now));

    [Fact]
    public async Task Handle_returns_name_and_code_from_valid_token()
    {
        var handler = BuildHandler();

        var result = await handler.Handle(new RegisterCitizenCommand("valid-token"), CancellationToken.None);

        result.FirstName.Should().Be("Ana");
        result.LastName.Should().Be("Novak");
        result.Code.Should().NotBeNullOrWhiteSpace().And.HaveLength(8);
        result.ExpiresAt.Should().Be(Now.AddHours(24));
    }

    [Fact]
    public async Task Handle_creates_citizen_when_not_found()
    {
        var citizenRepo = new FakeCitizenUserRepo();
        var handler = BuildHandler(citizenRepo: citizenRepo);

        await handler.Handle(new RegisterCitizenCommand("valid-token"), CancellationToken.None);

        citizenRepo.Added.Should().HaveCount(1);
        citizenRepo.Added[0].Emso.Should().Be(ValidClaims.Emso);
    }

    [Fact]
    public async Task Handle_reuses_existing_citizen_when_emso_matches()
    {
        var existing = CitizenUser.Onboard(
            CitizenUserId.New(), "Ana", "Novak", ValidClaims.Emso, null, null, Now);
        var citizenRepo = new FakeCitizenUserRepo(existing);
        var handler = BuildHandler(citizenRepo: citizenRepo);

        await handler.Handle(new RegisterCitizenCommand("valid-token"), CancellationToken.None);

        citizenRepo.Added.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_persists_activation_code()
    {
        var codeRepo = new FakeCitizenActivationCodeRepo();
        var handler = BuildHandler(codeRepo: codeRepo);

        var result = await handler.Handle(new RegisterCitizenCommand("valid-token"), CancellationToken.None);

        codeRepo.Added.Should().HaveCount(1);
        codeRepo.Added[0].Code.Should().Be(result.Code);
    }

    [Fact]
    public async Task Handle_saves_changes()
    {
        var uow = new FakeUnitOfWork();
        var handler = BuildHandler(uow: uow);

        await handler.Handle(new RegisterCitizenCommand("valid-token"), CancellationToken.None);

        uow.SaveChangesCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_throws_when_token_is_invalid()
    {
        var handler = BuildHandler(validator: new FakeSiTrustTokenValidator(null));

        var act = () => handler.Handle(new RegisterCitizenCommand("bad-token"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidSiTrustTokenException>();
    }

    [Fact]
    public async Task Handle_generates_different_codes_on_each_call()
    {
        var handler = BuildHandler();

        var r1 = await handler.Handle(new RegisterCitizenCommand("valid-token"), CancellationToken.None);
        var r2 = await handler.Handle(new RegisterCitizenCommand("valid-token"), CancellationToken.None);

        r1.Code.Should().NotBe(r2.Code);
    }
}

// ---- Test doubles ----

public sealed class FakeSiTrustTokenValidator(SiTrustClaims? claims) : ISiTrustTokenValidator
{
    public SiTrustClaims? Validate(string token) => claims;
}

public sealed class FakeCitizenUserRepo : ICitizenUserRepository
{
    private readonly CitizenUser? _existing;
    public List<CitizenUser> Added { get; } = new();

    public FakeCitizenUserRepo(CitizenUser? existing = null) => _existing = existing;

    public Task<CitizenUser?> GetByEmsoAsync(string emso, CancellationToken cancellationToken = default)
        => Task.FromResult(_existing?.Emso == emso ? _existing : null);

    public Task AddAsync(CitizenUser user, CancellationToken cancellationToken = default)
    {
        Added.Add(user);
        return Task.CompletedTask;
    }
}

public sealed class FakeCitizenActivationCodeRepo : ICitizenActivationCodeRepository
{
    public List<CitizenActivationCode> Added { get; } = new();

    public Task AddAsync(CitizenActivationCode code, CancellationToken cancellationToken = default)
    {
        Added.Add(code);
        return Task.CompletedTask;
    }
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }
}

public sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow => now;
}
