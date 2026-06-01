namespace ePrevzem.Application.Common.Abstractions;

public interface IPackageReferenceGenerator
{
    string Generate(DateTimeOffset now);
}
