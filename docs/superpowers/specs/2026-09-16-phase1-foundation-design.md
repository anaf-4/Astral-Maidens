# Phase 1 — Foundation Systems Design

**Date:** 2026-09-16
**Status:** Approved by user, pending implementation plan
**Parent doc:** `C:\Users\apple\Downloads\GDD.md` (탄막 슈팅 게임 기획서, "Astral Maidens" 가칭 — Unity 프로젝트와 다른 드라이브에 위치해 상대경로 대신 절대경로로 표기)
**Scope:** GDD 9번 항목의 착수 순서 중 1~4단계 + 8단계 일부 (오브젝트 풀링, 플레이어 컨트롤러, 탄막 패턴 데이터 구조, 기본 HUD). 보스 AI/스펠카드, 잡몹, 스코어링, Bomb의 실제 전탄소멸, Power 연동 탄종 변화는 이 스펙의 범위 밖이며 후속 스펙에서 다룬다.

---

## 1. Goals / Non-goals

**Goals**
- 플레이필드-사이드 UI 클래식 레이아웃으로 카메라/캔버스 구성
- `blue_magical_girl`을 사용하는 동작하는 플레이어 컨트롤러(8방향 이동, Focus, 피격/무적)
- 네이티브 `UnityEngine.Pool.ObjectPool<T>` 기반 탄 오브젝트 풀링
- GDD 3번 항목의 4종 패턴(Radial/Spiral/Targeted/Way-Shot)을 데이터(SO)로 표현하고 범용 로직으로 실행하는 `PatternExecutor`
- 위 파이프라인을 육안+유닛 테스트로 검증할 수 있는 최소 HUD와 테스트 이미터

**Non-goals (후속 스펙)**
- 실제 보스 AI/페이즈 상태머신, 스펠카드, 상태이상(슬로우/시야방해/Burn)
- Bomb의 전탄소멸+무적 효과, Power에 따른 탄종/화력 실제 강화
- 잡몹, 스코어링, 스테이지 진행/클리어 흐름

---

## 2. 화면/카메라 구성

- 카메라: Orthographic, `Orthographic Size = 8`, `Camera Rect = (x:0, y:0, w:0.6, h:1)` — 화면 좌측 60%에 플레이필드 렌더링.
- 플레이필드 논리 경계(`PlayfieldBounds`): 월드 좌표 기준 `X: -6 ~ 6`, `Y: -8 ~ 8` (가로 12 × 세로 16 unit), 원점 중심. 이 경계는 카메라가 실제로 보여주는 영역보다 좁아도 되며(카메라 여유 공간은 배경으로 채움), 게임플레이 로직(플레이어 이동 clamp, `BulletBoundsChecker`)은 이 논리 경계값을 기준으로 동작한다.
- UI: `Canvas`(Screen Space - Overlay), Reference Resolution `1920x1080`, Scale With Screen Size(Match 0.5). 사이드바 컨테이너는 Anchor `min(0.62, 0) ~ max(1, 1)`로 우측에 배치.
- 스프라이트 Pixels Per Unit: 모든 캐릭터/이펙트 스프라이트 `PPU = 100`으로 통일 임포트.

## 3. 에셋 임포트

| 대상 | 소스 | 처리 |
|---|---|---|
| 플레이어 | `AstralMaidens/players/blue_magical_girl/frames_fixed/*.png` (16프레임) | Sprite(2D and UI), PPU 100, Filter Mode: Point (no filter), Mipmap 비활성화, Pivot: Center |
| 테스트 탄 | `AstralMaidens/effects/ice_magic/frames_fixed/` 중 오브(04)·스파클(01) 2종 | 위와 동일 설정 |
| HUD | `AstralMaidens/ui/01_HP_Bomb_Icons/`, `ui/03_Player_Stats/` 전체 | Sprite(2D and UI), Filter Mode: Point, Mipmap 비활성화 |

임포트는 Unity 에디터 스크립트(`AssetPostprocessor` 또는 일괄 선택 후 Inspector 설정)로 처리하며, 개별 파일마다 수작업 설정하지 않는다.

## 4. 오브젝트 풀링 코어

- `Assets/Scripts/Core/Pooling/IPoolable.cs` — `OnSpawn()` / `OnDespawn()` 2개 메서드.
- `Assets/Scripts/Core/Pooling/BulletPoolManager.cs` — `Dictionary<GameObject, ObjectPool<Bullet>>`로 프리팹별 풀 관리, `Spawn(prefab, position, rotation)` / `Despawn(bullet)` 제공. 내부적으로 `UnityEngine.Pool.ObjectPool<T>` 사용(커스텀 큐 구현 없음).
- `Assets/Scripts/Bullets/Bullet.cs` — `IPoolable` 구현, `Vector2 velocity` 필드, 매 프레임 `transform.position += velocity * Time.deltaTime`.
- `Assets/Scripts/Bullets/BulletBoundsChecker.cs` — `FixedUpdate`가 아닌 5프레임마다 1회(`Time.frameCount % 5 == 0`) 활성 탄 리스트를 순회해 `PlayfieldBounds`(2번 항목 값, 여유 마진 +2 unit) 이탈 탄을 `BulletPoolManager.Despawn` 호출.

## 5. 플레이어 컨트롤러

- `Assets/Settings/PlayerControls.inputactions` 신규 생성 — Action Map `Gameplay`: `Move`(Vector2, WASD/방향키/게임패드 스틱), `Fire`(Button, Z/Key A 대응), `Focus`(Button, Shift/Key S 대응), `Bomb`(Button, X/Key D 대응). 기존 `InputSystem_Actions.inputactions`는 사용하지 않는다.
- `Assets/Scripts/Player/PlayerController.cs`: `Move` 입력을 정규화 후 속도 곱(대각선 추가 가속 없음). 이동속도 `6 unit/s`(통상), `3 unit/s`(Focus, GDD 50% 감속 규정). Focus 중 `PlayerHitbox`(CircleCollider2D, radius `0.08 unit`) SpriteRenderer 표시. 위치는 `PlayfieldBounds` 내로 `Mathf.Clamp`.
- `Assets/Scripts/Player/PlayerStats.cs`: `Life`(기본 3), `Spell`(기본 3), `Power`(기본 0.00, 최대 4.00) — 값 변경 시 `event Action<int/float>`로 HUD에 통지. `TakeHit()`: Life -1, 2초 무적(해당 시간 동안 충돌 무시 + SpriteRenderer 점멸), Life 0 도달 시 `event Action OnGameOver` 발행(처리는 후속 스펙).
- 충돌: `PlayerHitbox`(트리거) ↔ `EnemyBullet` 레이어 트리거 충돌 시 `PlayerStats.TakeHit()` 호출, 해당 탄은 즉시 despawn.

## 6. 탄막 패턴 시스템

- `Assets/Scripts/Bullets/Patterns/PatternType.cs` — `enum { Radial, Spiral, Targeted, WayShot }`
- `Assets/Scripts/Bullets/Patterns/BulletPatternSO.cs` (ScriptableObject): `PatternType type`, `GameObject bulletPrefab`, `int n`(Radial 탄수/Spiral arms), `int k`(Targeted/WayShot 탄수), `float deltaThetaDeg`(Spiral), `float fanAngleDeg`(WayShot), `float baseAngleDeg`, `float speed`, `float interval`.
- `Assets/Scripts/Bullets/Patterns/PatternExecutor.cs`: `BulletPatternSO`를 읽어 코루틴으로 `interval`마다 발사. 4종 분기:
  - Radial: `n`개 탄을 `360/n`도 균등 각도로 즉시 전량 발사.
  - Spiral: 매 tick마다 현재 각도를 `deltaThetaDeg`만큼 누적 회전시키며 `n`(arms)개 팔 동시 발사.
  - Targeted: 발사 시점 플레이어 위치 방향으로 `k`개(각도 미세 분산) 발사.
  - WayShot: `baseAngleDeg`(플레이어 조준 시 실시간 계산) 중심으로 `fanAngleDeg` 범위 내 `k`개 부채꼴 발사.
- `Assets/Scripts/Testing/PatternTestEmitter.cs`: 플레이필드 상단 `(0, 6)`에 고정 배치, 4개 `BulletPatternSO` 애셋(Radial N=16, Spiral arms=4/Δθ=6°/interval=0.05s, Targeted K=3, WayShot K=5/fanAngle=45°)을 6초씩 순환 실행하는 씬 오브젝트. 보스 AI가 아닌 파이프라인 검증용 임시 컴포넌트이며, 2단계에서 실제 보스로 대체된다.

## 7. HUD

- `Assets/Scripts/UI/HudController.cs`: `PlayerStats` 이벤트 구독.
  - Life: `ui/01_HP_Bomb_Icons/life_heart_*` 아이콘을 `Life` 개수만큼 가로 나열.
  - Bomb: `ui/01_HP_Bomb_Icons/bomb_*` 아이콘을 `Spell` 개수만큼 나열.
  - Power: `ui/03_Player_Stats/power_orb_framed.png` 배경 + `"0.00"~"4.00"` TextMeshPro 텍스트.
  - Score: 고정 텍스트 `"0"` (실제 스코어링은 후속 스펙).

## 8. 검증

- `Assets/Tests/EditMode/PatternExecutorMathTests.cs`: Radial N등분 각도 배열, Spiral Δθ 누적각, WayShot 부채꼴 각도 분산 계산 함수를 순수 함수로 분리해 assert 기반 테스트. Unity Test Framework 패키지가 `Packages/manifest.json`에 없다면 추가.
- Play 모드 수동 확인: `PatternTestEmitter` 4패턴 육안 확인, 플레이어 이동/Focus/피격/무적/HUD 갱신 확인.

## 9. 폴더 구조

```
Assets/
  Scripts/
    Core/Pooling/   (IPoolable, BulletPoolManager)
    Player/         (PlayerController, PlayerStats)
    Bullets/        (Bullet, BulletBoundsChecker)
    Bullets/Patterns/ (PatternType, BulletPatternSO, PatternExecutor)
    UI/             (HudController)
    Testing/        (PatternTestEmitter)
  Tests/EditMode/   (PatternExecutorMathTests)
  Settings/PlayerControls.inputactions
```

Assembly Definition(asmdef)은 이 단계에서 추가하지 않는다(스크립트 수가 적어 불필요, 필요해지면 추가).

---

## 열린 질문 / 다음 단계로 이월

- 정확한 보스 발사 파라미터(각 보스 실제 수치)는 이미 GDD 5번 항목에 명시되어 있으므로 2단계 스펙에서 그대로 가져와 구현.
- Power에 따른 탄종/화력 실제 강화 테이블은 밸런싱 단계(GDD 9-10)에서 확정.
- 잡몹 웨이브, 스테이지 진행/클리어 흐름, Bomb 전탄소멸은 별도 스펙.
- **하드 전제조건 (2단계 스펙 착수 전 필수 반영) — 탄막 진영(faction) 구분:** 현재 `Bullet`은 발사 주체(적/플레이어)를 구분하는 필드가 없고, `PlayerHitboxTrigger`는 씬에 존재하는 모든 `Bullet`을 무조건 피격 처리한다. 1단계는 적탄만 존재해 문제가 드러나지 않지만, 2단계에서 플레이어 사격(`Key A`)이 추가되는 즉시 플레이어 자신의 탄이 자신의 히트박스에 닿아 즉사 처리되는 버그가 발생한다. 2단계 스펙은 착수 시점에 `Bullet`에 소유자 구분(예: `bool isEnemyBullet` 또는 owner enum)을 추가하고 `PlayerHitboxTrigger`가 그 값을 확인하도록 반드시 포함해야 한다 — 물리 레이어 분리는 필요 시 추가하는 별도 최적화이며 이 전제조건의 필수 요건은 아니다.
