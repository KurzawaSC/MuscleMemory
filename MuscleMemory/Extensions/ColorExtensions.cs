namespace MuscleMemory.Extensions;

public static class ColorExtensions
{
    private const double RedLuminanceWeight = 0.2126;
    private const double GreenLuminanceWeight = 0.7152;
    private const double BlueLuminanceWeight = 0.0722;
    private const double LinearSegmentLimit = 0.04045;
    private const double LinearSegmentDivisor = 12.92;
    private const double GammaOffset = 0.055;
    private const double GammaDivisor = 1.055;
    private const double GammaExponent = 2.4;
    private const double DarkForegroundLuminanceThreshold = 0.5;

    public static bool PrefersDarkForeground(this Color background) =>
        RelativeLuminance(background) > DarkForegroundLuminanceThreshold;

    private static double RelativeLuminance(Color color) =>
        (RedLuminanceWeight * ToLinear(color.Red))
        + (GreenLuminanceWeight * ToLinear(color.Green))
        + (BlueLuminanceWeight * ToLinear(color.Blue));

    private static double ToLinear(double channel) =>
        channel <= LinearSegmentLimit
            ? channel / LinearSegmentDivisor
            : Math.Pow((channel + GammaOffset) / GammaDivisor, GammaExponent);
}
