namespace Jiten.Api.Helpers;

public static class DoubleExtensions
{
    public static double ZeroIfNaN(this double value)
    {
        return double.IsNaN(value) ? 0 : value;
    }
}