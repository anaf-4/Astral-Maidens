using System.Collections;
using UnityEngine;

public class PatternExecutor : MonoBehaviour
{
    [SerializeField] private BulletPatternSO pattern;
    [SerializeField] private Transform playerTransform;

    private int _tick;
    private Coroutine _running;

    public void SetPattern(BulletPatternSO newPattern)
    {
        pattern = newPattern;
        _tick = 0;
        if (isActiveAndEnabled)
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(FireLoop());
        }
    }

    public void SetPlayer(Transform player)
    {
        playerTransform = player;
    }

    private void OnEnable()
    {
        if (pattern != null) _running = StartCoroutine(FireLoop());
    }

    private void OnDisable()
    {
        if (_running != null) StopCoroutine(_running);
    }

    private IEnumerator FireLoop()
    {
        while (true)
        {
            if (pattern == null) yield break;
            Fire();
            yield return new WaitForSeconds(pattern.interval);
        }
    }

    private void Fire()
    {
        if (pattern == null || BulletPoolManager.Instance == null) return;

        float baseAngle = pattern.aimAtPlayer ? AngleToPlayerDeg() : pattern.baseAngleDeg;

        switch (pattern.type)
        {
            case PatternType.Radial:
                FireAtAngles(PatternMath.RadialAnglesDeg(pattern.n, baseAngle));
                break;
            case PatternType.Spiral:
                float armStep = 360f / pattern.n;
                var angles = new float[pattern.n];
                for (int i = 0; i < pattern.n; i++)
                {
                    angles[i] = PatternMath.SpiralAngleAtTick(baseAngle + i * armStep, pattern.deltaThetaDeg, _tick);
                }
                FireAtAngles(angles);
                _tick++;
                break;
            case PatternType.Targeted:
                FireAtAngles(PatternMath.WayShotAnglesDeg(pattern.k, AngleToPlayerDeg(), pattern.targetedSpreadDeg));
                break;
            case PatternType.WayShot:
                FireAtAngles(PatternMath.WayShotAnglesDeg(pattern.k, baseAngle, pattern.fanAngleDeg));
                break;
        }
    }

    private void FireAtAngles(float[] anglesDeg)
    {
        foreach (var angle in anglesDeg)
        {
            var dir = PatternMath.AngleToDirection(angle);
            BulletPoolManager.Instance.Spawn(pattern.bulletPrefab, transform.position, dir * pattern.speed);
        }
    }

    private float AngleToPlayerDeg()
    {
        if (playerTransform == null) return 270f;
        Vector2 diff = playerTransform.position - transform.position;
        return Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
    }
}
