using Kongroo.Catalog.Domain;
using Shouldly;

namespace Kongroo.Catalog.UnitTests.Catalog.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void ApplyDiscount_WithValidPercentage_ShouldReturnDiscountedMoney()
    {
        // Arrange
        var money = Money.From(20m, Currency.Usd);
        var discount = Percentage.From(25m);

        // Act
        var discountedMoney = money.ApplyDiscount(discount);

        // Assert
        discountedMoney.ShouldBe(Money.From(15m, Currency.Usd));
    }

    [Fact]
    public void ApplyDiscount_WhenDiscountProducesHalfCent_ShouldRoundAwayFromZero()
    {
        // Arrange
        var money = Money.From(10.05m, Currency.Usd);
        var discount = Percentage.From(50m);

        // Act
        var discountedMoney = money.ApplyDiscount(discount);

        // Assert
        discountedMoney.ShouldBe(Money.From(5.03m, Currency.Usd));
    }
}
