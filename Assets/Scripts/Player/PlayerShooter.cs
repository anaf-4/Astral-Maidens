using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private InputActionAsset controlsAsset;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireInterval = 0.1f;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private PlayerStats stats;

    private InputAction _fire;
    private float _cooldown;

    public void Configure(InputActionAsset controls, GameObject prefab)
    {
        controlsAsset = controls;
        bulletPrefab = prefab;
    }

    public void SetStats(PlayerStats playerStats)
    {
        stats = playerStats;
    }

    private void Awake()
    {
        var map = controlsAsset.FindActionMap("Gameplay");
        _fire = map.FindAction("Fire");
    }

    private void OnEnable()
    {
        if (stats != null) stats.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        if (stats != null) stats.OnGameOver -= HandleGameOver;
    }

    private void HandleGameOver()
    {
        enabled = false;
    }

    private void Update()
    {
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;

        if (_fire.IsPressed() && _cooldown <= 0f && BulletPoolManager.Instance != null)
        {
            BulletPoolManager.Instance.Spawn(bulletPrefab, transform.position, Vector2.up * bulletSpeed);
            _cooldown = fireInterval;
        }
    }
}
