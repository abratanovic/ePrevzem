using System.Security.Cryptography;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Domain.Identity;
using MediatR;

namespace ePrevzem.Application.Identity.RegisterCitizenByDocument;

public sealed record RegisterCitizenByDocumentCommand(
    string Emso,
    string FirstName,
    string LastName) : IRequest<RegisterCitizenByDocumentResponse>;

public sealed record RegisterCitizenByDocumentResponse(
    string FirstName,
    string LastName,
    string Code,
    DateTimeOffset ExpiresAt);

public sealed class RegisterCitizenByDocumentCommandHandler
    : IRequestHandler<RegisterCitizenByDocumentCommand, RegisterCitizenByDocumentResponse>
{
    private readonly ICitizenUserRepository _citizenUserRepository;
    private readonly ICitizenActivationCodeRepository _activationCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RegisterCitizenByDocumentCommandHandler(
        ICitizenUserRepository citizenUserRepository,
        ICitizenActivationCodeRepository activationCodeRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _citizenUserRepository = citizenUserRepository;
        _activationCodeRepository = activationCodeRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<RegisterCitizenByDocumentResponse> Handle(
        RegisterCitizenByDocumentCommand command,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var citizen = await _citizenUserRepository.GetByEmsoAsync(command.Emso, cancellationToken);
        if (citizen is null)
        {
            citizen = CitizenUser.Onboard(
                CitizenUserId.New(),
                command.FirstName,
                command.LastName,
                command.Emso,
                email: null,
                phoneNumber: null,
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

        return new RegisterCitizenByDocumentResponse(
            command.FirstName,
            command.LastName,
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
