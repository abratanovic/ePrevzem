using System.Security.Cryptography;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using MediatR;

namespace ePrevzem.Application.Identity.RegisterCitizen;

public sealed record RegisterCitizenCommand(string SiTrustToken) : IRequest<RegisterCitizenResponse>;

public sealed record RegisterCitizenResponse(
    string FirstName,
    string LastName,
    string Code,
    DateTimeOffset ExpiresAt);

public sealed class InvalidSiTrustTokenException()
    : Exception("SI-TRUST token is invalid or expired.");

public sealed class RegisterCitizenCommandHandler
    : IRequestHandler<RegisterCitizenCommand, RegisterCitizenResponse>
{
    private readonly ISiTrustTokenValidator _siTrustValidator;
    private readonly ICitizenUserRepository _citizenUserRepository;
    private readonly ICitizenActivationCodeRepository _activationCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RegisterCitizenCommandHandler(
        ISiTrustTokenValidator siTrustValidator,
        ICitizenUserRepository citizenUserRepository,
        ICitizenActivationCodeRepository activationCodeRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _siTrustValidator = siTrustValidator;
        _citizenUserRepository = citizenUserRepository;
        _activationCodeRepository = activationCodeRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<RegisterCitizenResponse> Handle(
        RegisterCitizenCommand command,
        CancellationToken cancellationToken)
    {
        var claims = _siTrustValidator.Validate(command.SiTrustToken)
            ?? throw new InvalidSiTrustTokenException();

        var now = _clock.UtcNow;

        var citizen = await _citizenUserRepository.GetByEmsoAsync(claims.Emso, cancellationToken);
        if (citizen is null)
        {
            citizen = CitizenUser.Onboard(
                CitizenUserId.New(),
                claims.FirstName,
                claims.LastName,
                claims.Emso,
                claims.Email,
                claims.Phone,
                now);
            await _citizenUserRepository.AddAsync(citizen, cancellationToken);
        }

        var rawCode = GenerateCode();
        var activationCode = CitizenActivationCode.Issue(
            CitizenActivationCodeId.New(),
            citizen.Id,
            rawCode,
            now,
            now.AddHours(24));

        await _activationCodeRepository.AddAsync(activationCode, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterCitizenResponse(
            claims.FirstName,
            claims.LastName,
            activationCode.Code,
            activationCode.ExpiresAt);
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var buffer = new char[8];
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        return new string(buffer);
    }
}
