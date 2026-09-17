using UnityEngine;

public class PlayerHitboxTrigger : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;

    public void SetStats(PlayerStats playerStats)
    {
        stats = playerStats;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        if (bullet == null || stats == null || stats.IsInvulnerable) return;
        if (bullet.Owner != BulletOwner.Enemy) return;

        stats.TakeHit();
        bullet.Despawn();
    }
}
