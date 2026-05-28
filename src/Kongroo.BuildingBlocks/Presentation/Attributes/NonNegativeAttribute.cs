using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Numerics;

namespace Kongroo.BuildingBlocks.Presentation.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NonNegativeAttribute<T>()
    : RangeAttribute(
        typeof(T),
        T.Zero.ToString(null, CultureInfo.InvariantCulture),
        T.MaxValue.ToString(null, CultureInfo.InvariantCulture)
    )
    where T : INumber<T>, IMinMaxValue<T>;
