using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    public event Action<int, int> OnPhaseHpChanged;
    public event Action<string, bool> OnPhaseChanged;
    public event Action<float, float> OnTimerChanged;
    public event Action OnBossDefeated;

    [SerializeField] private BossPhaseSO[] phases;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerStatusController playerStatus;

    private int _phaseIndex = -1;
    private int _currentHp;
    private float _phaseElapsed;
    private bool _defeated;
    private readonly List<GameObject> _activeExecutors = new();
    private Coroutine _slowReapplyRoutine;

    public void SetPlayer(Transform player)
    {
        playerTransform = player;
    }

    public void SetPlayerStatus(PlayerStatusController status)
    {
        playerStatus = status;
    }

    public void SetPhases(BossPhaseSO[] bossPhases)
    {
        phases = bossPhases;
    }

    private void Start()
    {
        StartPhase(0);
    }

    private void Update()
    {
        if (_defeated || phases == null || phases.Length == 0) return;

        _phaseElapsed += Time.deltaTime;
        var phase = phases[_phaseIndex];

        float timerValue = phase.isSpellCard && phase.timeLimitSeconds > 0f
            ? Mathf.Max(0f, phase.timeLimitSeconds - _phaseElapsed)
            : -1f;
        OnTimerChanged?.Invoke(timerValue, phase.timeLimitSeconds);

        if (BossPhaseLogic.ShouldAdvancePhase(phase, _currentHp, _phaseElapsed))
        {
            AdvancePhase();
        }
    }

    public void TakeDamage(int amount)
    {
        if (_defeated || phases == null || phases.Length == 0) return;
        _currentHp = Mathf.Max(0, _currentHp - amount);
        OnPhaseHpChanged?.Invoke(_currentHp, phases[_phaseIndex].hp);
    }

    private void AdvancePhase()
    {
        int next = _phaseIndex + 1;
        if (next >= phases.Length)
        {
            _defeated = true;
            StopAllExecutors();
            OnBossDefeated?.Invoke();
            Debug.Log("Boss defeated");
            return;
        }
        StartPhase(next);
    }

    private void StartPhase(int index)
    {
        StopAllExecutors();
        if (_slowReapplyRoutine != null) StopCoroutine(_slowReapplyRoutine);

        _phaseIndex = index;
        _phaseElapsed = 0f;
        var phase = phases[index];
        _currentHp = phase.hp;

        OnPhaseChanged?.Invoke(phase.phaseName, phase.isSpellCard);
        OnPhaseHpChanged?.Invoke(_currentHp, phase.hp);

        for (int i = 0; i < phase.patterns.Length; i++)
        {
            var executorObj = new GameObject("PatternExecutor_" + i, typeof(PatternExecutor));
            executorObj.transform.SetParent(transform, false);
            var executor = executorObj.GetComponent<PatternExecutor>();
            executor.SetPlayer(playerTransform);
            _activeExecutors.Add(executorObj);

            float delay = i < phase.patternStartDelays.Length ? phase.patternStartDelays[i] : 0f;
            StartCoroutine(DelayedSetPattern(executor, phase.patterns[i], delay));
        }

        if (phase.appliesSlow && playerStatus != null)
        {
            _slowReapplyRoutine = StartCoroutine(ReapplySlow(phase));
        }
    }

    private IEnumerator DelayedSetPattern(PatternExecutor executor, BulletPatternSO pattern, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        executor.SetPattern(pattern);
    }

    private IEnumerator ReapplySlow(BossPhaseSO phase)
    {
        while (true)
        {
            playerStatus.ApplySlow(phase.slowMultiplier, phase.slowDuration);
            yield return new WaitForSeconds(phase.slowReapplyInterval);
        }
    }

    private void StopAllExecutors()
    {
        foreach (var obj in _activeExecutors)
        {
            if (obj != null) Destroy(obj);
        }
        _activeExecutors.Clear();
    }
}
