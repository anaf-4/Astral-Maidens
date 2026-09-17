using UnityEngine;
using UnityEngine.UI;

public class BossHudController : MonoBehaviour
{
    [SerializeField] private BossController boss;
    [SerializeField] private Image hpBarFill;
    [SerializeField] private Image timerBarFill;
    [SerializeField] private GameObject timerContainer;

    public void Configure(BossController bossController, Image hpFill, Image timerFill, GameObject timerObj)
    {
        boss = bossController;
        hpBarFill = hpFill;
        timerBarFill = timerFill;
        timerContainer = timerObj;
    }

    private void OnEnable()
    {
        boss.OnPhaseHpChanged += RefreshHp;
        boss.OnPhaseChanged += RefreshPhase;
        boss.OnTimerChanged += RefreshTimer;
    }

    private void OnDisable()
    {
        boss.OnPhaseHpChanged -= RefreshHp;
        boss.OnPhaseChanged -= RefreshPhase;
        boss.OnTimerChanged -= RefreshTimer;
    }

    private void RefreshHp(int current, int max)
    {
        hpBarFill.fillAmount = max > 0 ? (float)current / max : 0f;
    }

    private void RefreshPhase(string phaseName, bool isSpellCard)
    {
        hpBarFill.fillAmount = 1f;
        timerContainer.SetActive(isSpellCard);
    }

    private void RefreshTimer(float remaining, float total)
    {
        if (remaining < 0f || total <= 0f) return;
        timerBarFill.fillAmount = remaining / total;
    }
}
