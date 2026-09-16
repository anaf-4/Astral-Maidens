using System.Collections.Generic;
using UnityEngine;

public class BulletBoundsChecker : MonoBehaviour
{
    [SerializeField] private float marginUnits = 2f;

    private Camera _camera;
    private readonly List<Bullet> _toDespawn = new();

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if (Time.frameCount % 5 != 0) return;
        if (BulletPoolManager.Instance == null || _camera == null) return;

        float halfHeight = _camera.orthographicSize;
        float halfWidth = halfHeight * _camera.aspect;
        Vector3 camPos = _camera.transform.position;
        float minX = camPos.x - halfWidth - marginUnits;
        float maxX = camPos.x + halfWidth + marginUnits;
        float minY = camPos.y - halfHeight - marginUnits;
        float maxY = camPos.y + halfHeight + marginUnits;

        _toDespawn.Clear();

        foreach (var bullet in BulletPoolManager.Instance.ActiveBullets)
        {
            var p = bullet.transform.position;
            if (p.x < minX || p.x > maxX || p.y < minY || p.y > maxY)
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
