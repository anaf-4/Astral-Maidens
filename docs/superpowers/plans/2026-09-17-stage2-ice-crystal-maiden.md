# Stage 2 (Ice Crystal Maiden Boss) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add player shooting, a bullet-owner (faction) distinction, a data-driven boss phase/spellcard state machine, a slow status effect, and a fully playable Ice Crystal Maiden boss (2 phases) with an HP/timer HUD to the Phase 1 foundation.

**Architecture:** Reuses Phase 1's `BulletPoolManager`/`Bullet`/`BulletPatternSO`/`PatternExecutor` as-is. Adds a thin orchestration layer (`BossPhaseSO` data + `BossController` MonoBehaviour) that dynamically creates/destroys `PatternExecutor` children per phase — no changes to `PatternExecutor` itself. All Unity Editor state (scenes, prefabs, assets) is built via `mcp__unity-mcp__Unity_RunCommand`, same as Phase 1.

**Tech Stack:** Unity 6000.3.23f1, URP 2D, New Input System (existing `PlayerControls.inputactions`, using its already-bound `Fire` action for the first time).

**Spec:** `E:\Astral Maidens\docs\superpowers\specs\2026-09-17-stage2-ice-crystal-maiden-design.md`

## Global Constraints

- Project root: `E:\Astral Maidens`, current branch `master` (Phase 1 already merged).
- No asmdef for gameplay scripts. EditMode tests live under an `Editor`-named folder (compiles into the default editor assembly, gets `nunit.framework` for free — established in Phase 1).
- `Unity_RunCommand` scratch scripts must fully-qualify `UnityEngine.UI.*` types (`UnityEngine.UI.Image`, not a bare `using` — this project's compile context hits `CS0118` otherwise, established in Phase 1 Task 1/10).
- Any script-authored `.inputactions` change must use `ToJson()`+`File.WriteAllText()`+`AssetDatabase.ImportAsset()`, never `AssetDatabase.CreateAsset()` (established in Phase 1 Task 7) — not needed in this plan since no `.inputactions` file changes, `Fire` binding already exists.
- Any `Instantiate()` of a UI prefab into a parented container must use the 3-arg overload with `worldPositionStays: false` (`Instantiate(prefab, parent, false)`) — the 2-arg overload silently renders invisible icons (Phase 1 post-merge bug, found by the user). Not needed in this plan (no per-count icon spawning), but any new UI construction must use `transform.SetParent(parent, false)`, never the 2-arg `SetParent`.
- Standard verification pattern: after any filesystem script write, `AssetDatabase.Refresh()` via `Unity_RunCommand`, then `Unity_GetConsoleLogs` (`logTypes: "Error"`) must be empty.
- **Cross-reference wiring check (Phase 1 Task 8 lesson):** every field a task wires via a `Configure()`/setter method must be verified with `Unity_ManageGameObject get_component` (`include_non_public_serialized: true`) showing a non-null value — not just "no console errors," which a silently-unwired field will not produce.
- `Unity_RunCommand`/`Unity_ManageEditor` calls intermittently fail with `"Unity not detected (no fresh discovery files found)"` — retry 1-3 times with a `Unity_ManageEditor GetState` probe in between; this is a known transient issue, not a real disconnect (established throughout Phase 1).
- `ProjectSettings/VersionControlSettings.asset` drifts to `Unity Version Control` mode whenever the Editor is open — check `git status` on this file after any scene-saving task and reset `EditorSettings.externalVersionControl = "Visible Meta Files"` if it drifted.
- Player character sprite PPU is 320 (Phase 1 post-merge fix, not 100 as the original Phase 1 spec said) — `Assets/Art/Players/BlueMagicalGirl/*.png`.
- `BulletBoundsChecker` already despawns bullets based on the camera's actual visible extents (Phase 1 post-merge fix) — this applies uniformly to player and boss bullets, no changes needed for this plan.
- Every task ends with a git commit.

---

### Task 1: Bullet Owner/Damage + Player Hitbox Filtering

**Files:**
- Create: `Assets/Scripts/Bullets/BulletOwner.cs`
- Modify: `Assets/Scripts/Bullets/Bullet.cs`
- Modify: `Assets/Scripts/Player/PlayerHitboxTrigger.cs`

**Interfaces:**
- Produces: `enum BulletOwner { Enemy, Player }`; `Bullet.Owner` (get), `Bullet.Damage` (get), `Bullet.SetOwner(BulletOwner)`, `Bullet.SetDamage(int)` — consumed by Task 2 (player bullet prefab), Task 7 (boss hit detection), Task 9 (boss bullet prefabs).

- [ ] **Step 1: Write `BulletOwner.cs`**

```csharp
public enum BulletOwner
{
    Enemy,
    Player
}
```

Write to `E:\Astral Maidens\Assets\Scripts\Bullets\BulletOwner.cs`.

- [ ] **Step 2: Modify `Bullet.cs` to add owner/damage**

Replace the full file content:

```csharp
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Bullet : MonoBehaviour, IPoolable
{
    [SerializeField] private BulletOwner owner = BulletOwner.Enemy;
    [SerializeField] private int damage = 1;

    public BulletOwner Owner => owner;
    public int Damage => damage;
    public Vector2 Velocity { get; private set; }

    private BulletPoolManager _manager;
    private GameObject _prefab;

    public void SetOwner(BulletOwner newOwner)
    {
        owner = newOwner;
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    public void Init(BulletPoolManager manager, GameObject prefab, Vector2 velocity)
    {
        _manager = manager;
        _prefab = prefab;
        Velocity = velocity;
    }

    public void OnSpawn()
    {
    }

    public void OnDespawn()
    {
    }

    public void Despawn()
    {
        _manager.Despawn(_prefab, this);
    }

    private void Update()
    {
        transform.position += (Vector3)(Velocity * Time.deltaTime);
    }
}
```

`owner` defaults to `BulletOwner.Enemy` (first enum member), so the existing `TestBullet.prefab` from Phase 1 needs no prefab edit — Unity applies the C# default for a newly-added serialized field with no stored value.

- [ ] **Step 3: Modify `PlayerHitboxTrigger.cs` to ignore non-enemy bullets**

Replace the full file content:

```csharp
using UnityEngine;

public class PlayerHitboxTrigger : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;

    public void SetStats(PlayerStats playerStats)
    {
        stats = playerStats;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        if (bullet == null || stats == null || stats.IsInvulnerable) return;
        if (bullet.Owner != BulletOwner.Enemy) return;

        stats.TakeHit();
        bullet.Despawn();
    }
}
```

(This carries forward the Phase 1 post-merge invuln-ignore fix — `stats.IsInvulnerable` check — plus the new owner filter. Field is already `[SerializeField]` from the Phase 1 Task 8 fix.)

- [ ] **Step 4: Refresh and verify compile**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        AssetDatabase.Refresh();
        result.Log("Refreshed");
    }
}
```

Then `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: empty.

- [ ] **Step 5: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Scripts/Bullets Assets/Scripts/Player/PlayerHitboxTrigger.cs && git commit -m "Add BulletOwner and filter PlayerHitboxTrigger to enemy bullets only" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 2: Player Shooter + Player Bullet Prefab

**Files:**
- Create: `Assets/Scripts/Player/PlayerShooter.cs`
- Create (via RunCommand): `Assets/Prefabs/Bullets/PlayerBullet.prefab`

**Interfaces:**
- Consumes: `Bullet.SetOwner`, `Bullet.SetDamage` (Task 1); `BulletPoolManager.Instance.Spawn(GameObject, Vector2, Vector2)` (Phase 1); `PlayerStats.OnGameOver` (Phase 1, `event Action`).
- Produces: `PlayerShooter.Configure(InputActionAsset, GameObject)`, `PlayerShooter.SetStats(PlayerStats)` — consumed by this task's own scene wiring only.

- [ ] **Step 1: Write `PlayerShooter.cs`**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private InputActionAsset controlsAsset;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireInterval = 0.1f;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private PlayerStats stats;

    private InputAction _fire;
    private float _cooldown;

    public void Configure(InputActionAsset controls, GameObject prefab)
    {
        controlsAsset = controls;
        bulletPrefab = prefab;
    }

    public void SetStats(PlayerStats playerStats)
    {
        stats = playerStats;
    }

    private void Awake()
    {
        var map = controlsAsset.FindActionMap("Gameplay");
        _fire = map.FindAction("Fire");
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
        if (_cooldown > 0f) _cooldown -= Time.deltaTime;

        if (_fire.IsPressed() && _cooldown <= 0f && BulletPoolManager.Instance != null)
        {
            BulletPoolManager.Instance.Spawn(bulletPrefab, transform.position, Vector2.up * bulletSpeed);
            _cooldown = fireInterval;
        }
    }
}
```

Write to `E:\Astral Maidens\Assets\Scripts\Player\PlayerShooter.cs`.

- [ ] **Step 2: Refresh and verify compile**

`Unity_RunCommand` with `AssetDatabase.Refresh()`, then `Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty.

- [ ] **Step 3: Create the player bullet prefab and attach `PlayerShooter` to the Player**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var iceSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/IceMagic/frame_01.png");

        var bulletObj = new GameObject("PlayerBullet", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(Bullet));
        var sr = bulletObj.GetComponent<SpriteRenderer>();
        sr.sprite = iceSprite;
        sr.color = new Color(0.85f, 0.95f, 1f, 1f);
        var rb = bulletObj.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var col = bulletObj.GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.12f;
        var bulletComp = bulletObj.GetComponent<Bullet>();
        bulletComp.SetOwner(BulletOwner.Player);
        bulletComp.SetDamage(1);

        Directory.CreateDirectory("Assets/Prefabs/Bullets");
        var prefab = PrefabUtility.SaveAsPrefabAsset(bulletObj, "Assets/Prefabs/Bullets/PlayerBullet.prefab");
        Object.DestroyImmediate(bulletObj);
        result.RegisterObjectCreation(prefab);

        var player = GameObject.Find("Player");
        var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/PlayerControls.inputactions");
        var shooter = player.AddComponent<PlayerShooter>();
        shooter.Configure(controls, prefab);
        shooter.SetStats(player.GetComponent<PlayerStats>());

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        result.Log("PlayerBullet prefab created and PlayerShooter wired");
    }
}
```

- [ ] **Step 4: Verify wiring and no errors**

`Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty. `Unity_ManageGameObject get_component` on `Player`, `component_name: "PlayerShooter"`, `include_non_public_serialized: true` — expect `controlsAsset`, `bulletPrefab`, `stats` all non-null.

- [ ] **Step 5: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Scripts/Player/PlayerShooter.cs Assets/Prefabs Assets/Scenes && git commit -m "Add PlayerShooter and PlayerBullet prefab" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 3: Slow Status Effect + Player Controller Integration

**Files:**
- Create: `Assets/Scripts/Player/IPlayerStatus.cs`
- Create: `Assets/Scripts/Player/SlowStatus.cs`
- Create: `Assets/Scripts/Player/PlayerStatusController.cs`
- Modify: `Assets/Scripts/Player/PlayerController.cs`

**Interfaces:**
- Produces: `PlayerStatusController.ApplySlow(float multiplier, float duration)`, `PlayerStatusController.CurrentSpeedMultiplier` (float, get) — consumed by Task 6 (`BossController`'s slow-reapply coroutine).
- Produces: `PlayerController.SetStatusController(PlayerStatusController)`, `PlayerController.SetStats(PlayerStats)` — consumed by this task's own scene wiring.

- [ ] **Step 1: Write `IPlayerStatus.cs`**

```csharp
public interface IPlayerStatus
{
    void Tick(float deltaTime);
    bool IsExpired { get; }
}
```

- [ ] **Step 2: Write `SlowStatus.cs`**

```csharp
public class SlowStatus : IPlayerStatus
{
    public float Multiplier { get; }

    private float _remaining;

    public SlowStatus(float multiplier, float duration)
    {
        Multiplier = multiplier;
        _remaining = duration;
    }

    public void Tick(float deltaTime)
    {
        _remaining -= deltaTime;
    }

    public bool IsExpired => _remaining <= 0f;
}
```

- [ ] **Step 3: Write `PlayerStatusController.cs`**

```csharp
using UnityEngine;

public class PlayerStatusController : MonoBehaviour
{
    private SlowStatus _activeSlow;

    public float CurrentSpeedMultiplier =>
        (_activeSlow != null && !_activeSlow.IsExpired) ? _activeSlow.Multiplier : 1f;

    public void ApplySlow(float multiplier, float duration)
    {
        _activeSlow = new SlowStatus(multiplier, duration);
    }

    private void Update()
    {
        if (_activeSlow == null) return;
        _activeSlow.Tick(Time.deltaTime);
        if (_activeSlow.IsExpired) _activeSlow = null;
    }
}
```

Write the three files to `E:\Astral Maidens\Assets\Scripts\Player\`.

- [ ] **Step 4: Modify `PlayerController.cs`**

Replace the full file content:

```csharp
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
```

- [ ] **Step 5: Refresh and verify compile**

`Unity_RunCommand` with `AssetDatabase.Refresh()`, then `Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty.

- [ ] **Step 6: Attach `PlayerStatusController` to Player and wire it**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var player = GameObject.Find("Player");
        var statusController = player.AddComponent<PlayerStatusController>();
        var controller = player.GetComponent<PlayerController>();
        controller.SetStatusController(statusController);
        controller.SetStats(player.GetComponent<PlayerStats>());
        result.RegisterObjectModification(player);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        result.Log("PlayerStatusController attached and wired");
    }
}
```

- [ ] **Step 7: Verify wiring and no errors**

`Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty. `Unity_ManageGameObject get_component` on `Player`, `component_name: "PlayerController"`, `include_non_public_serialized: true` — expect `statusController` and `stats` both non-null.

- [ ] **Step 8: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Scripts/Player Assets/Scenes && git commit -m "Add slow status effect and wire it + game-over stop into PlayerController" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 4: BossPhaseSO

**Files:**
- Create: `Assets/Scripts/Bosses/BossPhaseSO.cs`

**Interfaces:**
- Consumes: `BulletPatternSO` (Phase 1).
- Produces: `BossPhaseSO` public fields (`phaseName, hp, isSpellCard, timeLimitSeconds, patterns, patternStartDelays, appliesSlow, slowMultiplier, slowDuration, slowReapplyInterval`) — consumed by Task 5 (`BossPhaseLogic`), Task 6 (`BossController`), Task 10 (phase asset creation).

- [ ] **Step 1: Write `BossPhaseSO.cs`**

```csharp
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
```

Write to `E:\Astral Maidens\Assets\Scripts\Bosses\BossPhaseSO.cs`.

- [ ] **Step 2: Refresh and verify compile**

`Unity_RunCommand` with `AssetDatabase.Refresh()`, then `Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty.

- [ ] **Step 3: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Scripts/Bosses && git commit -m "Add BossPhaseSO data asset" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 5: Boss Phase Transition Logic (TDD)

**Files:**
- Create: `Assets/Tests/Editor/BossPhaseLogicTests.cs`
- Create: `Assets/Scripts/Bosses/BossPhaseLogic.cs`

**Interfaces:**
- Consumes: `BossPhaseSO` (Task 4).
- Produces: `static bool BossPhaseLogic.ShouldAdvancePhase(BossPhaseSO phase, int currentHp, float elapsedSeconds)` — consumed by Task 6 (`BossController`).

- [ ] **Step 1: Write the failing test file first**

```csharp
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
```

Write to `E:\Astral Maidens\Assets\Tests\Editor\BossPhaseLogicTests.cs`.

- [ ] **Step 2: Confirm it fails to compile (BossPhaseLogic doesn't exist yet)**

`Unity_RunCommand` with `AssetDatabase.Refresh()`, then `Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: an error referencing `BossPhaseLogic` could not be found, from `BossPhaseLogicTests.cs`.

- [ ] **Step 3: Write the minimal implementation**

```csharp
public static class BossPhaseLogic
{
    public static bool ShouldAdvancePhase(BossPhaseSO phase, int currentHp, float elapsedSeconds)
    {
        if (currentHp <= 0) return true;
        if (phase.isSpellCard && phase.timeLimitSeconds > 0f && elapsedSeconds >= phase.timeLimitSeconds) return true;
        return false;
    }
}
```

Write to `E:\Astral Maidens\Assets\Scripts\Bosses\BossPhaseLogic.cs`.

- [ ] **Step 4: Refresh and verify compile succeeds**

`Unity_RunCommand` with `AssetDatabase.Refresh()`, then `Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty.

- [ ] **Step 5: Execute the assertions live and confirm PASS**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        int failures = 0;

        void Check(string label, bool condition)
        {
            if (condition) result.Log("PASS: {0}", label);
            else { result.LogError("FAIL: " + label); failures++; }
        }

        var normal = ScriptableObject.CreateInstance<BossPhaseSO>();
        normal.isSpellCard = false;
        normal.timeLimitSeconds = 0f;
        Check("Normal advances at HP 0", BossPhaseLogic.ShouldAdvancePhase(normal, 0, 5f));
        Check("Normal does not advance with HP left", !BossPhaseLogic.ShouldAdvancePhase(normal, 5, 999f));

        var spell = ScriptableObject.CreateInstance<BossPhaseSO>();
        spell.isSpellCard = true;
        spell.timeLimitSeconds = 30f;
        Check("Spellcard advances on time-over", BossPhaseLogic.ShouldAdvancePhase(spell, 10, 30f));
        Check("Spellcard advances on HP 0 before time-over", BossPhaseLogic.ShouldAdvancePhase(spell, 0, 5f));
        Check("Spellcard does not advance mid-cast with HP left", !BossPhaseLogic.ShouldAdvancePhase(spell, 10, 10f));

        result.Log(failures == 0 ? "ALL CHECKS PASSED" : failures + " CHECK(S) FAILED");
    }
}
```

Expected final log line: `ALL CHECKS PASSED`.

- [ ] **Step 6: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Tests Assets/Scripts/Bosses/BossPhaseLogic.cs && git commit -m "Add BossPhaseLogic with EditMode tests for phase transition rules" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 6: BossController

**Files:**
- Create: `Assets/Scripts/Bosses/BossController.cs`

**Interfaces:**
- Consumes: `BossPhaseLogic.ShouldAdvancePhase` (Task 5); `BossPhaseSO` (Task 4); `PatternExecutor.SetPlayer`, `PatternExecutor.SetPattern` (Phase 1, unchanged); `PlayerStatusController.ApplySlow` (Task 3).
- Produces: `BossController.SetPlayer(Transform)`, `BossController.SetPlayerStatus(PlayerStatusController)`, `BossController.SetPhases(BossPhaseSO[])`, `BossController.TakeDamage(int)`, events `OnPhaseHpChanged(int current, int max)`, `OnPhaseChanged(string name, bool isSpellCard)`, `OnTimerChanged(float remaining, float total)`, `OnBossDefeated()` — consumed by Task 7 (`BossHitbox`), Task 10 (scene wiring), Task 12 (`BossHudController`).

Note: `OnTimerChanged` carries `(remaining, total)` rather than just `remaining` (a one-parameter refinement over the design spec's literal signature) so the HUD can compute a fill ratio without separately tracking the active phase's time limit.

- [ ] **Step 1: Write `BossController.cs`**

```csharp
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
```

Write to `E:\Astral Maidens\Assets\Scripts\Bosses\BossController.cs`.

- [ ] **Step 2: Refresh and verify compile**

`Unity_RunCommand` with `AssetDatabase.Refresh()`, then `Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty.

- [ ] **Step 3: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Scripts/Bosses/BossController.cs && git commit -m "Add BossController phase/spellcard state machine" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 7: BossHitbox

**Files:**
- Create: `Assets/Scripts/Bosses/BossHitbox.cs`

**Interfaces:**
- Consumes: `Bullet.Owner`, `Bullet.Damage` (Task 1); `BossController.TakeDamage(int)` (Task 6).
- Produces: `BossHitbox.SetBoss(BossController)` — consumed by Task 10 (scene wiring).

- [ ] **Step 1: Write `BossHitbox.cs`**

```csharp
using UnityEngine;

public class BossHitbox : MonoBehaviour
{
    [SerializeField] private BossController boss;

    public void SetBoss(BossController bossController)
    {
        boss = bossController;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        if (bullet == null || boss == null) return;
        if (bullet.Owner != BulletOwner.Player) return;

        boss.TakeDamage(bullet.Damage);
    }
}
```

Write to `E:\Astral Maidens\Assets\Scripts\Bosses\BossHitbox.cs`.

- [ ] **Step 2: Refresh and verify compile**

`Unity_RunCommand` with `AssetDatabase.Refresh()`, then `Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty.

- [ ] **Step 3: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Scripts/Bosses/BossHitbox.cs && git commit -m "Add BossHitbox for player-bullet damage detection" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 8: Boss and Bullet Effect Asset Import

**Files:**
- Copies source PNGs into: `Assets/Art/Bosses/IceCrystalMaiden/`, `Assets/Art/Effects/IceMagic/`

**Interfaces:**
- Produces: `Assets/Art/Bosses/IceCrystalMaiden/frame_01.png` (Sprite, PPU 130), `Assets/Art/Effects/IceMagic/frame_03.png`, `frame_05.png`, `frame_09.png` (Sprite, PPU 100, matching the existing frame_01/frame_04 already imported in Phase 1) — consumed by Task 9 (boss bullet prefabs) and Task 10 (boss sprite, scene wiring).

- [ ] **Step 1: Copy source files into the Unity project**

```bash
mkdir -p "/e/Astral Maidens/Assets/Art/Bosses/IceCrystalMaiden"

cp "/c/Users/apple/Downloads/AstralMaidens/bosses/ice_crystal_maiden/frames_fixed/frame_01.png" "/e/Astral Maidens/Assets/Art/Bosses/IceCrystalMaiden/"

cp "/c/Users/apple/Downloads/AstralMaidens/effects/ice_magic/frames_fixed/frame_03.png" "/c/Users/apple/Downloads/AstralMaidens/effects/ice_magic/frames_fixed/frame_05.png" "/c/Users/apple/Downloads/AstralMaidens/effects/ice_magic/frames_fixed/frame_09.png" "/e/Astral Maidens/Assets/Art/Effects/IceMagic/"
```

- [ ] **Step 2: Import and configure the textures**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    private void ConfigureSprite(string path, float ppu)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.spritePixelsPerUnit = ppu;
        importer.spritePivot = new Vector2(0.5f, 0.5f);
        importer.SaveAndReimport();
    }

    public void Execute(ExecutionResult result)
    {
        AssetDatabase.Refresh();

        ConfigureSprite("Assets/Art/Bosses/IceCrystalMaiden/frame_01.png", 130f);
        ConfigureSprite("Assets/Art/Effects/IceMagic/frame_03.png", 100f);
        ConfigureSprite("Assets/Art/Effects/IceMagic/frame_05.png", 100f);
        ConfigureSprite("Assets/Art/Effects/IceMagic/frame_09.png", 100f);

        result.Log("Configured 4 sprites (boss + 3 effect frames)");
    }
}
```

- [ ] **Step 3: Verify no import errors**

`Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty.

- [ ] **Step 4: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Art/Bosses Assets/Art/Effects && git commit -m "Import Ice Crystal Maiden sprite and additional ice_magic effect frames" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 9: Boss Bullet Prefabs + Pattern Assets

**Files:**
- Create (via RunCommand): `Assets/Prefabs/Bullets/IceShard.prefab`, `CrystalMoon.prefab`, `IceNeedle.prefab`
- Create (via RunCommand): `Assets/Data/Bosses/IceCrystalMaiden_Radial24.asset`, `IceCrystalMaiden_Targeted5.asset`, `IceCrystalMaiden_Spiral8.asset`, `IceCrystalMaiden_Radial32.asset`

**Interfaces:**
- Consumes: `Assets/Art/Effects/IceMagic/frame_03.png`, `frame_05.png`, `frame_09.png` (Task 8); `Bullet` (Task 1); `BulletPatternSO`, `PatternType` (Phase 1).
- Produces: 4 `BulletPatternSO` assets at the paths above — consumed by Task 10 (`BossPhaseSO` assets).

- [ ] **Step 1: Create the 3 boss bullet prefabs and 4 pattern assets**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    private GameObject MakeBulletPrefab(string name, Sprite sprite, string path)
    {
        var go = new GameObject(name, typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(Bullet));
        go.GetComponent<SpriteRenderer>().sprite = sprite;
        var rb = go.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var col = go.GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.15f;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    public void Execute(ExecutionResult result)
    {
        var iceShardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/IceMagic/frame_09.png");
        var crystalMoonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/IceMagic/frame_03.png");
        var iceNeedleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/IceMagic/frame_05.png");

        var iceShardPrefab = MakeBulletPrefab("IceShard", iceShardSprite, "Assets/Prefabs/Bullets/IceShard.prefab");
        var crystalMoonPrefab = MakeBulletPrefab("CrystalMoon", crystalMoonSprite, "Assets/Prefabs/Bullets/CrystalMoon.prefab");
        var iceNeedlePrefab = MakeBulletPrefab("IceNeedle", iceNeedleSprite, "Assets/Prefabs/Bullets/IceNeedle.prefab");

        Directory.CreateDirectory("Assets/Data/Bosses");

        var radial24 = ScriptableObject.CreateInstance<BulletPatternSO>();
        radial24.type = PatternType.Radial;
        radial24.bulletPrefab = iceShardPrefab;
        radial24.n = 24;
        radial24.speed = 3f;
        radial24.interval = 1.2f;
        AssetDatabase.CreateAsset(radial24, "Assets/Data/Bosses/IceCrystalMaiden_Radial24.asset");

        var targeted5 = ScriptableObject.CreateInstance<BulletPatternSO>();
        targeted5.type = PatternType.Targeted;
        targeted5.bulletPrefab = iceShardPrefab;
        targeted5.k = 5;
        targeted5.targetedSpreadDeg = 10f;
        targeted5.speed = 3f;
        targeted5.interval = 1.2f;
        AssetDatabase.CreateAsset(targeted5, "Assets/Data/Bosses/IceCrystalMaiden_Targeted5.asset");

        var spiral8 = ScriptableObject.CreateInstance<BulletPatternSO>();
        spiral8.type = PatternType.Spiral;
        spiral8.bulletPrefab = crystalMoonPrefab;
        spiral8.n = 8;
        spiral8.deltaThetaDeg = 6f;
        spiral8.speed = 3f;
        spiral8.interval = 0.05f;
        AssetDatabase.CreateAsset(spiral8, "Assets/Data/Bosses/IceCrystalMaiden_Spiral8.asset");

        var radial32 = ScriptableObject.CreateInstance<BulletPatternSO>();
        radial32.type = PatternType.Radial;
        radial32.bulletPrefab = iceNeedlePrefab;
        radial32.n = 32;
        radial32.speed = 3f;
        radial32.interval = 4f;
        AssetDatabase.CreateAsset(radial32, "Assets/Data/Bosses/IceCrystalMaiden_Radial32.asset");

        AssetDatabase.SaveAssets();
        result.Log("Created 3 boss bullet prefabs and 4 pattern assets");
    }
}
```

- [ ] **Step 2: Verify assets exist and no errors**

`Unity_ManageAsset` `Action: "Search"`, `Path: "Assets/Data/Bosses"`, `SearchPattern: "t:BulletPatternSO"` — expect 4 results. `Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty.

- [ ] **Step 3: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Prefabs/Bullets Assets/Data/Bosses && git commit -m "Add boss bullet prefabs and Ice Crystal Maiden pattern assets" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 10: Boss Phase Assets + Scene Wiring

**Files:**
- Create (via RunCommand): `Assets/Data/Bosses/IceCrystalMaiden_Phase1.asset`, `IceCrystalMaiden_Phase2.asset`

**Interfaces:**
- Consumes: 4 `BulletPatternSO` assets (Task 9); `Assets/Art/Bosses/IceCrystalMaiden/frame_01.png` (Task 8); `BossController`, `BossHitbox` (Task 6/7); `BossController.SetPlayer/SetPlayerStatus/SetPhases`, `BossHitbox.SetBoss`.
- Produces: scene object `Enemies/Boss` — consumed by Task 12 (`BossHudController` wiring) and Task 13 (manual verification).

- [ ] **Step 1: Create the two `BossPhaseSO` assets and the `Boss` GameObject**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var radial24 = AssetDatabase.LoadAssetAtPath<BulletPatternSO>("Assets/Data/Bosses/IceCrystalMaiden_Radial24.asset");
        var targeted5 = AssetDatabase.LoadAssetAtPath<BulletPatternSO>("Assets/Data/Bosses/IceCrystalMaiden_Targeted5.asset");
        var spiral8 = AssetDatabase.LoadAssetAtPath<BulletPatternSO>("Assets/Data/Bosses/IceCrystalMaiden_Spiral8.asset");
        var radial32 = AssetDatabase.LoadAssetAtPath<BulletPatternSO>("Assets/Data/Bosses/IceCrystalMaiden_Radial32.asset");

        var phase1 = ScriptableObject.CreateInstance<BossPhaseSO>();
        phase1.phaseName = "Normal";
        phase1.hp = 30;
        phase1.isSpellCard = false;
        phase1.timeLimitSeconds = 0f;
        phase1.patterns = new[] { radial24, targeted5 };
        phase1.patternStartDelays = new[] { 0f, 1.2f };
        phase1.appliesSlow = false;
        AssetDatabase.CreateAsset(phase1, "Assets/Data/Bosses/IceCrystalMaiden_Phase1.asset");

        var phase2 = ScriptableObject.CreateInstance<BossPhaseSO>();
        phase2.phaseName = "Perpetual Frost";
        phase2.hp = 50;
        phase2.isSpellCard = true;
        phase2.timeLimitSeconds = 30f;
        phase2.patterns = new[] { spiral8, radial32 };
        phase2.patternStartDelays = new[] { 0f, 0f };
        phase2.appliesSlow = true;
        phase2.slowMultiplier = 0.4f;
        phase2.slowDuration = 8f;
        phase2.slowReapplyInterval = 4f;
        AssetDatabase.CreateAsset(phase2, "Assets/Data/Bosses/IceCrystalMaiden_Phase2.asset");

        AssetDatabase.SaveAssets();

        var bossSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Bosses/IceCrystalMaiden/frame_01.png");
        var enemies = GameObject.Find("Enemies");

        var bossObj = new GameObject("Boss", typeof(SpriteRenderer), typeof(BossController));
        bossObj.transform.SetParent(enemies.transform, false);
        bossObj.transform.position = new Vector3(0f, 6f, 0f);
        bossObj.GetComponent<SpriteRenderer>().sprite = bossSprite;
        result.RegisterObjectCreation(bossObj);

        var hitboxObj = new GameObject("BossHitbox", typeof(CircleCollider2D), typeof(BossHitbox));
        hitboxObj.transform.SetParent(bossObj.transform, false);
        var col = hitboxObj.GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.0f;
        result.RegisterObjectCreation(hitboxObj);

        var player = GameObject.Find("Player");
        var bossController = bossObj.GetComponent<BossController>();
        bossController.SetPlayer(player.transform);
        bossController.SetPlayerStatus(player.GetComponent<PlayerStatusController>());
        bossController.SetPhases(new[] { phase1, phase2 });

        hitboxObj.GetComponent<BossHitbox>().SetBoss(bossController);

        var testEmitter = GameObject.Find("TestEmitter");
        if (testEmitter != null) testEmitter.SetActive(false);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        result.Log("Boss phases created and Boss GameObject wired; TestEmitter disabled");
    }
}
```

- [ ] **Step 2: Verify wiring and no errors**

`Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty. `Unity_ManageGameObject get_component` on `Boss`, `component_name: "BossController"`, `include_non_public_serialized: true` — expect `phases` (2 entries), `playerTransform`, `playerStatus` all non-null/non-empty. Same check on `Boss/BossHitbox`, `component_name: "BossHitbox"` — expect `boss` non-null. `Unity_ManageScene GetHierarchy Depth:0` — confirm `TestEmitter` shows `activeSelf: false` and `Boss` exists under `Enemies`.

- [ ] **Step 3: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Data/Bosses Assets/Scenes && git commit -m "Add Ice Crystal Maiden phase assets, wire Boss into scene, disable TestEmitter" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 11: Boss HP Bar / Timer UI Asset Import

**Files:**
- Copies source PNGs into: `Assets/Art/UI/`

**Interfaces:**
- Produces: `Assets/Art/UI/bossbar_simple_filled_red.png`, `Assets/Art/UI/timerbar_small_blue.png` (Sprite, PPU 100 default, Point filter) — consumed by Task 12.

- [ ] **Step 1: Copy source files**

```bash
cp "/c/Users/apple/Downloads/AstralMaidens/ui/02_Boss_HPBar_Timer/bossbar_simple_filled_red.png" "/c/Users/apple/Downloads/AstralMaidens/ui/02_Boss_HPBar_Timer/timerbar_small_blue.png" "/e/Astral Maidens/Assets/Art/UI/"
```

- [ ] **Step 2: Import and configure**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        AssetDatabase.Refresh();

        string[] paths =
        {
            "Assets/Art/UI/bossbar_simple_filled_red.png",
            "Assets/Art/UI/timerbar_small_blue.png"
        };

        foreach (var path in paths)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        result.Log("Configured 2 boss HUD sprites");
    }
}
```

- [ ] **Step 3: Verify no errors**

`Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty.

- [ ] **Step 4: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Art/UI && git commit -m "Import boss HP bar and timer bar sprites" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 12: BossHudController

**Files:**
- Create: `Assets/Scripts/UI/BossHudController.cs`

**Interfaces:**
- Consumes: `BossController` events (Task 6); `Assets/Art/UI/bossbar_simple_filled_red.png`, `timerbar_small_blue.png` (Task 11).
- Produces: nothing consumed by later tasks (leaf; final visible feature).

- [ ] **Step 1: Write `BossHudController.cs`**

```csharp
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
```

Write to `E:\Astral Maidens\Assets\Scripts\UI\BossHudController.cs`.

- [ ] **Step 2: Refresh and verify compile**

`Unity_RunCommand` with `AssetDatabase.Refresh()`, then `Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty.

- [ ] **Step 3: Build the boss HUD elements and wire them**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var hpSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/bossbar_simple_filled_red.png");
        var timerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/timerbar_small_blue.png");
        var canvas = GameObject.Find("UICanvas").transform;

        var barObj = new GameObject("BossHpBar", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        barObj.transform.SetParent(canvas, false);
        var barRect = barObj.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.05f, 0.9f);
        barRect.anchorMax = new Vector2(0.55f, 0.98f);
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = Vector2.zero;
        var barImage = barObj.GetComponent<UnityEngine.UI.Image>();
        barImage.sprite = hpSprite;
        barImage.type = UnityEngine.UI.Image.Type.Filled;
        barImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        barImage.fillAmount = 1f;
        result.RegisterObjectCreation(barObj);

        var timerObj = new GameObject("BossTimerBar", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        timerObj.transform.SetParent(canvas, false);
        var timerRect = timerObj.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.05f, 0.85f);
        timerRect.anchorMax = new Vector2(0.3f, 0.89f);
        timerRect.offsetMin = Vector2.zero;
        timerRect.offsetMax = Vector2.zero;
        var timerImage = timerObj.GetComponent<UnityEngine.UI.Image>();
        timerImage.sprite = timerSprite;
        timerImage.type = UnityEngine.UI.Image.Type.Filled;
        timerImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        timerImage.fillAmount = 1f;
        timerObj.SetActive(false);
        result.RegisterObjectCreation(timerObj);

        var boss = GameObject.Find("Boss").GetComponent<BossController>();
        var hudObj = new GameObject("BossHudController", typeof(BossHudController));
        hudObj.transform.SetParent(canvas, false);
        var bossHud = hudObj.GetComponent<BossHudController>();
        bossHud.Configure(boss, barImage, timerImage, timerObj);
        result.RegisterObjectCreation(hudObj);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        result.Log("Boss HUD wired to BossController: {0}", boss);
    }
}
```

- [ ] **Step 4: Verify wiring and no errors**

`Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty. `Unity_ManageGameObject get_component` on `UICanvas/BossHudController`, `component_name: "BossHudController"`, `include_non_public_serialized: true` — expect `boss`, `hpBarFill`, `timerBarFill`, `timerContainer` all non-null.

- [ ] **Step 5: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Scripts/UI/BossHudController.cs Assets/Scenes && git commit -m "Add BossHudController and wire HP/timer bars" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 13: Final Integration Pass

**Files:** none (verification only).

- [ ] **Step 1: Full Play Mode smoke test**

Call `mcp__unity-mcp__Unity_ManageEditor` `Action: "Play"`. Immediately call `Unity_RunCommand` with `Application.runInBackground = true;` (Phase 1 established: without this, the Editor's Play loop nearly freezes when the window lacks OS focus during automated sessions). Then verify in sequence:

1. `Unity_GetConsoleLogs` (`logTypes: "Error"`) — empty at start.

2. **Player shooting hits the boss.** No input-injection tool exists (documented limitation since Phase 1 Task 8/11), so exercise the exact same code path `PlayerShooter` would (`BulletPoolManager.Instance.Spawn` with the `PlayerBullet` prefab) directly:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullets/PlayerBullet.prefab");
        var player = GameObject.Find("Player");
        BulletPoolManager.Instance.Spawn(prefab, player.transform.position, Vector2.up * 10f);
        result.Log("Spawned a PlayerBullet toward the boss");
    }
}
```

Wait ~1 second (real time, with `runInBackground` already on) for the bullet to travel from the player's position up to the boss, then check `Boss`'s `BossController` HP via `Unity_ManageGameObject get_component` (`include_non_public_serialized: true`) — `_currentHp` should read `29` (started at `30`, one hit for `1` damage). Confirms `PlayerBullet` → `BossHitbox` → `BossController.TakeDamage` end-to-end.

3. **Boss fires its Phase 1 patterns.** `Unity_ManageScene GetHierarchy Depth:-1` under `Boss` — confirm `PatternExecutor_0`/`PatternExecutor_1` child objects exist. Confirm `IceShard(Clone)` objects appear under `Systems` within a few seconds (Radial 24-way firing every 1.2s).

4. **Phase transition + slow gimmick.** Call `BossController.TakeDamage(30)` directly (deterministic, since `BossPhaseLogic`'s HP-zero rule is already unit-tested in Task 5 — no need to fire 30 individual bullets):

```csharp
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var boss = GameObject.Find("Boss").GetComponent<BossController>();
        boss.TakeDamage(30);
        result.Log("Forced phase 1 to clear via direct damage");
    }
}
```

Wait a frame or two, then `Unity_ManageGameObject get_component` on `Boss`/`BossController` — confirm the phase advanced (check `Unity_GetConsoleLogs` for no errors from the `Destroy`/re-create of `PatternExecutor_*`). Wait a few seconds for `BossController`'s `ReapplySlow` coroutine to run at least once, then query `Player`'s `PlayerStatusController` — `Unity_ManageGameObject get_component` won't show `CurrentSpeedMultiplier` (it's a property, not a serialized field), so instead query it via `Unity_RunCommand`:

```csharp
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var status = GameObject.Find("Player").GetComponent<PlayerStatusController>();
        result.Log("CurrentSpeedMultiplier={0}", status.CurrentSpeedMultiplier);
    }
}
```

Expected: `0.4`.

5. **Boss defeat.** Call `boss.TakeDamage(50)` (same pattern as step 4) to clear phase 2. `Unity_GetConsoleLogs` — expect a `Boss defeated` log entry. `Unity_ManageScene GetHierarchy` under `Boss` — expect no `PatternExecutor_*` children remaining.

6. **Player game-over stop.** Call `PlayerStats.TakeHit()` 3 times via `Unity_RunCommand`, waiting slightly over `invulnerabilitySeconds` (2s, via `PowerShell Start-Sleep`) between each call so the invulnerability gate doesn't block the next hit — `startingLife` is 3, so the third call should reach 0:

```csharp
using UnityEngine;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        GameObject.Find("Player").GetComponent<PlayerStats>().TakeHit();
        result.Log("TakeHit called");
    }
}
```

(Run three times with a `Start-Sleep -Seconds 3` between each.) After the third call, `Unity_ManageGameObject get_component` on `Player`'s `PlayerController` and `PlayerShooter` — both should report `enabled: false` (or confirm indirectly: no new `PlayerBullet(Clone)` objects appear even after another direct-spawn attempt is skipped — simplest direct check is that `Unity_GetConsoleLogs` shows no errors and a subsequent `TakeHit()` call is a silent no-op since `PlayerStats.OnGameOver` only fires once per threshold crossing in this design, which is expected, not a bug).

7. `Unity_GetConsoleLogs` (`logTypes: "Error"`) — empty throughout all of the above.

Call `mcp__unity-mcp__Unity_ManageEditor` `Action: "Stop"` when done. Check `git status --short` — if `ProjectSettings/VersionControlSettings.asset` drifted, reset it (Global Constraints).

- [ ] **Step 2: Fix any issues found**

If Step 1 surfaces a bug, fix the relevant script(s), re-run `AssetDatabase.Refresh()`, re-verify console is clean, and repeat Step 1's relevant checks until all pass.

- [ ] **Step 3: Final commit**

```bash
cd "/e/Astral Maidens" && git add -A && git status --short
```

If anything is unstaged (fixes from Step 2), commit it:

```bash
cd "/e/Astral Maidens" && git commit -m "Fix issues found in Stage 2 integration pass" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

Stage 2 is complete once all 13 tasks are committed and Step 1's checklist passes cleanly.
