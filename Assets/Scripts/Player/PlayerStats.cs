using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public event Action<int> OnLifeChanged;
    public event Action<int> OnSpellChanged;
    public event Action<float> OnPowerChanged;
    public event Action OnGameOver;

    [SerializeField] private int startingLife = 3;
    [SerializeField] private int startingSpell = 3;
    [SerializeField] private float invulnerabilitySeconds = 2f;
    [SerializeField] private float blinkInterval = 0.1f;

    private const float MaxPower = 4f;

    private int _life;
    private int _spell;
    private float _power;
    private float _invulnTimer;
    private SpriteRenderer _spriteRenderer;

    public bool IsInvulnerable => _invulnTimer > 0f;

    private void Awake()
    {
        _life = startingLife;
        _spell = startingSpell;
        _power = 0f;
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        OnLifeChanged?.Invoke(_life);
        OnSpellChanged?.Invoke(_spell);
        OnPowerChanged?.Invoke(_power);
    }

    private void Update()
    {
        if (_invulnTimer > 0f)
        {
            _invulnTimer -= Time.deltaTime;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = Mathf.FloorToInt(_invulnTimer / blinkInterval) % 2 == 0;
            }
        }
        else if (_spriteRenderer != null && !_spriteRenderer.enabled)
        {
            _spriteRenderer.enabled = true;
        }
    }

    public void TakeHit()
    {
        if (IsInvulnerable) return;

        _life--;
        OnLifeChanged?.Invoke(_life);
        _invulnTimer = invulnerabilitySeconds;

        if (_life <= 0)
        {
            OnGameOver?.Invoke();
        }
    }
}
