public interface IPlayerStatus
{
    void Tick(float deltaTime);
    bool IsExpired { get; }
}
