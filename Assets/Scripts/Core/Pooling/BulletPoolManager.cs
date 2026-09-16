using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance { get; private set; }

    private readonly Dictionary<GameObject, ObjectPool<Bullet>> _pools = new();
    private readonly HashSet<Bullet> _active = new();

    public IReadOnlyCollection<Bullet> ActiveBullets => _active;

    private void Awake()
    {
        Instance = this;
    }

    public Bullet Spawn(GameObject prefab, Vector2 position, Vector2 velocity)
    {
        if (!_pools.TryGetValue(prefab, out var pool))
        {
            pool = new ObjectPool<Bullet>(
                createFunc: () => CreateBullet(prefab),
                actionOnGet: b => b.gameObject.SetActive(true),
                actionOnRelease: b => b.gameObject.SetActive(false),
                actionOnDestroy: b => Destroy(b.gameObject),
                defaultCapacity: 64);
            _pools[prefab] = pool;
        }

        var bullet = pool.Get();
        bullet.transform.position = position;
        bullet.Init(this, prefab, velocity);
        bullet.OnSpawn();
        _active.Add(bullet);
        return bullet;
    }

    public void Despawn(GameObject prefab, Bullet bullet)
    {
        if (!_active.Remove(bullet)) return;
        bullet.OnDespawn();
        _pools[prefab].Release(bullet);
    }

    private Bullet CreateBullet(GameObject prefab)
    {
        var go = Instantiate(prefab, transform);
        return go.GetComponent<Bullet>();
    }
}
