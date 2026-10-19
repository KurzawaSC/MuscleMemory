namespace MuscleMemory.Charting;

public static class ChartHitTest
{
    public static int? Nearest(IReadOnlyList<float> pointXs, float touchX, float tolerance)
    {
        int? nearest = null;
        var nearestDistance = tolerance;

        for (var index = 0; index < pointXs.Count; index++)
        {
            var distance = Math.Abs(pointXs[index] - touchX);

            if (distance <= nearestDistance)
            {
                nearest = index;
                nearestDistance = distance;
            }
        }

        return nearest;
    }
}
