namespace Kongroo.BuildingBlocks.Domain.Exceptions;

public sealed class UnauthorizedException : DomainException
{
    public UnauthorizedException(string resourceName, string reason)
        : base($"{resourceName} resource unauthorized: {reason}.")
    {
        ResourceName = resourceName;
        Reason = reason;
    }

    public UnauthorizedException(string resourceName, string reason, Exception innerException)
        : base($"{resourceName} resource unauthorized: {reason}.", innerException)
    {
        ResourceName = resourceName;
        Reason = reason;
    }

    public string ResourceName { get; }

    public string Reason { get; }
}
