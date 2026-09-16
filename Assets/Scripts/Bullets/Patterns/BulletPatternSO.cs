using UnityEngine;

[CreateAssetMenu(fileName = "NewBulletPattern", menuName = "AstralMaidens/Bullet Pattern")]
public class BulletPatternSO : ScriptableObject
{
    public PatternType type;
    public GameObject bulletPrefab;
    public int n = 8;
    public int k = 5;
    public float deltaThetaDeg = 6f;
    public float fanAngleDeg = 60f;
    public float baseAngleDeg;
    public float targetedSpreadDeg = 10f;
    public float speed = 3f;
    public float interval = 1f;
    public bool aimAtPlayer;
}
