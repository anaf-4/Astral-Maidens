# Phase 1 Foundation Systems Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the foundation layer of the "Astral Maidens" bullet-hell game — playfield/camera setup, pooled bullets, a data-driven 4-pattern shooting system, a working player controller, and a minimal HUD — validated by a pattern-math unit check and a manual Play Mode pass.

**Architecture:** Plain MonoBehaviour/coroutine gameplay code (no ECS/Job System, no asmdef) driven by `UnityEngine.Pool.ObjectPool<T>` for bullets and `ScriptableObject` data assets for bullet patterns. Unity Editor state (scenes, prefabs, imported textures, the Input Actions asset) is built via `mcp__unity-mcp__Unity_RunCommand` (compiles and runs C# directly inside the running Editor) rather than hand-authoring binary/GUID-bearing asset files.

**Tech Stack:** Unity 6000.3.23f1, URP 2D (already configured), New Input System 1.20.0, Unity Test Framework 1.6.0 (already in `Packages/manifest.json`), legacy `UnityEngine.UI` (no TextMeshPro — avoids an extra "Import TMP Essentials" step for a phase-1 placeholder HUD).

**Spec:** `E:\Astral Maidens\docs\superpowers\specs\2026-09-16-phase1-foundation-design.md`

## Global Constraints

- Project root: `E:\Astral Maidens` (git repo already initialized, baseline + spec committed).
- Sprite import: `Pixels Per Unit = 100`, `Filter Mode = Point (no filter)`, `Mipmap = disabled`, pivot center — applies to every sprite imported in this plan.
- Playfield logical bounds: world X `-6..6`, Y `-8..8` (12×16 units), centered at origin.
- Camera: Orthographic, `orthographicSize = 8`, `rect = (0, 0, 0.6, 1)` (left 60% of screen).
- No asmdef for gameplay scripts (`Assembly-CSharp` default). The one EditMode test file lives under an `Editor/` folder so it compiles into the default editor assembly and gets `nunit.framework` for free — no asmdef needed there either.
- No dedicated physics layers this phase — trigger filtering is done by `GetComponent<Bullet>()` in code (YAGNI; add layers in the Stage 2 boss plan if profiling needs it).
- Player character: `blue_magical_girl`. Input: new `PlayerControls.inputactions` (`Gameplay` map: Move/Fire/Focus/Bomb) — the project's default `InputSystem_Actions.inputactions` is left untouched.
- Standard verification pattern used throughout: after any filesystem script write, run `AssetDatabase.Refresh()` via `Unity_RunCommand`, then call `mcp__unity-mcp__Unity_GetConsoleLogs` with `logTypes: "Error"` and confirm it returns no entries referencing the new file.
- Every task ends with a git commit in `E:\Astral Maidens` (Bash tool; remember each Bash call resets cwd, so prefix with `cd "/e/Astral Maidens" &&`).

---

### Task 1: Playfield Bounds + Camera & Canvas Setup

**Files:**
- Create: `Assets/Scripts/Core/PlayfieldBounds.cs`

**Interfaces:**
- Produces: `PlayfieldBounds.MinX/MaxX/MinY/MaxY` (public `const float`), used by every task that clamps or bounds-checks positions.
- Produces (scene): `GameObject`s named `Systems` and `Enemies` at scene root, a `Canvas` named `UICanvas` with a child `Sidebar` (`Image`, anchored `(0.62,0)-(1,1)`).

- [ ] **Step 1: Write `PlayfieldBounds.cs`**

```csharp
public static class PlayfieldBounds
{
    public const float MinX = -6f;
    public const float MaxX = 6f;
    public const float MinY = -8f;
    public const float MaxY = 8f;
}
```

Use the Write tool to create this at `E:\Astral Maidens\Assets\Scripts\Core\PlayfieldBounds.cs`.

- [ ] **Step 2: Refresh and verify compile**

Call `mcp__unity-mcp__Unity_RunCommand` with:

```csharp
using UnityEngine;
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

Then call `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: no entries mentioning `PlayfieldBounds`.

- [ ] **Step 3: Configure camera, canvas, and scene organizer objects**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var camObj = GameObject.Find("Main Camera");
        var cam = camObj.GetComponent<Camera>();
        result.RegisterObjectModification(cam);
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.rect = new Rect(0f, 0f, 0.6f, 1f);

        var canvasObj = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        result.RegisterObjectCreation(canvasObj);
        var canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var sidebarObj = new GameObject("Sidebar", typeof(RectTransform), typeof(Image));
        sidebarObj.transform.SetParent(canvasObj.transform, false);
        result.RegisterObjectCreation(sidebarObj);
        var sidebarRect = sidebarObj.GetComponent<RectTransform>();
        sidebarRect.anchorMin = new Vector2(0.62f, 0f);
        sidebarRect.anchorMax = new Vector2(1f, 1f);
        sidebarRect.offsetMin = Vector2.zero;
        sidebarRect.offsetMax = Vector2.zero;
        sidebarObj.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.12f, 1f);

        result.RegisterObjectCreation(new GameObject("Systems"));
        result.RegisterObjectCreation(new GameObject("Enemies"));

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        result.Log("Camera/Canvas/organizer setup complete and scene saved");
    }
}
```

- [ ] **Step 4: Verify scene saved cleanly**

Call `mcp__unity-mcp__Unity_ManageScene` with `Action: "GetActive"`. Expected: `isDirty: false`.

- [ ] **Step 5: Commit**

```bash
cd "/e/Astral Maidens" && git add Assets/Scripts/Core/PlayfieldBounds.cs Assets/Scripts/Core/PlayfieldBounds.cs.meta Assets/Scenes/SampleScene.unity && git commit -m "Add playfield bounds, camera framing, and UI canvas" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

(If the `.meta` file wasn't generated yet at add-time, run `git add -A Assets/Scripts Assets/Scenes` instead.)

---

### Task 2: Asset Import (Player, Test Bullet, HUD sprites)

**Files:**
- Copies source PNGs into: `Assets/Art/Players/BlueMagicalGirl/`, `Assets/Art/Effects/IceMagic/`, `Assets/Art/UI/`

**Interfaces:**
- Produces: importable `Sprite` assets at
  - `Assets/Art/Players/BlueMagicalGirl/frame_01.png` … `frame_16.png`
  - `Assets/Art/Effects/IceMagic/frame_01.png`, `Assets/Art/Effects/IceMagic/frame_04.png`
  - `Assets/Art/UI/life_heart_blue.png`, `Assets/Art/UI/bomb_star_diamond_small.png`, `Assets/Art/UI/power_orb_framed.png`

- [ ] **Step 1: Copy source files into the Unity project**

```bash
mkdir -p "/e/Astral Maidens/Assets/Art/Players/BlueMagicalGirl" "/e/Astral Maidens/Assets/Art/Effects/IceMagic" "/e/Astral Maidens/Assets/Art/UI"

cp "/c/Users/apple/Downloads/AstralMaidens/players/blue_magical_girl/frames_fixed/"*.png "/e/Astral Maidens/Assets/Art/Players/BlueMagicalGirl/"

cp "/c/Users/apple/Downloads/AstralMaidens/effects/ice_magic/frames_fixed/frame_01.png" "/c/Users/apple/Downloads/AstralMaidens/effects/ice_magic/frames_fixed/frame_04.png" "/e/Astral Maidens/Assets/Art/Effects/IceMagic/"

cp "/c/Users/apple/Downloads/AstralMaidens/ui/01_HP_Bomb_Icons/life_heart_blue.png" "/c/Users/apple/Downloads/AstralMaidens/ui/01_HP_Bomb_Icons/bomb_star_diamond_small.png" "/e/Astral Maidens/Assets/Art/UI/"

cp "/c/Users/apple/Downloads/AstralMaidens/ui/03_Player_Stats/power_orb_framed.png" "/e/Astral Maidens/Assets/Art/UI/"
```

- [ ] **Step 2: Import and configure all copied textures as sprites**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        AssetDatabase.Refresh();

        string[] folders =
        {
            "Assets/Art/Players/BlueMagicalGirl",
            "Assets/Art/Effects/IceMagic",
            "Assets/Art/UI"
        };

        int total = 0;
        foreach (var folder in folders)
        {
            foreach (var file in Directory.GetFiles(folder, "*.png"))
            {
                string assetPath = file.Replace("\\", "/");
                var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.spritePixelsPerUnit = 100f;
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                importer.SaveAndReimport();
                total++;
            }
        }
        result.Log("Configured {0} sprite(s) across {1} folder(s)", total, folders.Length);
    }
}
```

Expected log: `Configured 21 sprite(s) across 3 folder(s)` (16 player + 2 effect + 3 UI).

- [ ] **Step 3: Verify no import errors**

Call `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: empty.

- [ ] **Step 4: Commit**

```bash
cd "/e/Astral Maidens" && git add Assets/Art && git commit -m "Import player, test-bullet, and HUD sprites with pixel-art settings" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 3: Object Pooling Core

**Files:**
- Create: `Assets/Scripts/Core/Pooling/IPoolable.cs`
- Create: `Assets/Scripts/Bullets/Bullet.cs`
- Create: `Assets/Scripts/Core/Pooling/BulletPoolManager.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks (pure new code).
- Produces:
  - `IPoolable.OnSpawn()`, `IPoolable.OnDespawn()`
  - `Bullet : MonoBehaviour, IPoolable` — `Vector2 Velocity { get; }`, `void Init(BulletPoolManager, GameObject prefab, Vector2 velocity)`, `void Despawn()`
  - `BulletPoolManager : MonoBehaviour` — `static BulletPoolManager Instance`, `Bullet Spawn(GameObject prefab, Vector2 position, Vector2 velocity)`, `void Despawn(GameObject prefab, Bullet bullet)`, `IReadOnlyCollection<Bullet> ActiveBullets`

- [ ] **Step 1: Write `IPoolable.cs`**

```csharp
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}
```

Write to `E:\Astral Maidens\Assets\Scripts\Core\Pooling\IPoolable.cs`.

- [ ] **Step 2: Write `Bullet.cs`**

```csharp
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Bullet : MonoBehaviour, IPoolable
{
    public Vector2 Velocity { get; private set; }

    private BulletPoolManager _manager;
    private GameObject _prefab;

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

Write to `E:\Astral Maidens\Assets\Scripts\Bullets\Bullet.cs`.

- [ ] **Step 3: Write `BulletPoolManager.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletPoolManager : MonoBehaviour
{
    public static BulletPoolManager Instance { get; private set; }

    private readonly Dictionary<GameObject, ObjectPool<Bullet>> _pools = new();
    private readonly HashSet<Bullet> _active = new();

    public IReadOnlyCollection<Bullet> ActiveBullets => _active;

    private void Awake()
    {
        Instance = this;
    }

    public Bullet Spawn(GameObject prefab, Vector2 position, Vector2 velocity)
    {
        if (!_pools.TryGetValue(prefab, out var pool))
        {
            pool = new ObjectPool<Bullet>(
                createFunc: () => CreateBullet(prefab),
                actionOnGet: b => b.gameObject.SetActive(true),
                actionOnRelease: b => b.gameObject.SetActive(false),
                actionOnDestroy: b => Destroy(b.gameObject),
                defaultCapacity: 64);
            _pools[prefab] = pool;
        }

        var bullet = pool.Get();
        bullet.transform.position = position;
        bullet.Init(this, prefab, velocity);
        bullet.OnSpawn();
        _active.Add(bullet);
        return bullet;
    }

    public void Despawn(GameObject prefab, Bullet bullet)
    {
        bullet.OnDespawn();
        _active.Remove(bullet);
        _pools[prefab].Release(bullet);
    }

    private Bullet CreateBullet(GameObject prefab)
    {
        var go = Instantiate(prefab, transform);
        return go.GetComponent<Bullet>();
    }
}
```

Write to `E:\Astral Maidens\Assets\Scripts\Core\Pooling\BulletPoolManager.cs`.

- [ ] **Step 4: Refresh, verify compile, attach to scene**

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
        AssetDatabase.Refresh();
        var systems = GameObject.Find("Systems");
        var pm = systems.AddComponent<BulletPoolManager>();
        result.RegisterObjectModification(systems);
        result.Log("Added BulletPoolManager to Systems: {0}", pm);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }
}
```

- [ ] **Step 5: Verify no compile/console errors**

Call `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: empty.

- [ ] **Step 6: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Scripts/Core Assets/Scripts/Bullets Assets/Scenes && git commit -m "Add IPoolable/Bullet/BulletPoolManager pooling core" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 4: Bullet Bounds Checker

**Files:**
- Create: `Assets/Scripts/Bullets/BulletBoundsChecker.cs`

**Interfaces:**
- Consumes: `BulletPoolManager.Instance.ActiveBullets`, `Bullet.Despawn()`, `PlayfieldBounds.MinX/MaxX/MinY/MaxY`.
- Produces: nothing consumed by later tasks (leaf component).

- [ ] **Step 1: Write `BulletBoundsChecker.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

public class BulletBoundsChecker : MonoBehaviour
{
    [SerializeField] private float marginUnits = 2f;

    private readonly List<Bullet> _toDespawn = new();

    private void Update()
    {
        if (Time.frameCount % 5 != 0) return;
        if (BulletPoolManager.Instance == null) return;

        _toDespawn.Clear();

        foreach (var bullet in BulletPoolManager.Instance.ActiveBullets)
        {
            var p = bullet.transform.position;
            if (p.x < PlayfieldBounds.MinX - marginUnits || p.x > PlayfieldBounds.MaxX + marginUnits ||
                p.y < PlayfieldBounds.MinY - marginUnits || p.y > PlayfieldBounds.MaxY + marginUnits)
            {
                _toDespawn.Add(bullet);
            }
        }

        foreach (var bullet in _toDespawn)
        {
            bullet.Despawn();
        }
    }
}
```

Write to `E:\Astral Maidens\Assets\Scripts\Bullets\BulletBoundsChecker.cs`.

- [ ] **Step 2: Refresh, verify compile, attach to scene**

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
        AssetDatabase.Refresh();
        var systems = GameObject.Find("Systems");
        var checker = systems.AddComponent<BulletBoundsChecker>();
        result.RegisterObjectModification(systems);
        result.Log("Added BulletBoundsChecker to Systems: {0}", checker);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }
}
```

- [ ] **Step 3: Verify no compile/console errors**

Call `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: empty.

- [ ] **Step 4: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Scripts/Bullets Assets/Scenes && git commit -m "Add BulletBoundsChecker for batched off-screen despawn" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 5: Pattern Math (TDD)

**Files:**
- Create: `Assets/Tests/Editor/PatternMathTests.cs`
- Create: `Assets/Scripts/Bullets/Patterns/PatternMath.cs`

**Interfaces:**
- Produces: `PatternMath.RadialAnglesDeg(int n, float baseAngleDeg)`, `PatternMath.SpiralAngleAtTick(float startAngleDeg, float deltaThetaDeg, int tick)`, `PatternMath.WayShotAnglesDeg(int k, float baseAngleDeg, float fanAngleDeg)`, `PatternMath.AngleToDirection(float angleDeg)` — all `static`, consumed by `PatternExecutor` in Task 6.

- [ ] **Step 1: Write the failing test file first**

```csharp
using NUnit.Framework;
using UnityEngine;

public class PatternMathTests
{
    [Test]
    public void RadialAnglesDeg_DividesEqually()
    {
        var angles = PatternMath.RadialAnglesDeg(4, 0f);
        Assert.AreEqual(4, angles.Length);
        Assert.AreEqual(0f, angles[0], 0.001f);
        Assert.AreEqual(90f, angles[1], 0.001f);
        Assert.AreEqual(180f, angles[2], 0.001f);
        Assert.AreEqual(270f, angles[3], 0.001f);
    }

    [Test]
    public void SpiralAngleAtTick_AccumulatesRotation()
    {
        Assert.AreEqual(0f, PatternMath.SpiralAngleAtTick(0f, 6f, 0), 0.001f);
        Assert.AreEqual(6f, PatternMath.SpiralAngleAtTick(0f, 6f, 1), 0.001f);
        Assert.AreEqual(60f, PatternMath.SpiralAngleAtTick(0f, 6f, 10), 0.001f);
    }

    [Test]
    public void WayShotAnglesDeg_SpreadsWithinFanAngle()
    {
        var angles = PatternMath.WayShotAnglesDeg(5, 90f, 60f);
        Assert.AreEqual(5, angles.Length);
        Assert.AreEqual(60f, angles[0], 0.001f);
        Assert.AreEqual(90f, angles[2], 0.001f);
        Assert.AreEqual(120f, angles[4], 0.001f);
    }

    [Test]
    public void WayShotAnglesDeg_SingleBulletUsesBaseAngle()
    {
        var angles = PatternMath.WayShotAnglesDeg(1, 45f, 60f);
        Assert.AreEqual(45f, angles[0], 0.001f);
    }

    [Test]
    public void AngleToDirection_ZeroDegreesPointsRight()
    {
        var dir = PatternMath.AngleToDirection(0f);
        Assert.AreEqual(1f, dir.x, 0.001f);
        Assert.AreEqual(0f, dir.y, 0.001f);
    }
}
```

Write to `E:\Astral Maidens\Assets\Tests\Editor\PatternMathTests.cs` (the `Editor` folder segment makes this compile into the default editor assembly, which has `nunit.framework` available with no asmdef needed).

- [ ] **Step 2: Confirm it fails to compile (PatternMath doesn't exist yet)**

Call `mcp__unity-mcp__Unity_RunCommand` with a trivial `AssetDatabase.Refresh()` body (same as Task 1 Step 2), then `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: an error referencing `PatternMath` could not be found, from `PatternMathTests.cs`.

- [ ] **Step 3: Write the minimal implementation**

```csharp
using UnityEngine;

public static class PatternMath
{
    public static float[] RadialAnglesDeg(int n, float baseAngleDeg)
    {
        var angles = new float[n];
        for (int i = 0; i < n; i++)
        {
            angles[i] = baseAngleDeg + i * (360f / n);
        }
        return angles;
    }

    public static float SpiralAngleAtTick(float startAngleDeg, float deltaThetaDeg, int tick)
    {
        return startAngleDeg + deltaThetaDeg * tick;
    }

    public static float[] WayShotAnglesDeg(int k, float baseAngleDeg, float fanAngleDeg)
    {
        var angles = new float[k];
        if (k == 1)
        {
            angles[0] = baseAngleDeg;
            return angles;
        }
        float step = fanAngleDeg / (k - 1);
        float start = baseAngleDeg - fanAngleDeg / 2f;
        for (int i = 0; i < k; i++)
        {
            angles[i] = start + step * i;
        }
        return angles;
    }

    public static Vector2 AngleToDirection(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
```

Write to `E:\Astral Maidens\Assets\Scripts\Bullets\Patterns\PatternMath.cs`.

- [ ] **Step 4: Refresh and verify compile succeeds**

Call `mcp__unity-mcp__Unity_RunCommand` with `AssetDatabase.Refresh()`, then `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: empty (no more "PatternMath not found").

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

        var radial = PatternMath.RadialAnglesDeg(4, 0f);
        Check("Radial 4-way angles", Mathf.Approximately(radial[0], 0f) && Mathf.Approximately(radial[1], 90f) && Mathf.Approximately(radial[2], 180f) && Mathf.Approximately(radial[3], 270f));

        Check("Spiral tick 0", Mathf.Approximately(PatternMath.SpiralAngleAtTick(0f, 6f, 0), 0f));
        Check("Spiral tick 10", Mathf.Approximately(PatternMath.SpiralAngleAtTick(0f, 6f, 10), 60f));

        var wayshot = PatternMath.WayShotAnglesDeg(5, 90f, 60f);
        Check("WayShot fan edges/center", Mathf.Approximately(wayshot[0], 60f) && Mathf.Approximately(wayshot[2], 90f) && Mathf.Approximately(wayshot[4], 120f));

        var single = PatternMath.WayShotAnglesDeg(1, 45f, 60f);
        Check("WayShot single uses base angle", Mathf.Approximately(single[0], 45f));

        var dir = PatternMath.AngleToDirection(0f);
        Check("AngleToDirection(0) points +X", Mathf.Approximately(dir.x, 1f) && Mathf.Approximately(dir.y, 0f));

        result.Log(failures == 0 ? "ALL CHECKS PASSED" : failures + " CHECK(S) FAILED");
    }
}
```

Expected final log line: `ALL CHECKS PASSED`.

- [ ] **Step 6: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Tests Assets/Scripts/Bullets/Patterns && git commit -m "Add PatternMath with EditMode tests for Radial/Spiral/WayShot math" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 6: BulletPatternSO + PatternExecutor

**Files:**
- Create: `Assets/Scripts/Bullets/Patterns/PatternType.cs`
- Create: `Assets/Scripts/Bullets/Patterns/BulletPatternSO.cs`
- Create: `Assets/Scripts/Bullets/Patterns/PatternExecutor.cs`

**Interfaces:**
- Consumes: `PatternMath.*` (Task 5), `BulletPoolManager.Instance.Spawn(...)` (Task 3).
- Produces: `BulletPatternSO` (public fields: `type, bulletPrefab, n, k, deltaThetaDeg, fanAngleDeg, baseAngleDeg, targetedSpreadDeg, speed, interval, aimAtPlayer`), `PatternExecutor.SetPattern(BulletPatternSO)`, `PatternExecutor.SetPlayer(Transform)` — both consumed by Task 9.

- [ ] **Step 1: Write `PatternType.cs`**

```csharp
public enum PatternType
{
    Radial,
    Spiral,
    Targeted,
    WayShot
}
```

Write to `E:\Astral Maidens\Assets\Scripts\Bullets\Patterns\PatternType.cs`.

- [ ] **Step 2: Write `BulletPatternSO.cs`**

```csharp
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
```

Write to `E:\Astral Maidens\Assets\Scripts\Bullets\Patterns\BulletPatternSO.cs`.

- [ ] **Step 3: Write `PatternExecutor.cs`**

```csharp
using System.Collections;
using UnityEngine;

public class PatternExecutor : MonoBehaviour
{
    [SerializeField] private BulletPatternSO pattern;
    [SerializeField] private Transform playerTransform;

    private int _tick;
    private Coroutine _running;

    public void SetPattern(BulletPatternSO newPattern)
    {
        pattern = newPattern;
        _tick = 0;
        if (isActiveAndEnabled)
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(FireLoop());
        }
    }

    public void SetPlayer(Transform player)
    {
        playerTransform = player;
    }

    private void OnEnable()
    {
        if (pattern != null) _running = StartCoroutine(FireLoop());
    }

    private void OnDisable()
    {
        if (_running != null) StopCoroutine(_running);
    }

    private IEnumerator FireLoop()
    {
        while (true)
        {
            Fire();
            yield return new WaitForSeconds(pattern.interval);
        }
    }

    private void Fire()
    {
        if (pattern == null || BulletPoolManager.Instance == null) return;

        float baseAngle = pattern.aimAtPlayer ? AngleToPlayerDeg() : pattern.baseAngleDeg;

        switch (pattern.type)
        {
            case PatternType.Radial:
                FireAtAngles(PatternMath.RadialAnglesDeg(pattern.n, baseAngle));
                break;
            case PatternType.Spiral:
                float armStep = 360f / pattern.n;
                var angles = new float[pattern.n];
                for (int i = 0; i < pattern.n; i++)
                {
                    angles[i] = PatternMath.SpiralAngleAtTick(baseAngle + i * armStep, pattern.deltaThetaDeg, _tick);
                }
                FireAtAngles(angles);
                _tick++;
                break;
            case PatternType.Targeted:
                FireAtAngles(PatternMath.WayShotAnglesDeg(pattern.k, AngleToPlayerDeg(), pattern.targetedSpreadDeg));
                break;
            case PatternType.WayShot:
                FireAtAngles(PatternMath.WayShotAnglesDeg(pattern.k, baseAngle, pattern.fanAngleDeg));
                break;
        }
    }

    private void FireAtAngles(float[] anglesDeg)
    {
        foreach (var angle in anglesDeg)
        {
            var dir = PatternMath.AngleToDirection(angle);
            BulletPoolManager.Instance.Spawn(pattern.bulletPrefab, transform.position, dir * pattern.speed);
        }
    }

    private float AngleToPlayerDeg()
    {
        if (playerTransform == null) return 270f;
        Vector2 diff = playerTransform.position - transform.position;
        return Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
    }
}
```

Write to `E:\Astral Maidens\Assets\Scripts\Bullets\Patterns\PatternExecutor.cs`.

- [ ] **Step 4: Refresh and verify compile**

Call `mcp__unity-mcp__Unity_RunCommand` with `AssetDatabase.Refresh()`, then `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: empty.

- [ ] **Step 5: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Scripts/Bullets/Patterns && git commit -m "Add BulletPatternSO data asset and PatternExecutor (Radial/Spiral/Targeted/WayShot)" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 7: Input Actions + PlayerController

**Files:**
- Create (via RunCommand, not filesystem): `Assets/Settings/PlayerControls.inputactions`
- Create: `Assets/Scripts/Player/PlayerController.cs`

**Interfaces:**
- Consumes: `PlayfieldBounds.MinX/MaxX/MinY/MaxY` (Task 1).
- Produces: `PlayerController.Configure(InputActionAsset controls, SpriteRenderer hitboxVisual)`, consumed by Task 8's scene-wiring step.

- [ ] **Step 1: Build the Input Actions asset via the Input System API**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = asset.AddActionMap("Gameplay");

        var move = map.AddAction("Move", InputActionType.Value, expectedControlType: "Vector2");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        move.AddBinding("<Gamepad>/leftStick");

        var fire = map.AddAction("Fire", InputActionType.Button);
        fire.AddBinding("<Keyboard>/z");
        fire.AddBinding("<Gamepad>/buttonSouth");

        var focus = map.AddAction("Focus", InputActionType.Button);
        focus.AddBinding("<Keyboard>/leftShift");
        focus.AddBinding("<Gamepad>/leftTrigger");

        var bomb = map.AddAction("Bomb", InputActionType.Button);
        bomb.AddBinding("<Keyboard>/x");
        bomb.AddBinding("<Gamepad>/buttonEast");

        AssetDatabase.CreateAsset(asset, "Assets/Settings/PlayerControls.inputactions");
        AssetDatabase.SaveAssets();
        result.RegisterObjectCreation(asset);
        result.Log("Created PlayerControls.inputactions with Gameplay map (Move/Fire/Focus/Bomb)");
    }
}
```

- [ ] **Step 2: Verify the asset was created**

Call `mcp__unity-mcp__Unity_ManageAsset` with `Action: "GetInfo"`, `Path: "Assets/Settings/PlayerControls.inputactions"`. Expected: `success: true`.

- [ ] **Step 3: Write `PlayerController.cs`**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputActionAsset controlsAsset;
    [SerializeField] private SpriteRenderer hitboxRenderer;
    [SerializeField] private float normalSpeed = 6f;
    [SerializeField] private float focusSpeed = 3f;

    private InputAction _move;
    private InputAction _focus;

    public void Configure(InputActionAsset controls, SpriteRenderer hitboxVisual)
    {
        controlsAsset = controls;
        hitboxRenderer = hitboxVisual;
    }

    private void Awake()
    {
        var map = controlsAsset.FindActionMap("Gameplay");
        _move = map.FindAction("Move");
        _focus = map.FindAction("Focus");
        map.Enable();
    }

    private void Update()
    {
        bool focusing = _focus.IsPressed();
        float speed = focusing ? focusSpeed : normalSpeed;
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

Write to `E:\Astral Maidens\Assets\Scripts\Player\PlayerController.cs`.

- [ ] **Step 4: Refresh and verify compile**

Call `mcp__unity-mcp__Unity_RunCommand` with `AssetDatabase.Refresh()`, then `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: empty.

- [ ] **Step 5: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Settings/PlayerControls.inputactions* Assets/Scripts/Player && git commit -m "Add PlayerControls input actions and PlayerController movement" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 8: PlayerStats + Hitbox + Player Scene Wiring

**Files:**
- Create: `Assets/Scripts/Player/PlayerStats.cs`
- Create: `Assets/Scripts/Player/PlayerHitboxTrigger.cs`

**Interfaces:**
- Consumes: `PlayerController.Configure(...)` (Task 7), `Bullet.Despawn()` (Task 3).
- Produces: `PlayerStats` events `OnLifeChanged(int)`, `OnSpellChanged(int)`, `OnPowerChanged(float)`, `OnGameOver`, method `TakeHit()` — consumed by Task 10 (HUD) and this task's own wiring. Scene object named `Player` (with child `Hitbox`) — its `transform` is consumed by Task 9 (`PatternExecutor.SetPlayer`).

- [ ] **Step 1: Write `PlayerStats.cs`**

```csharp
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
```

Write to `E:\Astral Maidens\Assets\Scripts\Player\PlayerStats.cs`.

- [ ] **Step 2: Write `PlayerHitboxTrigger.cs`**

```csharp
using UnityEngine;

public class PlayerHitboxTrigger : MonoBehaviour
{
    private PlayerStats _stats;

    public void SetStats(PlayerStats stats)
    {
        _stats = stats;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var bullet = other.GetComponent<Bullet>();
        if (bullet == null || _stats == null) return;

        _stats.TakeHit();
        bullet.Despawn();
    }
}
```

Write to `E:\Astral Maidens\Assets\Scripts\Player\PlayerHitboxTrigger.cs`.

- [ ] **Step 3: Refresh and verify compile**

Call `mcp__unity-mcp__Unity_RunCommand` with `AssetDatabase.Refresh()`, then `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: empty.

- [ ] **Step 4: Build the Player GameObject in the scene**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Players/BlueMagicalGirl/frame_01.png");
        var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/PlayerControls.inputactions");

        var playerObj = new GameObject("Player", typeof(SpriteRenderer));
        playerObj.transform.position = new Vector3(0f, -6f, 0f);
        playerObj.GetComponent<SpriteRenderer>().sprite = playerSprite;
        result.RegisterObjectCreation(playerObj);

        var hitboxObj = new GameObject("Hitbox", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(PlayerHitboxTrigger));
        hitboxObj.transform.SetParent(playerObj.transform, false);
        var hitboxRenderer = hitboxObj.GetComponent<SpriteRenderer>();
        hitboxRenderer.sprite = playerSprite;
        hitboxRenderer.color = new Color(1f, 1f, 1f, 0.6f);
        hitboxRenderer.transform.localScale = Vector3.one * 0.05f;
        hitboxRenderer.enabled = false;
        var hitboxCollider = hitboxObj.GetComponent<CircleCollider2D>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.radius = 0.08f;
        result.RegisterObjectCreation(hitboxObj);

        var stats = playerObj.AddComponent<PlayerStats>();
        var controller = playerObj.AddComponent<PlayerController>();
        controller.Configure(controls, hitboxRenderer);
        hitboxObj.GetComponent<PlayerHitboxTrigger>().SetStats(stats);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        result.Log("Player wired: {0}", playerObj);
    }
}
```

- [ ] **Step 5: Verify no console errors and Player exists**

Call `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty. Call `mcp__unity-mcp__Unity_ManageScene` `Action: "GetHierarchy"`, `Depth: 0` — expect a root object named `Player`.

- [ ] **Step 6: Manual Play Mode check**

Call `mcp__unity-mcp__Unity_ManageEditor` `Action: "Play"`. Confirm via `Unity_GetConsoleLogs` (`logTypes: "Error"`) that no runtime errors appear in the first few seconds (8-directional move, Focus-held hitbox visibility). Call `mcp__unity-mcp__Unity_ManageEditor` `Action: "Stop"` afterward.

- [ ] **Step 7: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Scripts/Player Assets/Scenes && git commit -m "Add PlayerStats/PlayerHitboxTrigger and wire the Player into the scene" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 9: Bullet Prefab + Pattern Test Emitter

**Files:**
- Create (via RunCommand): `Assets/Prefabs/Bullets/TestBullet.prefab`
- Create (via RunCommand): `Assets/Data/Patterns/RadialTest.asset`, `SpiralTest.asset`, `TargetedTest.asset`, `WayShotTest.asset`
- Create: `Assets/Scripts/Testing/PatternTestEmitter.cs`

**Interfaces:**
- Consumes: `Bullet` (Task 3), `BulletPatternSO`/`PatternExecutor` (Task 6), `Player` scene object's `transform` (Task 8).
- Produces: scene object `Enemies/TestEmitter` cycling all 4 patterns — consumed only by manual verification in Task 11.

- [ ] **Step 1: Write `PatternTestEmitter.cs`**

```csharp
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
```

Write to `E:\Astral Maidens\Assets\Scripts\Testing\PatternTestEmitter.cs`.

- [ ] **Step 2: Refresh and verify compile**

Call `mcp__unity-mcp__Unity_RunCommand` with `AssetDatabase.Refresh()`, then `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: empty.

- [ ] **Step 3: Create the bullet prefab, the 4 pattern assets, and the emitter**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        var bulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Effects/IceMagic/frame_04.png");

        var bulletObj = new GameObject("TestBullet", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(Bullet));
        bulletObj.GetComponent<SpriteRenderer>().sprite = bulletSprite;
        var rb = bulletObj.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        var col = bulletObj.GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.15f;

        Directory.CreateDirectory("Assets/Prefabs/Bullets");
        var prefab = PrefabUtility.SaveAsPrefabAsset(bulletObj, "Assets/Prefabs/Bullets/TestBullet.prefab");
        Object.DestroyImmediate(bulletObj);
        result.RegisterObjectCreation(prefab);

        Directory.CreateDirectory("Assets/Data/Patterns");

        var radial = ScriptableObject.CreateInstance<BulletPatternSO>();
        radial.type = PatternType.Radial;
        radial.bulletPrefab = prefab;
        radial.n = 16;
        radial.speed = 3f;
        radial.interval = 1.2f;
        AssetDatabase.CreateAsset(radial, "Assets/Data/Patterns/RadialTest.asset");

        var spiral = ScriptableObject.CreateInstance<BulletPatternSO>();
        spiral.type = PatternType.Spiral;
        spiral.bulletPrefab = prefab;
        spiral.n = 4;
        spiral.deltaThetaDeg = 6f;
        spiral.speed = 3f;
        spiral.interval = 0.05f;
        AssetDatabase.CreateAsset(spiral, "Assets/Data/Patterns/SpiralTest.asset");

        var targeted = ScriptableObject.CreateInstance<BulletPatternSO>();
        targeted.type = PatternType.Targeted;
        targeted.bulletPrefab = prefab;
        targeted.k = 3;
        targeted.targetedSpreadDeg = 10f;
        targeted.speed = 3f;
        targeted.interval = 1f;
        AssetDatabase.CreateAsset(targeted, "Assets/Data/Patterns/TargetedTest.asset");

        var wayshot = ScriptableObject.CreateInstance<BulletPatternSO>();
        wayshot.type = PatternType.WayShot;
        wayshot.bulletPrefab = prefab;
        wayshot.k = 5;
        wayshot.fanAngleDeg = 45f;
        wayshot.aimAtPlayer = true;
        wayshot.speed = 3f;
        wayshot.interval = 1f;
        AssetDatabase.CreateAsset(wayshot, "Assets/Data/Patterns/WayShotTest.asset");

        AssetDatabase.SaveAssets();

        var enemies = GameObject.Find("Enemies");
        var emitterObj = new GameObject("TestEmitter", typeof(PatternExecutor), typeof(PatternTestEmitter));
        emitterObj.transform.SetParent(enemies.transform, false);
        emitterObj.transform.position = new Vector3(0f, 6f, 0f);
        result.RegisterObjectCreation(emitterObj);

        var player = GameObject.Find("Player");
        emitterObj.GetComponent<PatternExecutor>().SetPlayer(player.transform);
        emitterObj.GetComponent<PatternTestEmitter>().SetPatterns(new[] { radial, spiral, targeted, wayshot });

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        result.Log("TestBullet prefab, 4 pattern assets, and TestEmitter created");
    }
}
```

- [ ] **Step 4: Verify assets exist and no console errors**

Call `mcp__unity-mcp__Unity_ManageAsset` `Action: "Search"`, `Path: "Assets/Data/Patterns"`, `SearchPattern: "*.asset"`. Expected: 4 results. Call `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty.

- [ ] **Step 5: Manual Play Mode check**

Call `mcp__unity-mcp__Unity_ManageEditor` `Action: "Play"`. After a few seconds call `mcp__unity-mcp__Unity_Camera_Capture` (or `Unity_SceneView_Capture2DScene`) to visually confirm bullets are being fired from `TestEmitter` toward the playfield. Call `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`) — expect empty. Call `mcp__unity-mcp__Unity_ManageEditor` `Action: "Stop"`.

- [ ] **Step 6: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Prefabs Assets/Data Assets/Scripts/Testing Assets/Scenes && git commit -m "Add TestBullet prefab, 4 pattern data assets, and PatternTestEmitter" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 10: HUD

**Files:**
- Create: `Assets/Scripts/UI/HudController.cs`

**Interfaces:**
- Consumes: `PlayerStats` events (Task 8).
- Produces: nothing consumed by later tasks (leaf; final visible feature).

- [ ] **Step 1: Write `HudController.cs`**

```csharp
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [SerializeField] private PlayerStats stats;
    [SerializeField] private Transform lifeContainer;
    [SerializeField] private Transform bombContainer;
    [SerializeField] private Text powerText;
    [SerializeField] private Text scoreText;
    [SerializeField] private GameObject lifeIconPrefab;
    [SerializeField] private GameObject bombIconPrefab;

    public void Configure(PlayerStats playerStats, Transform lifeRow, Transform bombRow, Text power, Text score, GameObject lifeIcon, GameObject bombIcon)
    {
        stats = playerStats;
        lifeContainer = lifeRow;
        bombContainer = bombRow;
        powerText = power;
        scoreText = score;
        lifeIconPrefab = lifeIcon;
        bombIconPrefab = bombIcon;
    }

    private void OnEnable()
    {
        stats.OnLifeChanged += RefreshLife;
        stats.OnSpellChanged += RefreshSpell;
        stats.OnPowerChanged += RefreshPower;
    }

    private void OnDisable()
    {
        stats.OnLifeChanged -= RefreshLife;
        stats.OnSpellChanged -= RefreshSpell;
        stats.OnPowerChanged -= RefreshPower;
    }

    private void Start()
    {
        scoreText.text = "0";
    }

    private void RefreshLife(int count) => RefreshIcons(lifeContainer, lifeIconPrefab, count);

    private void RefreshSpell(int count) => RefreshIcons(bombContainer, bombIconPrefab, count);

    private void RefreshPower(float value) => powerText.text = value.ToString("0.00");

    private void RefreshIcons(Transform container, GameObject iconPrefab, int count)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
        for (int i = 0; i < count; i++)
        {
            Instantiate(iconPrefab, container);
        }
    }
}
```

Write to `E:\Astral Maidens\Assets\Scripts\UI\HudController.cs`.

- [ ] **Step 2: Refresh and verify compile**

Call `mcp__unity-mcp__Unity_RunCommand` with `AssetDatabase.Refresh()`, then `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: empty.

- [ ] **Step 3: Build the HUD hierarchy under Sidebar and wire it up**

Call `mcp__unity-mcp__Unity_RunCommand`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

internal class CommandScript : IRunCommand
{
    private GameObject MakeIconPrefab(string name, Sprite sprite, string path)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(40f, 40f);
        go.GetComponent<Image>().sprite = sprite;
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return prefab;
    }

    private GameObject MakeRow(string name, Transform parent, float anchorY)
    {
        var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent, false);
        var rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, anchorY);
        rect.anchorMax = new Vector2(0.9f, anchorY + 0.08f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return row;
    }

    private Text MakeText(string name, Transform parent, float anchorY, string initial)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, anchorY);
        rect.anchorMax = new Vector2(0.9f, anchorY + 0.06f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<Text>();
        text.text = initial;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.color = Color.white;
        return text;
    }

    public void Execute(ExecutionResult result)
    {
        var lifeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/life_heart_blue.png");
        var bombSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/bomb_star_diamond_small.png");
        var powerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/power_orb_framed.png");

        var lifeIconPrefab = MakeIconPrefab("LifeIcon", lifeSprite, "Assets/Prefabs/UI/LifeIcon.prefab");
        var bombIconPrefab = MakeIconPrefab("BombIcon", bombSprite, "Assets/Prefabs/UI/BombIcon.prefab");

        var sidebar = GameObject.Find("Sidebar").transform;

        var lifeRow = MakeRow("LifeRow", sidebar, 0.85f);
        var bombRow = MakeRow("BombRow", sidebar, 0.75f);

        var powerRow = MakeRow("PowerRow", sidebar, 0.6f);
        var powerIconObj = new GameObject("PowerIcon", typeof(RectTransform), typeof(Image));
        powerIconObj.transform.SetParent(powerRow.transform, false);
        powerIconObj.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
        powerIconObj.GetComponent<Image>().sprite = powerSprite;

        var powerTextObj = new GameObject("PowerValue", typeof(RectTransform), typeof(Text));
        powerTextObj.transform.SetParent(powerRow.transform, false);
        powerTextObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 32f);
        var powerText = powerTextObj.GetComponent<Text>();
        powerText.text = "0.00";
        powerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        powerText.fontSize = 24;
        powerText.color = Color.white;

        var scoreText = MakeText("ScoreText", sidebar, 0.5f, "0");

        var player = GameObject.Find("Player");
        var stats = player.GetComponent<PlayerStats>();

        var hudObj = new GameObject("HudController", typeof(HudController));
        hudObj.transform.SetParent(sidebar, false);
        var hud = hudObj.GetComponent<HudController>();
        hud.Configure(stats, lifeRow.transform, bombRow.transform, powerText, scoreText, lifeIconPrefab, bombIconPrefab);
        result.RegisterObjectCreation(hudObj);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        result.Log("HUD wired to PlayerStats: {0}", stats);
    }
}
```

- [ ] **Step 4: Verify no console errors**

Call `mcp__unity-mcp__Unity_GetConsoleLogs` (`logTypes: "Error"`). Expected: empty.

- [ ] **Step 5: Manual Play Mode check**

Call `mcp__unity-mcp__Unity_ManageEditor` `Action: "Play"`. Confirm via `Unity_Camera_Capture` that Life hearts (3) and Bomb icons (3) render in the sidebar and `Power: 0.00` / `Score: 0` text is visible. Take a hit in-game (or verify code path) and confirm the heart row shrinks to 2. Call `mcp__unity-mcp__Unity_ManageEditor` `Action: "Stop"`.

- [ ] **Step 6: Commit**

```bash
cd "/e/Astral Maidens" && git add -A Assets/Scripts/UI Assets/Prefabs/UI Assets/Scenes && git commit -m "Add HudController and wire Life/Bomb/Power/Score sidebar UI" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

---

### Task 11: Final Integration Pass

**Files:** none (verification only).

- [ ] **Step 1: Full Play Mode smoke test**

Call `mcp__unity-mcp__Unity_ManageEditor` `Action: "Play"`. Verify in sequence via `Unity_Camera_Capture` / `Unity_GetConsoleLogs`:
1. Player renders at bottom of playfield, moves in 8 directions, clamped to `PlayfieldBounds`.
2. Holding Focus slows movement and shows the small hitbox dot.
3. `TestEmitter` cycles all 4 visually distinct patterns over ~24 seconds (Radial burst, Spiral arms, Targeted shots toward the player, WayShot fan).
4. Colliding the player hitbox with a bullet decrements a Life heart in the HUD and grants ~2s of blinking invulnerability (no double-hit).
5. No errors or exceptions in `Unity_GetConsoleLogs` (`logTypes: "Error"`) over the whole run.

Call `mcp__unity-mcp__Unity_ManageEditor` `Action: "Stop"` when done.

- [ ] **Step 2: Fix any issues found**

If Step 1 surfaces a bug, fix the relevant script(s) with `Unity_ApplyTextEdits`/`Unity_ScriptApplyEdits` or the Write tool, re-run `AssetDatabase.Refresh()`, re-verify console is clean, and repeat Step 1 until all 5 checks pass.

- [ ] **Step 3: Final commit**

```bash
cd "/e/Astral Maidens" && git add -A && git status --short
```

If anything is unstaged (fixes from Step 2), commit it:

```bash
cd "/e/Astral Maidens" && git commit -m "Fix issues found in Phase 1 integration pass" -m "Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>"
```

Phase 1 is complete once all 11 tasks are committed and the Step 1 checklist passes cleanly.
