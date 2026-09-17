using NUnit.Framework;
using UnityEngine;

public class BossPhaseLogicTests
{
    private BossPhaseSO MakeNormalPhase()
    {
        var phase = ScriptableObject.CreateInstance<BossPhaseSO>();
        phase.isSpellCard = false;
        phase.timeLimitSeconds = 0f;
        return phase;
    }

    private BossPhaseSO MakeSpellCardPhase(float timeLimit)
    {
        var phase = ScriptableObject.CreateInstance<BossPhaseSO>();
        phase.isSpellCard = true;
        phase.timeLimitSeconds = timeLimit;
        return phase;
    }

    [Test]
    public void NormalPhase_AdvancesWhenHpZero()
    {
        var phase = MakeNormalPhase();
        Assert.IsTrue(BossPhaseLogic.ShouldAdvancePhase(phase, 0, 5f));
    }

    [Test]
    public void NormalPhase_DoesNotAdvanceWhileHpPositive()
    {
        var phase = MakeNormalPhase();
        Assert.IsFalse(BossPhaseLogic.ShouldAdvancePhase(phase, 5, 999f));
    }

    [Test]
    public void SpellCard_AdvancesOnTimeOverEvenWithHpRemaining()
    {
        var phase = MakeSpellCardPhase(30f);
        Assert.IsTrue(BossPhaseLogic.ShouldAdvancePhase(phase, 10, 30f));
    }

    [Test]
    public void SpellCard_AdvancesOnHpZeroBeforeTimeOver()
    {
        var phase = MakeSpellCardPhase(30f);
        Assert.IsTrue(BossPhaseLogic.ShouldAdvancePhase(phase, 0, 5f));
    }

    [Test]
    public void SpellCard_DoesNotAdvanceBeforeTimeOrHpDepleted()
    {
        var phase = MakeSpellCardPhase(30f);
        Assert.IsFalse(BossPhaseLogic.ShouldAdvancePhase(phase, 10, 10f));
    }
}
