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

app.Run();
