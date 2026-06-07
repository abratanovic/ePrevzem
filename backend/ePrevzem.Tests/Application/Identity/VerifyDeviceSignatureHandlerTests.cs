using System.Security.Cryptography;
using System.Text;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.DeviceAuth;
using ePrevzem.Application.Identity.DeviceVerify;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Domain.Identity;
using ePrevzem.Domain.Lockers;
using ePrevzem.Domain.Organizations;
using FluentAssertions;

namespace ePrevzem.Tests.Application.Identity;

public class VerifyDeviceSignatureHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    private static readonly CitizenUserId CitizenId = CitizenUserId.New();
    private static readonly CitizenDeviceId CitizenDeviceId = CitizenDeviceId.New();
    private static readonly EmployeeAccountId EmployeeId = EmployeeAccountId.New();
    private static readonly EmployeeDeviceId EmployeeDeviceId = EmployeeDeviceId.New();
    private static readonly OrganizationId OrgId = OrganizationId.New();

    private static VerifyDeviceSignatureCommandHandler BuildHandler(
        TestDeviceChallengeRepoForVerify? challengeRepo = null,
        TestCitizenUserRepoForVerify? citizenUserRepo = null,
        TestEmployeeAccountRepoForVerify? employeeRepo = null,
        TestOrganizationRepoForVerify? orgRepo = null,
        TestRefreshTokenRepositoryForVerify? refreshTokenRepo = null,
        TestSignatureVerifier? verifier = null,
        TestUnitOfWorkForVerify? unitOfWork = null)
    {
        challengeRepo ??= new TestDeviceChallengeRepoForVerify();
        citizenUserRepo ??= DefaultCitizenUserRepo();
        employeeRepo ??= new TestEmployeeAccountRepoForVerify();
        orgRepo ??= DefaultOrgRepo();
        refreshTokenRepo ??= new TestRefreshTokenRepositoryForVerify();
        verifier ??= new TestSignatureVerifier();
        unitOfWork ??= new TestUnitOfWorkForVerify();

        return new VerifyDeviceSignatureCommandHandler(
            challengeRepo,
            citizenUserRepo,
            employeeRepo,
            orgRepo,
            refreshTokenRepo,
            verifier,
            unitOfWork,
            new TestClockForVerify(Now),
            new TestTokenServiceForVerify());
    }

    private static TestCitizenUserRepoForVerify DefaultCitizenUserRepo()
    {
        var repo = new TestCitizenUserRepoForVerify();
        var citizen = CitizenUser.Onboard(
            CitizenId, "Janez", "Novak", "1234567890123",
            "janez@example.com", "0123456789", Now);
        var device = citizen.RegisterDevice(CitizenDeviceId, new byte[] { 1, 2, 3 }, "fingerprint", "MyPhone", Now);
        repo.Add(citizen);
        return repo;
    }

    private static TestOrganizationRepoForVerify DefaultOrgRepo()
    {
        var repo = new TestOrganizationRepoForVerify();
        repo.Add(Organization.Create(OrgId, "Test Org", "SI00000001", "0000001000",
            TimeSpan.FromDays(7), Now));
        return repo;
    }

    [Fact]
    public async Task Handle_with_valid_citizen_signature_verifies_and_returns_session()
    {
        // Setup: create a real P-256 key, export PEM
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();
        var publicKeyPemChars = PemEncoding.Write("PUBLIC KEY", publicKeyBytes);
        var publicKeyPem = new string(publicKeyPemChars);

        // Setup citizen with device using PEM public key
        var citizenRepo = new TestCitizenUserRepoForVerify();
        var citizen = CitizenUser.Onboard(
            CitizenId, "Janez", "Novak", "1234567890123",
            "janez@example.com", "0123456789", Now);
        var device = citizen.RegisterDevice(
            CitizenDeviceId,
            Encoding.UTF8.GetBytes(publicKeyPem),
            "fingerprint",
            "MyPhone",
            Now);
        citizenRepo.Add(citizen);

        // Setup challenge
        var nonce = new byte[] { 1, 2, 3, 4, 5 };
        var challenge = DeviceChallenge.Issue(
            DeviceChallengeId.New(),
            CitizenDeviceId.Value,
            DeviceKind.Citizen,
            nonce,
            Now,
            Now.AddMinutes(2));
        var challengeRepo = new TestDeviceChallengeRepoForVerify();
        challengeRepo.Add(challenge);

        // Sign the nonce
        var signature = ecdsa.SignData(nonce, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        var signatureBase64 = Convert.ToBase64String(signature);

        // Setup verifier
        var verifier = new TestSignatureVerifier();
        verifier.SetupVerification(publicKeyPem, nonce, signature, shouldSucceed: true);

        var refreshTokenRepo = new TestRefreshTokenRepositoryForVerify();
        var handler = BuildHandler(
            challengeRepo: challengeRepo,
            citizenUserRepo: citizenRepo,
            refreshTokenRepo: refreshTokenRepo,
            verifier: verifier);

        var result = await handler.Handle(
            new VerifyDeviceSignatureCommand(CitizenDeviceId.Value, signatureBase64),
            CancellationToken.None);

        result.Role.Should().Be("Citizen");
        result.FirstName.Should().Be("Janez");
        result.LastName.Should().Be("Novak");
        result.AccessToken.Should().NotBeEmpty();
        result.RefreshToken.Should().NotBeEmpty();
        refreshTokenRepo.Items.Should().HaveCount(1);
        challenge.ConsumedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_with_valid_employee_signature_verifies_and_returns_session()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();
        var publicKeyPemChars = PemEncoding.Write("PUBLIC KEY", publicKeyBytes);
        var publicKeyPem = new string(publicKeyPemChars);

        var employeeRepo = new TestEmployeeAccountRepoForVerify();
        var employee = EmployeeAccount.Create(
            EmployeeId,
            OrgId,
            "Ana", "Kovač", "ana@example.com",
            new[] { EmployeeAccountRole.Operator },
            Array.Empty<PickupStationId>(),
            ProvisioningCodeId.New(),
            Now);
        var device = employee.RegisterDevice(
            EmployeeDeviceId,
            Encoding.UTF8.GetBytes(publicKeyPem),
            "fingerprint",
            "LaptopPC",
            Now);
        employeeRepo.Add(employee);

        var nonce = new byte[] { 5, 4, 3, 2, 1 };
        var challenge = DeviceChallenge.Issue(
            DeviceChallengeId.New(),
            EmployeeDeviceId.Value,
            DeviceKind.Employee,
            nonce,
            Now,
            Now.AddMinutes(2));
        var challengeRepo = new TestDeviceChallengeRepoForVerify();
        challengeRepo.Add(challenge);

        var signature = ecdsa.SignData(nonce, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        var signatureBase64 = Convert.ToBase64String(signature);

        var verifier = new TestSignatureVerifier();
        verifier.SetupVerification(publicKeyPem, nonce, signature, shouldSucceed: true);

        var refreshTokenRepo = new TestRefreshTokenRepositoryForVerify();
        var handler = BuildHandler(
            challengeRepo: challengeRepo,
            employeeRepo: employeeRepo,
            refreshTokenRepo: refreshTokenRepo,
            verifier: verifier);

        var result = await handler.Handle(
            new VerifyDeviceSignatureCommand(EmployeeDeviceId.Value, signatureBase64),
            CancellationToken.None);

        result.Role.Should().Be("Employee");
        result.FirstName.Should().Be("Ana");
        result.LastName.Should().Be("Kovač");
        result.OrganizationId.Should().Be(OrgId.Value);
        result.OrganizationName.Should().Be("Test Org");
        result.Roles.Should().Contain("Operator");
        refreshTokenRepo.Items.Should().HaveCount(1);
        challenge.ConsumedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_with_no_active_challenge_throws_invalid_challenge()
    {
        var handler = BuildHandler();

        var act = () => handler.Handle(
            new VerifyDeviceSignatureCommand(Guid.NewGuid(), "signature_data"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidChallengeException>();
    }

    [Fact]
    public async Task Handle_with_invalid_signature_throws_invalid_signature()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicKeyBytes = ecdsa.ExportSubjectPublicKeyInfo();
        var publicKeyPemChars = PemEncoding.Write("PUBLIC KEY", publicKeyBytes);
        var publicKeyPem = new string(publicKeyPemChars);

        var citizenRepo = new TestCitizenUserRepoForVerify();
        var citizen = CitizenUser.Onboard(
            CitizenId, "Janez", "Novak", "1234567890123",
            "janez@example.com", "0123456789", Now);
        var device = citizen.RegisterDevice(
            CitizenDeviceId,
            Encoding.UTF8.GetBytes(publicKeyPem),
            "fingerprint",
            "MyPhone",
            Now);
        citizenRepo.Add(citizen);

        var nonce = new byte[] { 1, 2, 3, 4, 5 };
        var challenge = DeviceChallenge.Issue(
            DeviceChallengeId.New(),
            CitizenDeviceId.Value,
            DeviceKind.Citizen,
            nonce,
            Now,
            Now.AddMinutes(2));
        var challengeRepo = new TestDeviceChallengeRepoForVerify();
        challengeRepo.Add(challenge);

        var verifier = new TestSignatureVerifier();
        verifier.SetupVerification(publicKeyPem, nonce, new byte[] { 0, 0, 0 }, shouldSucceed: false);

        var handler = BuildHandler(
            challengeRepo: challengeRepo,
            citizenUserRepo: citizenRepo,
            verifier: verifier);

        var act = () => handler.Handle(
            new VerifyDeviceSignatureCommand(CitizenDeviceId.Value, "aW52YWxpZF9zaWduYXR1cmU="),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidSignatureException>();
    }

    [Fact]
    public async Task Handle_with_valid_base64_but_wrong_signature_throws_invalid_signature_and_preserves_challenge()
    {
        // Regression test for BUG 1: signature result must be enforced
        // Create two P-256 keys: one for the device, one for signing (different key)
        using var deviceEcdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var devicePublicKeyBytes = deviceEcdsa.ExportSubjectPublicKeyInfo();
        var devicePublicKeyPemChars = PemEncoding.Write("PUBLIC KEY", devicePublicKeyBytes);
        var devicePublicKeyPem = new string(devicePublicKeyPemChars);

        using var wrongEcdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var citizenRepo = new TestCitizenUserRepoForVerify();
        var citizen = CitizenUser.Onboard(
            CitizenId, "Janez", "Novak", "1234567890123",
            "janez@example.com", "0123456789", Now);
        var device = citizen.RegisterDevice(
            CitizenDeviceId,
            Encoding.UTF8.GetBytes(devicePublicKeyPem),
            "fingerprint",
            "MyPhone",
            Now);
        citizenRepo.Add(citizen);

        var nonce = new byte[] { 1, 2, 3, 4, 5 };
        var challenge = DeviceChallenge.Issue(
            DeviceChallengeId.New(),
            CitizenDeviceId.Value,
            DeviceKind.Citizen,
            nonce,
            Now,
            Now.AddMinutes(2));
        var challengeRepo = new TestDeviceChallengeRepoForVerify();
        challengeRepo.Add(challenge);

        // Sign with the WRONG key, producing valid base64 but wrong signature
        var wrongSignature = wrongEcdsa.SignData(nonce, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        var wrongSignatureBase64 = Convert.ToBase64String(wrongSignature);

        var verifier = new TestSignatureVerifier();
        // Setup: when asked to verify with devicePublicKeyPem, nonce, and the wrong signature, return false
        verifier.SetupVerification(devicePublicKeyPem, nonce, wrongSignature, shouldSucceed: false);

        var handler = BuildHandler(
            challengeRepo: challengeRepo,
            citizenUserRepo: citizenRepo,
            verifier: verifier);

        var act = () => handler.Handle(
            new VerifyDeviceSignatureCommand(CitizenDeviceId.Value, wrongSignatureBase64),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidSignatureException>();
        // Challenge should still be active (NOT consumed)
        challenge.ConsumedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_with_unknown_device_throws_device_not_found()
    {
        var nonce = new byte[] { 1, 2, 3, 4, 5 };
        var challenge = DeviceChallenge.Issue(
            DeviceChallengeId.New(),
            Guid.NewGuid(),
            DeviceKind.Citizen,
            nonce,
            Now,
            Now.AddMinutes(2));
        var challengeRepo = new TestDeviceChallengeRepoForVerify();
        challengeRepo.Add(challenge);

        var citizenRepo = new TestCitizenUserRepoForVerify();
        var employeeRepo = new TestEmployeeAccountRepoForVerify();

        var handler = BuildHandler(
            challengeRepo: challengeRepo,
            citizenUserRepo: citizenRepo,
            employeeRepo: employeeRepo);

        var act = () => handler.Handle(
            new VerifyDeviceSignatureCommand(challenge.DeviceId, "c2lnbmF0dXJl"),
            CancellationToken.None);

        await act.Should().ThrowAsync<DeviceNotFoundException>();
    }
}

public sealed class TestDeviceChallengeRepoForVerify : IDeviceChallengeRepository
{
    private readonly List<DeviceChallenge> _items = new();

    public void Add(DeviceChallenge challenge) => _items.Add(challenge);

    public Task AddAsync(DeviceChallenge challenge, CancellationToken cancellationToken = default)
    {
        if (!_items.Contains(challenge))
        {
            _items.Add(challenge);
        }
        return Task.CompletedTask;
    }

    public Task<DeviceChallenge?> GetLatestActiveAsync(Guid deviceId, DateTimeOffset now, CancellationToken cancellationToken = default)
        => Task.FromResult(_items
            .Where(x => x.DeviceId == deviceId && x.IsConsumable(now))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault());
}

public sealed class TestCitizenUserRepoForVerify : ICitizenUserRepository
{
    private readonly List<CitizenUser> _items = new();

    public void Add(CitizenUser user) => _items.Add(user);

    public Task<CitizenUser?> GetByIdAsync(CitizenUserId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

    public Task<CitizenUser?> GetByEmsoAsync(string emso, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.Emso == emso));

    public Task<CitizenUser?> GetByCitizenDeviceIdAsync(CitizenDeviceId deviceId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.Devices.Any(d => d.Id == deviceId)));

    public Task AddAsync(CitizenUser user, CancellationToken cancellationToken = default)
    {
        if (!_items.Contains(user))
        {
            _items.Add(user);
        }
        return Task.CompletedTask;
    }
}

public sealed class TestEmployeeAccountRepoForVerify : IEmployeeAccountRepository
{
    private readonly List<EmployeeAccount> _items = new();

    public void Add(EmployeeAccount account) => _items.Add(account);

    public Task<EmployeeAccount?> GetByIdAsync(EmployeeAccountId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

    public Task<EmployeeAccount?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.Email == normalizedEmail));

    public Task<EmployeeAccount?> GetByEmployeeDeviceIdAsync(EmployeeDeviceId deviceId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.Devices.Any(d => d.Id == deviceId)));

    public Task<EmployeeAccount?> GetByProvisioningCodeIdAsync(ProvisioningCodeId provisioningCodeId, CancellationToken cancellationToken = default)
        => Task.FromResult(_items.FirstOrDefault(x => x.CreatedFromProvisioningCodeId == provisioningCodeId));

    public Task AddAsync(EmployeeAccount account, CancellationToken cancellationToken = default)
    {
        if (!_items.Contains(account))
        {
            _items.Add(account);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<EmployeeAccount>> GetByOrganisationIdAsync(OrganizationId organisationId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<EmployeeAccount>>(_items);
}

public sealed class TestOrganizationRepoForVerify : IOrganizationRepository
{
    private readonly List<Organization> _orgs = new();

    public void Add(Organization org) => _orgs.Add(org);

    public Task<Organization?> GetByIdAsync(OrganizationId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_orgs.SingleOrDefault(x => x.Id == id));

    public Task<bool> ExistsByTaxNumberAsync(string taxNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_orgs.Any(x => x.TaxNumber == taxNumber));

    public Task<bool> ExistsByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(_orgs.Any(x => x.RegistrationNumber == registrationNumber));

    public Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        _orgs.Add(organization);
        return Task.CompletedTask;
    }
}

public sealed class TestRefreshTokenRepositoryForVerify : IRefreshTokenRepository
{
    public List<RefreshToken> Items { get; } = new();

    public Task<RefreshToken?> GetByIdAsync(RefreshTokenId id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.SingleOrDefault(x => x.TokenHash == tokenHash));

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        Items.Add(refreshToken);
        return Task.CompletedTask;
    }
}

public sealed class TestSignatureVerifier : ISignatureVerifier
{
    private string? _expectedPem;
    private byte[]? _expectedNonce;
    private byte[]? _expectedSignature;
    private bool _shouldSucceed;

    public void SetupVerification(string publicKeyPem, byte[] nonce, byte[] signature, bool shouldSucceed)
    {
        _expectedPem = publicKeyPem;
        _expectedNonce = nonce;
        _expectedSignature = signature;
        _shouldSucceed = shouldSucceed;
    }

    public bool Verify(string publicKeyPem, byte[] data, byte[] signatureDer)
    {
        // Return bool directly: true for valid, false for invalid
        return _shouldSucceed;
    }
}

public sealed class TestUnitOfWorkForVerify : IUnitOfWork
{
    public bool SaveChangesCalled { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCalled = true;
        return Task.FromResult(1);
    }
}

public sealed class TestClockForVerify : IClock
{
    private readonly DateTimeOffset _now;
    public TestClockForVerify(DateTimeOffset now) => _now = now;
    public DateTimeOffset UtcNow => _now;
}

public sealed class TestTokenServiceForVerify : ITokenService
{
    private int _accessTokenCounter = 0;
    private int _refreshTokenCounter = 0;

    public AccessTokenResult IssueAccessToken(SystemAdmin admin)
        => new($"access_token_{++_accessTokenCounter}", _clock.UtcNow.AddHours(1));

    public AccessTokenResult IssueAccessToken(OrganizationAdminAccount admin)
        => new($"access_token_{++_accessTokenCounter}", _clock.UtcNow.AddHours(1));

    public AccessTokenResult IssueAccessToken(EmployeeAccount employee)
        => new($"access_token_{++_accessTokenCounter}", _clock.UtcNow.AddHours(1));

    public AccessTokenResult IssueAccessToken(CitizenUser citizen)
        => new($"access_token_{++_accessTokenCounter}", _clock.UtcNow.AddHours(1));

    public RefreshTokenResult IssueRefreshToken(DateTimeOffset now)
        => new($"refresh_plaintext_{++_refreshTokenCounter}", $"refresh_hash_{_refreshTokenCounter}", now.AddDays(14));

    private static class _clock
    {
        public static readonly DateTimeOffset UtcNow = new(2026, 5, 18, 10, 0, 0, TimeSpan.Zero);
    }
}
