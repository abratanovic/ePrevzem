using ePrevzem.Application.Common.Abstractions;
using ePrevzem.Application.Identity.ChangeOrgAdminPassword;
using ePrevzem.Application.Identity.ChangePasswordUnified;
using ePrevzem.Application.Identity.Dtos;
using ePrevzem.Application.Identity.Login;
using ePrevzem.Application.Identity.LoginUnified;
using ePrevzem.Application.Identity.RegisterCitizen;
using ePrevzem.Application.Identity.RegisterCitizenByDocument;
using ePrevzem.Application.Identity.VerifyDocumentAndRegister;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ePrevzem.Api.Controllers.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string InvalidCredentialsType = "urn:eprevzem:identity:invalid-credentials";

    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    [ProducesResponseType<UnifiedLoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] UnifiedLoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new LoginUnifiedCommand(request.Email, request.Password),
                cancellationToken);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (InvalidCredentialsException)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials",
                type: InvalidCredentialsType,
                detail: "Napačen e-poštni naslov ali geslo.");
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(
                new ChangePasswordUnifiedCommand(request.CurrentPassword, request.NewPassword),
                cancellationToken);
            return NoContent();
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (WrongCurrentPasswordException)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Wrong current password",
                type: "urn:eprevzem:identity:wrong-current-password",
                detail: "Trenutno geslo ni pravilno.");
        }
    }

    [AllowAnonymous]
    [HttpPost("citizen/register")]
    [ProducesResponseType<RegisterCitizenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterCitizen(
        [FromBody] RegisterCitizenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new RegisterCitizenCommand(request.SiTrustToken),
                cancellationToken);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
        catch (InvalidSiTrustTokenException)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid SI-TRUST token",
                type: "urn:eprevzem:identity:invalid-sitrust-token",
                detail: "SI-TRUST žeton je neveljaven ali je potekel.");
        }
    }

    [AllowAnonymous]
    [HttpPost("citizen/register-by-document")]
    [ProducesResponseType<RegisterCitizenByDocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterCitizenByDocument(
        [FromBody] RegisterCitizenByDocumentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _mediator.Send(
                new RegisterCitizenByDocumentCommand(request.Emso, request.FirstName, request.LastName),
                cancellationToken);
            return Ok(response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(CreateValidationProblemDetails(ex));
        }
    }

    [AllowAnonymous]
    [HttpPost("citizen/verify-and-register-by-document")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<RegisterCitizenByDocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> VerifyAndRegisterByDocument(
        [FromForm] IFormFile idFront,
        [FromForm] List<IFormFile> selfieFrames,
        [FromForm] string variant = "driving_licence",
        CancellationToken cancellationToken = default)
    {
        var idFrontBytes = await ReadFormFileAsync(idFront, cancellationToken);
        var selfieFrameData = new List<SelfieFrame>(selfieFrames.Count);
        foreach (var file in selfieFrames)
            selfieFrameData.Add(new SelfieFrame(await ReadFormFileAsync(file, cancellationToken), file.FileName));

        try
        {
            var response = await _mediator.Send(
                new VerifyDocumentAndRegisterCommand(idFrontBytes, idFront.FileName, selfieFrameData, variant),
                cancellationToken);
            return Ok(response);
        }
        catch (DocumentVerificationFailedException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Verification failed",
                detail: string.Join(", ", ex.Reasons));
        }
        catch (CvIdentityUnavailableException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Verification service unavailable",
                detail: ex.Message);
        }
    }

    private static async Task<byte[]> ReadFormFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream((int)file.Length);
        await file.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }

    private static ValidationProblemDetails CreateValidationProblemDetails(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                x => x.Key,
                x => x.Select(y => y.ErrorMessage).ToArray());
        return new ValidationProblemDetails(errors);
    }
}

public sealed record UnifiedLoginRequest(string Email, string Password);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record RegisterCitizenRequest(string SiTrustToken);
public sealed record RegisterCitizenByDocumentRequest(string Emso, string FirstName, string LastName);
