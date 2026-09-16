using UnityEngine;

[RequireComponent(typeof(PatternExecutor))]
public class PatternTestEmitter : MonoBehaviour
{
    [SerializeField] private BulletPatternSO[] patterns;
    [SerializeField] private float secondsPerPattern = 6f;

    private PatternExecutor _executor;
    private int _index;
    private float _timer;

    public void SetPatterns(BulletPatternSO[] newPatterns)
    {
        patterns = newPatterns;
    }

    private void Awake()
    {
        _executor = GetComponent<PatternExecutor>();
    }

    private void Start()
    {
        if (patterns == null || patterns.Length == 0) return;
        _executor.SetPattern(patterns[0]);
    }

    private void Update()
    {
        if (patterns == null || patterns.Length == 0) return;
        _timer += Time.deltaTime;
        if (_timer >= secondsPerPattern)
        {
            _timer = 0f;
            _index = (_index + 1) % patterns.Length;
            _executor.SetPattern(patterns[_index]);
        }
    }
}
