using SiTrustMock;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddSingleton<AuthAttemptStore>();
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();

var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret is not configured");
builder.Services.AddSingleton(new JwtSettings(jwtSecret));
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<IAuthService, AuthService>();

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
