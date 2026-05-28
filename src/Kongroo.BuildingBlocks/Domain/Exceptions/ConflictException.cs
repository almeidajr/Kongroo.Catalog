namespace Kongroo.BuildingBlocks.Domain.Exceptions;

public sealed class ConflictException : DomainException
{
    public ConflictException(string resourceName, string reason)
        : base($"{resourceName} resource conflict: {reason}.")
    {
        ResourceName = resourceName;
        Reason = reason;
    }

    public ConflictException(string resourceName, string reason, Exception innerException)
        : base($"{resourceName} resource conflict: {reason}.", innerException)
    {
        ResourceName = resourceName;
        Reason = reason;
    }

    public string ResourceName { get; }

    public string Reason { get; }
}
