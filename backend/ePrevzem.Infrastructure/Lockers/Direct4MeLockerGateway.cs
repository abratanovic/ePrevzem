using System.IO.Compression;
using System.Net.Http.Json;
using ePrevzem.Application.Common.Abstractions;
using Microsoft.Extensions.Options;

namespace ePrevzem.Infrastructure.Lockers;

public sealed class Direct4MeOptions
{
    public string BaseUrl { get; set; } = "https://api-d4me-stage.direct4.me/sandbox/v1";
    public string ApiKey { get; set; } = string.Empty;
    public int TokenFormat { get; set; } = 1;
}

/// <summary>
/// Direct4Me adapter for <see cref="ILockerGateway"/>. Calls the vendor
/// <c>POST /Access/openbox</c> endpoint, then base64-decodes (and gunzips when
/// gzip-framed) the returned token into raw WAV bytes. The vendor API key and
/// base URL live in server configuration; clients never see them.
/// </summary>
public sealed class Direct4MeLockerGateway : ILockerGateway
{
    private readonly HttpClient _httpClient;
    private readonly Direct4MeOptions _options;

    public Direct4MeLockerGateway(HttpClient httpClient, IOptions<Direct4MeOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<byte[]> OpenBoxAsync(long boxId, CancellationToken cancellationToken = default)
    {
        OpenBoxResponseBody? body;
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                "Access/openbox",
                new OpenBoxRequestBody(boxId, _options.TokenFormat),
                cancellationToken);
            body = await response.Content.ReadFromJsonAsync<OpenBoxResponseBody>(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new LockerOpenException("Locker hardware is unreachable.", innerException: ex);
        }

        if (!response.IsSuccessStatusCode || body is null || body.Result != 0 || string.IsNullOrEmpty(body.Data))
            throw new LockerOpenException("Locker open was rejected by the hardware.", body?.ErrorNumber);

        var raw = Convert.FromBase64String(body.Data);
        return IsGzip(raw) ? Decompress(raw) : raw;
    }

    private static bool IsGzip(byte[] data) => data.Length >= 2 && data[0] == 0x1f && data[1] == 0x8b;

    private static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    private sealed record OpenBoxRequestBody(long BoxId, int TokenFormat);
    private sealed record OpenBoxResponseBody(string? Data, int Result, int ErrorNumber);
}
