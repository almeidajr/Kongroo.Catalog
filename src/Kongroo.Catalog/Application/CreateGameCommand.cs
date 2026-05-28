using Kongroo.Catalog.Domain;

namespace Kongroo.Catalog.Application;

public sealed record CreateGameCommand(string Title, string Description, decimal PriceAmount, Currency Currency);

