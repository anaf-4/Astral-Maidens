using UnityEngine;

public class PlayerHitboxTrigger : MonoBehaviour
{
    [SerializeField] private PlayerStats _stats;

    public void SetStats(PlayerStats stats)
    {
        _stats = stats;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        if (bullet == null || _stats == null || _stats.IsInvulnerable) return;

        _stats.TakeHit();
        bullet.Despawn();
    }
}
