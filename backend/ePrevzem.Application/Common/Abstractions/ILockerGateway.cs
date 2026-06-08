namespace ePrevzem.Application.Common.Abstractions;

/// <summary>
/// Outbound port to the smart-locker hardware vendor. Opens a physical box by
/// its hardware id and returns the ready-to-play audio token (WAV bytes) the
/// client device plays to actuate the lock.
/// </summary>
public interface ILockerGateway
{
    Task<byte[]> OpenBoxAsync(long boxId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Thrown when the locker hardware rejects an open request or is unreachable.
/// Surfaces as HTTP 502 to the client. <see cref="ErrorNumber"/> carries the
/// vendor error code when one was returned.
/// </summary>
public sealed class LockerOpenException : Exception
{
    public int? ErrorNumber { get; }

    public LockerOpenException(string message, int? errorNumber = null, Exception? innerException = null)
        : base(message, innerException)
        => ErrorNumber = errorNumber;
}
