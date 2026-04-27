using QRCoder;
using SiTrustMock;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddSingleton<AuthAttemptStore>();

var app = builder.Build();

app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/auth/initiate", (string? redirectUrl, HttpRequest request, AuthAttemptStore store) =>
{
    if (string.IsNullOrWhiteSpace(redirectUrl))
        return Results.BadRequest(new { error = "redirectUrl is required" });

    var attemptId = store.Create(redirectUrl);

    var host = $"{request.Scheme}://{request.Host}";
    var completeUrl = $"{host}/api/auth/complete?attemptId={attemptId}";

    var qrGenerator = new QRCodeGenerator();
    var qrData = qrGenerator.CreateQrCode(completeUrl, QRCodeGenerator.ECCLevel.Q);
    var qrCode = new PngByteQRCode(qrData);
    var qrBytes = qrCode.GetGraphic(10);
    var qrBase64 = Convert.ToBase64String(qrBytes);

    return Results.Ok(new { attemptId, qrCodeImage = qrBase64 });
});

app.MapPost("/api/auth/complete", (string? attemptId, CompleteAuthRequest? body, AuthAttemptStore store) =>
{
    if (string.IsNullOrWhiteSpace(attemptId))
        return Results.BadRequest(new { error = "attemptId is required" });

    if (body is null ||
        string.IsNullOrWhiteSpace(body.FirstName) ||
        string.IsNullOrWhiteSpace(body.LastName) ||
        string.IsNullOrWhiteSpace(body.Emso) ||
        string.IsNullOrWhiteSpace(body.Phone) ||
        string.IsNullOrWhiteSpace(body.Email) ||
        string.IsNullOrWhiteSpace(body.DateOfBirth) ||
        string.IsNullOrWhiteSpace(body.Address) ||
        string.IsNullOrWhiteSpace(body.Zip) ||
        string.IsNullOrWhiteSpace(body.City))
        return Results.BadRequest(new { error = "All user fields are required" });

    var userData = new UserData(
        body.FirstName!, body.LastName!, body.Emso!, body.Phone!,
        body.Email!, body.DateOfBirth!, body.Address!, body.Zip!, body.City!);

    if (!store.Complete(attemptId, userData))
        return Results.NotFound(new { error = "Attempt not found" });

    return Results.Ok();
});

app.Run();
