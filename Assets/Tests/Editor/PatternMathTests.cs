using NUnit.Framework;
using UnityEngine;

public class PatternMathTests
{
    [Test]
    public void RadialAnglesDeg_DividesEqually()
    {
        var angles = PatternMath.RadialAnglesDeg(4, 0f);
        Assert.AreEqual(4, angles.Length);
        Assert.AreEqual(0f, angles[0], 0.001f);
        Assert.AreEqual(90f, angles[1], 0.001f);
        Assert.AreEqual(180f, angles[2], 0.001f);
        Assert.AreEqual(270f, angles[3], 0.001f);
    }

    [Test]
    public void SpiralAngleAtTick_AccumulatesRotation()
    {
        Assert.AreEqual(0f, PatternMath.SpiralAngleAtTick(0f, 6f, 0), 0.001f);
        Assert.AreEqual(6f, PatternMath.SpiralAngleAtTick(0f, 6f, 1), 0.001f);
        Assert.AreEqual(60f, PatternMath.SpiralAngleAtTick(0f, 6f, 10), 0.001f);
    }

    [Test]
    public void WayShotAnglesDeg_SpreadsWithinFanAngle()
    {
        var angles = PatternMath.WayShotAnglesDeg(5, 90f, 60f);
        Assert.AreEqual(5, angles.Length);
        Assert.AreEqual(60f, angles[0], 0.001f);
        Assert.AreEqual(90f, angles[2], 0.001f);
        Assert.AreEqual(120f, angles[4], 0.001f);
    }

    [Test]
    public void WayShotAnglesDeg_SingleBulletUsesBaseAngle()
    {
        var angles = PatternMath.WayShotAnglesDeg(1, 45f, 60f);
        Assert.AreEqual(45f, angles[0], 0.001f);
    }

    [Test]
    public void AngleToDirection_ZeroDegreesPointsRight()
    {
        var dir = PatternMath.AngleToDirection(0f);
        Assert.AreEqual(1f, dir.x, 0.001f);
        Assert.AreEqual(0f, dir.y, 0.001f);
    }
}
