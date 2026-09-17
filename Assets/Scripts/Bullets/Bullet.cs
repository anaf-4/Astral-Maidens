using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Bullet : MonoBehaviour, IPoolable
{
    [SerializeField] private BulletOwner owner = BulletOwner.Enemy;
    [SerializeField] private int damage = 1;

    public BulletOwner Owner => owner;
    public int Damage => damage;
    public Vector2 Velocity { get; private set; }

    private BulletPoolManager _manager;
    private GameObject _prefab;

    public void SetOwner(BulletOwner newOwner)
    {
        owner = newOwner;
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    public void Init(BulletPoolManager manager, GameObject prefab, Vector2 velocity)
    {
        _manager = manager;
        _prefab = prefab;
        Velocity = velocity;
    }

    public void OnSpawn()
    {
    }

    public void OnDespawn()
    {
    }

    public void Despawn()
    {
        _manager.Despawn(_prefab, this);
    }

    private void Update()
    {
        transform.position += (Vector3)(Velocity * Time.deltaTime);
    }
}
