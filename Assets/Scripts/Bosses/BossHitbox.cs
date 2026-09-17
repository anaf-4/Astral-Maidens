using UnityEngine;

public class BossHitbox : MonoBehaviour
{
    [SerializeField] private BossController boss;

    public void SetBoss(BossController bossController)
    {
        boss = bossController;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        if (bullet == null || boss == null) return;
        if (bullet.Owner != BulletOwner.Player) return;

        boss.TakeDamage(bullet.Damage);
    }
}
