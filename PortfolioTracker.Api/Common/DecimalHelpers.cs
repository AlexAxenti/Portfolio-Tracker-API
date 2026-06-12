namespace PortfolioTracker.Api.Common;

public static class DecimalHelpers
{
    public static decimal RoundToThreeDecimals(decimal value)
    {
        return Math.Round(value, 3, MidpointRounding.AwayFromZero);
    }
}
