namespace Kongroo.Catalog.Application;

/// <summary>Signals that Game Search cannot answer because Elasticsearch did not.</summary>
public sealed class GameSearchUnavailableException : Exception
{
    public GameSearchUnavailableException() { }

    public GameSearchUnavailableException(string message)
        : base(message) { }

    public GameSearchUnavailableException(string message, Exception? innerException)
        : base(message, innerException) { }
}
