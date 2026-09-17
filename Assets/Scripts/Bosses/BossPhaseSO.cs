using UnityEngine;

[CreateAssetMenu(fileName = "NewBossPhase", menuName = "AstralMaidens/Boss Phase")]
public class BossPhaseSO : ScriptableObject
{
    public string phaseName;
    public int hp = 30;
    public bool isSpellCard;
    public float timeLimitSeconds;
    public BulletPatternSO[] patterns;
    public float[] patternStartDelays;
    public bool appliesSlow;
    public float slowMultiplier = 0.4f;
    public float slowDuration = 8f;
    public float slowReapplyInterval = 4f;
}
