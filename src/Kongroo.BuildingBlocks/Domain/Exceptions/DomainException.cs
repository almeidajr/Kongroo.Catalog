namespace Kongroo.BuildingBlocks.Domain.Exceptions;

public abstract class DomainException : InvalidOperationException
{
    protected DomainException(string message)
        : base(message) { }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
