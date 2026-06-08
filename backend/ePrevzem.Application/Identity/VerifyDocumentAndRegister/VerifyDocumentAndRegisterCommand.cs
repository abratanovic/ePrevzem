using System.Security.Cryptography;
using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.RegisterCitizenByDocument;
using ePrevzem.Domain.Identity;
using MediatR;

namespace ePrevzem.Application.Identity.VerifyDocumentAndRegister;

public sealed record VerifyDocumentAndRegisterCommand(
    byte[] IdFront,
    string IdFrontFileName,
    IReadOnlyList<SelfieFrame> SelfieFrames,
    string Variant = "driving_licence") : IRequest<RegisterCitizenByDocumentResponse>;

public sealed class VerifyDocumentAndRegisterCommandHandler
    : IRequestHandler<VerifyDocumentAndRegisterCommand, RegisterCitizenByDocumentResponse>
{
    private readonly ICvIdentityClient _cvIdentityClient;
    private readonly ICitizenUserRepository _citizenUserRepository;
    private readonly ICitizenActivationCodeRepository _activationCodeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public VerifyDocumentAndRegisterCommandHandler(
        ICvIdentityClient cvIdentityClient,
        ICitizenUserRepository citizenUserRepository,
        ICitizenActivationCodeRepository activationCodeRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _cvIdentityClient = cvIdentityClient;
        _citizenUserRepository = citizenUserRepository;
        _activationCodeRepository = activationCodeRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<RegisterCitizenByDocumentResponse> Handle(
        VerifyDocumentAndRegisterCommand command,
        CancellationToken cancellationToken)
    {
        var identity = await _cvIdentityClient.VerifyAsync(
            command.IdFront,
            command.IdFrontFileName,
            command.SelfieFrames,
            command.Variant,
            cancellationToken);

        if (!identity.Verified)
            throw new DocumentVerificationFailedException(identity.Reasons);

        var now = _clock.UtcNow;

        var citizen = await _citizenUserRepository.GetByEmsoAsync(identity.Emso!, cancellationToken);
        if (citizen is null)
        {
            citizen = CitizenUser.Onboard(
                CitizenUserId.New(),
                identity.FirstName!,
                identity.LastName!,
                identity.Emso!,
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
            citizen.FirstName,
            citizen.LastName,
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
