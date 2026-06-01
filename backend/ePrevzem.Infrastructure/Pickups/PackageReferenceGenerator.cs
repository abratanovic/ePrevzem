using System.Security.Cryptography;
using ePrevzem.Application.Common.Abstractions;

namespace ePrevzem.Infrastructure.Pickups;

public sealed class PackageReferenceGenerator : IPackageReferenceGenerator
{
    public string Generate(DateTimeOffset now)
        => $"EP-{now.Year}-{RandomNumberGenerator.GetInt32(1_000_000):000000}";
}
