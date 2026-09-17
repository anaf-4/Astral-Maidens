public static class BossPhaseLogic
{
    public static bool ShouldAdvancePhase(BossPhaseSO phase, int currentHp, float elapsedSeconds)
    {
        if (currentHp <= 0) return true;
        if (phase.isSpellCard && phase.timeLimitSeconds > 0f && elapsedSeconds >= phase.timeLimitSeconds) return true;
        return false;
    }
}
