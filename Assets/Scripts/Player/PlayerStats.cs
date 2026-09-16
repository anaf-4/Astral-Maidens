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

    private const float MaxPower = 4f;

    private int _life;
    private int _spell;
    private float _power;
    private float _invulnTimer;

    public bool IsInvulnerable => _invulnTimer > 0f;

    private void Awake()
    {
        _life = startingLife;
        _spell = startingSpell;
        _power = 0f;
    }

    private void Start()
    {
        OnLifeChanged?.Invoke(_life);
        OnSpellChanged?.Invoke(_spell);
        OnPowerChanged?.Invoke(_power);
    }

    private void Update()
    {
        if (_invulnTimer > 0f) _invulnTimer -= Time.deltaTime;
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
