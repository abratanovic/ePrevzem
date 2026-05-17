using ePrevzem.Application.Common.Abstractions;

namespace ePrevzem.Infrastructure.Time;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
