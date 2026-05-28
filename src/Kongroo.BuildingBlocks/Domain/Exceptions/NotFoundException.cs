namespace Kongroo.BuildingBlocks.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string resourceName, string lookup)
        : base($"{resourceName} resource with {lookup} was not found.")
    {
        ResourceName = resourceName;
        Lookup = lookup;
    }

    public NotFoundException(string resourceName, string lookup, Exception innerException)
        : base($"{resourceName} resource with {lookup} was not found.", innerException)
    {
        ResourceName = resourceName;
        Lookup = lookup;
    }

    public string ResourceName { get; }

    public string Lookup { get; }
}
