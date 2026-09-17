using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputActionAsset controlsAsset;
    [SerializeField] private SpriteRenderer hitboxRenderer;
    [SerializeField] private float normalSpeed = 6f;
    [SerializeField] private float focusSpeed = 3f;
    [SerializeField] private PlayerStatusController statusController;
    [SerializeField] private PlayerStats stats;

    private InputAction _move;
    private InputAction _focus;

    public void Configure(InputActionAsset controls, SpriteRenderer hitboxVisual)
    {
        controlsAsset = controls;
        hitboxRenderer = hitboxVisual;
    }

    public void SetStatusController(PlayerStatusController status)
    {
        statusController = status;
    }

    public void SetStats(PlayerStats playerStats)
    {
        stats = playerStats;
    }

    private void Awake()
    {
        var map = controlsAsset.FindActionMap("Gameplay");
        _move = map.FindAction("Move");
        _focus = map.FindAction("Focus");
        map.Enable();
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
        bool focusing = _focus.IsPressed();
        float multiplier = statusController != null ? statusController.CurrentSpeedMultiplier : 1f;
        float speed = (focusing ? focusSpeed : normalSpeed) * multiplier;
        if (hitboxRenderer != null) hitboxRenderer.enabled = focusing;

        Vector2 input = _move.ReadValue<Vector2>();
        Vector3 delta = (Vector3)(input.normalized * speed * Time.deltaTime);
        Vector3 pos = transform.position + delta;

        pos.x = Mathf.Clamp(pos.x, PlayfieldBounds.MinX, PlayfieldBounds.MaxX);
        pos.y = Mathf.Clamp(pos.y, PlayfieldBounds.MinY, PlayfieldBounds.MaxY);

        transform.position = pos;
    }
}
