using System.Collections.Generic;
using UnityEngine;

public class BulletBoundsChecker : MonoBehaviour
{
    [SerializeField] private float marginUnits = 2f;

    private readonly List<Bullet> _toDespawn = new();

    private void Update()
    {
        if (Time.frameCount % 5 != 0) return;
        if (BulletPoolManager.Instance == null) return;

        _toDespawn.Clear();

        foreach (var bullet in BulletPoolManager.Instance.ActiveBullets)
        {
            var p = bullet.transform.position;
            if (p.x < PlayfieldBounds.MinX - marginUnits || p.x > PlayfieldBounds.MaxX + marginUnits ||
                p.y < PlayfieldBounds.MinY - marginUnits || p.y > PlayfieldBounds.MaxY + marginUnits)
            {
                _toDespawn.Add(bullet);
            }
        }

        foreach (var bullet in _toDespawn)
        {
            bullet.Despawn();
        }
    }
}
