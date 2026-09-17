using UnityEngine;

public class PlayerStatusController : MonoBehaviour
{
    private SlowStatus _activeSlow;

    public float CurrentSpeedMultiplier =>
        (_activeSlow != null && !_activeSlow.IsExpired) ? _activeSlow.Multiplier : 1f;

    public void ApplySlow(float multiplier, float duration)
    {
        _activeSlow = new SlowStatus(multiplier, duration);
    }

    private void Update()
    {
        if (_activeSlow == null) return;
        _activeSlow.Tick(Time.deltaTime);
        if (_activeSlow.IsExpired) _activeSlow = null;
    }
}
