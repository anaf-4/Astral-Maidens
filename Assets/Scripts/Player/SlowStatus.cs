public class SlowStatus : IPlayerStatus
{
    public float Multiplier { get; }

    private float _remaining;

    public SlowStatus(float multiplier, float duration)
    {
        Multiplier = multiplier;
        _remaining = duration;
    }

    public void Tick(float deltaTime)
    {
        _remaining -= deltaTime;
    }

    public bool IsExpired => _remaining <= 0f;
}
