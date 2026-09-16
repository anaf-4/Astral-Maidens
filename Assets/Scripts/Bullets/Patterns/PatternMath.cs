using UnityEngine;

public static class PatternMath
{
    public static float[] RadialAnglesDeg(int n, float baseAngleDeg)
    {
        var angles = new float[n];
        for (int i = 0; i < n; i++)
        {
            angles[i] = baseAngleDeg + i * (360f / n);
        }
        return angles;
    }

    public static float SpiralAngleAtTick(float startAngleDeg, float deltaThetaDeg, int tick)
    {
        return startAngleDeg + deltaThetaDeg * tick;
    }

    public static float[] WayShotAnglesDeg(int k, float baseAngleDeg, float fanAngleDeg)
    {
        var angles = new float[k];
        if (k == 1)
        {
            angles[0] = baseAngleDeg;
            return angles;
        }
        float step = fanAngleDeg / (k - 1);
        float start = baseAngleDeg - fanAngleDeg / 2f;
        for (int i = 0; i < k; i++)
        {
            angles[i] = start + step * i;
        }
        return angles;
    }

    public static Vector2 AngleToDirection(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
