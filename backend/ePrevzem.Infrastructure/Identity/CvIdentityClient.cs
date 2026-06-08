using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ePrevzem.Application.Common.Abstractions;
using Microsoft.Extensions.Options;

namespace ePrevzem.Infrastructure.Identity;

public sealed class CvIdentityOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
}

public sealed class CvIdentityClient : ICvIdentityClient
{
    private readonly HttpClient _httpClient;

    public CvIdentityClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CvIdentityVerifyResult> VerifyAsync(
        byte[] idFront,
        string idFrontFileName,
        IReadOnlyList<SelfieFrame> selfieFrames,
        string variant,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(idFront), "id_front", idFrontFileName);
        foreach (var frame in selfieFrames)
            form.Add(new ByteArrayContent(frame.Data), "selfie_frames", frame.FileName);
        form.Add(new StringContent(variant), "variant");

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("verify", form, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new CvIdentityUnavailableException("CV identity service is unreachable.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new CvIdentityUnavailableException(
                $"CV identity service returned {(int)response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<CvIdentityResponse>(cancellationToken)
            ?? throw new CvIdentityUnavailableException("CV identity service returned an empty response.");

        return new CvIdentityVerifyResult(
            result.Verified,
            result.FirstName,
            result.LastName,
            result.Emso,
            result.Reasons ?? []);
    }

    private sealed record CvIdentityResponse(
        [property: JsonPropertyName("verified")] bool Verified,
        [property: JsonPropertyName("first_name")] string? FirstName,
        [property: JsonPropertyName("last_name")] string? LastName,
        [property: JsonPropertyName("emso")] string? Emso,
        [property: JsonPropertyName("reasons")] IReadOnlyList<string>? Reasons);
}
