# Stage 2 — Ice Crystal Maiden Boss Design

**Date:** 2026-09-17
**Status:** Approved by user, pending implementation plan
**Parent doc:** `C:\Users\apple\Downloads\GDD.md` (섹션 5-1, 6, 7-3), `docs/superpowers/specs/2026-09-16-phase1-foundation-design.md` (1단계, 이 스펙이 그 위에 얹힘)
**Scope:** 플레이어 사격, 탄막 소유자(faction) 구분, 보스 페이즈/스펠카드 상태머신, 상태이상 시스템(슬로우만), Ice Crystal Maiden 보스(2페이즈), 보스 체력바/타이머 UI. GDD 9번 항목 순서의 5~8단계에 해당.

---

## 1. Goals / Non-goals

**Goals**
- `Fire` 입력으로 플레이어가 직선 관통 탄을 연사한다.
- 탄에 소유자(`Enemy`/`Player`) 구분이 생겨, 플레이어가 자신의 탄에 맞지 않고 보스는 적탄이 아닌 플레이어 탄에만 반응한다.
- 데이터 기반 보스 페이즈/스펠카드 상태머신(`BossPhaseSO`/`BossController`)을 구현하고, GDD 5-1의 실제 수치로 Ice Crystal Maiden(2페이즈: 통상 1 + 스펠카드 1)을 조립한다.
- 스펠카드의 슬로우 디버프 기믹을 `IPlayerStatus`/`PlayerStatusController`로 구현한다(GDD 7-3 아키텍처 그대로, 슬로우 하나만).
- 보스 체력바(페이즈 전환이 시각적으로 구분됨) + 스펠카드 타이머 UI.
- 최소한의 승패 처리(발사/이동 중단 + 로그).

**Non-goals (후속)**
- Purple Moon Archmage/Sun Goddess 및 그 전용 기믹(시야방해/Burn).
- 잡몹, 스코어링, 스테이지 클리어/재시작 화면, Bomb 전탄소멸.
- Power에 따른 탄종/화력 강화.
- 보스 스프라이트 애니메이션(1단계 플레이어와 동일하게 정지 프레임 1장만 사용).

---

## 2. 탄막 소유자 구분

- `Assets/Scripts/Bullets/BulletOwner.cs` — `enum { Enemy, Player }`.
- `Bullet.cs`에 `[SerializeField] private BulletOwner owner = BulletOwner.Enemy;` 필드 + `public BulletOwner Owner => owner;` 추가. 프리팹 단위로 고정(적탄 프리팹은 `Enemy`, 플레이어 탄 프리팹은 `Player`) — `BulletPoolManager.Spawn(prefab, position, velocity)` 시그니처는 변경 없음.
- `PlayerHitboxTrigger.OnTriggerEnter2D`: `bullet.Owner != BulletOwner.Enemy`면 무시.
- `BossHitbox.OnTriggerEnter2D`(신규): `bullet.Owner != BulletOwner.Player`면 무시.
- 플레이어 탄은 "관통형"이라 피격 시 despawn되지 않는다. Unity 트리거는 겹쳐있는 동안 `OnTriggerEnter2D`가 한 번만 발화하므로 프레임당 중복 데미지 걱정 없이 그대로 통과시키면 된다.

## 3. 플레이어 사격

- `Assets/Scripts/Player/PlayerShooter.cs`: `Fire` 액션이 눌려있는 동안 `fireInterval`(기본 0.1초)마다 플레이어 위치에서 `Vector2.up * bulletSpeed`(기본 10 unit/s, 적탄 3 unit/s보다 빠르게)로 `BulletPoolManager.Spawn` 호출. Focus 여부는 사격에 영향 없음(1단계 스펙 범위 밖).
- `Assets/Prefabs/Bullets/PlayerBullet.prefab`: `TestBullet.prefab`과 동일 구조(SpriteRenderer + Kinematic Rigidbody2D + trigger CircleCollider2D + `Bullet`)지만 `owner = Player`, 스프라이트는 기존에 임포트된 `Assets/Art/Effects/IceMagic/frame_01.png`를 흰색에 가까운 tint(`Color(0.85, 0.95, 1, 1)`)로 재사용해 적탄(청록색 무tint)과 구분. 반지름 0.12.
- 데미지: `Bullet`에 `[SerializeField] private int damage = 1;` + `public int Damage => damage;` 추가(적탄은 이 필드를 쓰지 않음 — 플레이어는 `PlayerStats.TakeHit()`이 고정 1 Life 차감이라 데미지 수치가 필요 없음, 보스만 가변 데미지가 필요).

## 4. 보스 페이즈/스펠카드 상태머신

- `Assets/Scripts/Bosses/BossPhaseSO.cs` (ScriptableObject):
  ```
  string phaseName
  int hp                      // 통상 페이즈 격파 임계치(스펠카드는 무시, 타임오버로만 종료)
  bool isSpellCard
  float timeLimitSeconds       // 스펠카드만 사용, 0=무제한(통상 페이즈)
  BulletPatternSO[] patterns    // 동시에 실행할 패턴들
  float[] patternStartDelays    // patterns와 같은 길이, 각 패턴 시작 지연(초). 기본 0.
  bool appliesSlow
  float slowMultiplier          // 기본 0.4
  float slowDuration            // 기본 8
  float slowReapplyInterval     // 기본 4 (스펠카드 진행 중 재적용 주기)
  ```
- `Assets/Scripts/Bosses/BossController.cs`:
  - `BossPhaseSO[] phases`, `int currentPhaseIndex`, `int currentPhaseHp`.
  - 페이즈 시작: 이전 페이즈용으로 만들어둔 `PatternExecutor` 컴포넌트들을 전부 `Destroy` → 새 페이즈의 `patterns.Length`만큼 자식 GameObject(`PatternExecutor` 부착)를 동적으로 생성해 `patterns[i]`를 `patternStartDelays[i]`만큼 지연 후 `SetPattern` 호출(지연은 `BossController`의 코루틴이 처리, `PatternExecutor` 자체는 1단계 API 그대로 — 코드 안 건드림). 페이즈마다 패턴 개수가 달라도(이 보스는 둘 다 2개지만, 향후 다른 보스는 다를 수 있음) 그대로 대응 가능. `appliesSlow`면 스펠카드 시작 즉시 `ApplySlow` 호출 + `slowReapplyInterval`마다 스펠카드 종료까지 반복 호출하는 코루틴 시작.
  - 통상 페이즈: `TakeDamage(int amount)`로 `currentPhaseHp`가 0 이하가 되면 다음 페이즈로.
  - 스펠카드: 매 프레임 남은 시간 체크, `timeLimitSeconds` 경과 시 타임오버로 다음 페이즈. (스펠카드도 `TakeDamage`로 HP 0 도달 시 즉시 다음 페이즈로 전환 가능 — GDD 4번 "스펠카드 파쇄 시 보너스" 문구상 데미지로도 깨질 수 있어야 함. 보너스 점수/아이템 지급 자체는 스코어링 범위 밖이라 생략, 파쇄로 인한 페이즈 전환만 구현.)
  - 마지막 페이즈 종료(HP 0 또는 타임오버) → 모든 `PatternExecutor` 정지, `OnBossDefeated` 이벤트 발행 + `Debug.Log`.
  - `event Action<int,int> OnPhaseHpChanged`(current, max), `event Action<string,bool> OnPhaseChanged`(name, isSpellCard), `event Action<float> OnTimerChanged`(스펠카드 남은 시간, 통상 페이즈면 -1).
- `Assets/Scripts/Bosses/BossHitbox.cs`: `Player` 소유 탄과 충돌 시 `bossController.TakeDamage(bullet.Damage)`.

## 5. 상태이상 시스템 (슬로우)

- `Assets/Scripts/Player/IPlayerStatus.cs` — `void Tick(float deltaTime); bool IsExpired { get; }`.
- `Assets/Scripts/Player/SlowStatus.cs` — `IPlayerStatus` 구현, `Multiplier` 프로퍼티 + 남은 시간 카운트다운.
- `Assets/Scripts/Player/PlayerStatusController.cs`: 현재 활성 `SlowStatus` 하나 보관(새로 걸리면 교체), `ApplySlow(multiplier, duration)`, `public float CurrentSpeedMultiplier`(활성 슬로우 없으면 1).
- `PlayerController.Update()`: `speed = (focusing ? focusSpeed : normalSpeed) * statusController.CurrentSpeedMultiplier`.

## 6. Ice Crystal Maiden 보스 조립

- `Assets/Art/Bosses/IceCrystalMaiden/character_01.png` 임포트(PPU 130 — 트림된 평균 폭 ≈263px 기준 시각적 폭 약 2 unit, 플레이어보다 크게). Point filter, no mipmap, 나머지 15프레임은 이번 단계에서 미사용(애니메이션 없음, 1단계 플레이어와 동일 원칙).
- 적탄 이펙트 추가 임포트: `Assets/Art/Effects/IceMagic/frame_03.png`(초승달, crystal_moon), `frame_05.png`(snowflake 변형, ice_needle), `frame_09.png`(sparkle-orb, ice_shard) — 기존 frame_01/frame_04는 그대로 둠(플레이어 탄/1단계 테스트용).
- `Assets/Data/Bosses/IceCrystalMaiden_Phase1.asset`(통상):
  - `hp=30`, `isSpellCard=false`, `timeLimitSeconds=0`
  - `patterns[0]` = Radial(N=24, speed=3, interval=1.2, bulletPrefab=ice_shard 기반 신규 프리팹), `patternStartDelays[0]=0`
  - `patterns[1]` = Targeted(K=5, targetedSpreadDeg=10, speed=3, interval=1.2, 같은 프리팹), `patternStartDelays[1]=1.2` (GDD "Radial 24way 발사 후 1.2초 텀 두고 Targeted 5way" — 같은 1.2초 주기를 유지하되 서로 1.2초씩 어긋나게 시작해 교대로 발사되는 리듬을 만듦)
  - `appliesSlow=false`
- `Assets/Data/Bosses/IceCrystalMaiden_Phase2.asset`(스펠카드 "永久凍結 · 퍼페추얼 프로스트"):
  - `hp=50`, `isSpellCard=true`, `timeLimitSeconds=30`
  - `patterns[0]` = Spiral(n=8 arms, deltaThetaDeg=6, speed=3, interval=0.05, bulletPrefab=crystal_moon 기반 프리팹), `patternStartDelays[0]=0`
  - `patterns[1]` = Radial(N=32, speed=3, interval=4, bulletPrefab=ice_needle 기반 프리팹), `patternStartDelays[1]=0`
  - `appliesSlow=true`, `slowMultiplier=0.4`, `slowDuration=8`, `slowReapplyInterval=4`
- 각 탄종(ice_shard/crystal_moon/ice_needle)마다 별도 `Bullet` 프리팹 필요(스프라이트만 다름, 구조는 `TestBullet.prefab`과 동일, `owner=Enemy`) — `Assets/Prefabs/Bullets/`에 3개 추가.
- 씬: 기존 `PatternTestEmitter`(`Enemies/TestEmitter`)는 비활성화(또는 제거 — 스크립트/에셋 자체는 남겨둠, 1단계 스펙에 "2단계에서 실제 보스로 대체된다"고 이미 명시됨). `Enemies` 하위에 `Boss` GameObject를 `(0, 6, 0)`에 배치, `SpriteRenderer`+`BossController`+`BossHitbox`(CircleCollider2D trigger, radius ≈1.0). 페이즈 실행용 `PatternExecutor`는 4번 항목대로 `BossController`가 페이즈 전환마다 동적으로 만들고 지움.

## 7. 보스 체력바/타이머 UI

- `Assets/Art/UI/bossbar_simple_filled_red.png`, `Assets/Art/UI/timerbar_small_blue.png` 신규 임포트.
- `Assets/Scripts/UI/BossHudController.cs`: `BossController` 이벤트 구독.
  - HP 바: `Image`(`type=Filled`, `fillMethod=Horizontal`)의 `fillAmount = currentHp/maxHp`. **페이즈 전환 시 `fillAmount`를 1로 리셋**해 "페이즈 세그먼트 구분"을 표현(GDD 요구사항 — 연속 바 하나에 구간선을 그리는 대신, 페이즈가 바뀔 때마다 바가 가득 찬 상태로 다시 시작하는 것으로 시각적 구분을 준다).
  - 타이머: 스펠카드 페이즈에서만 표시(통상 페이즈는 숨김), 같은 방식으로 `fillAmount = remaining/timeLimitSeconds`.
  - 배치: Canvas 전체 화면 기준 상단 중앙, 플레이필드 영역(카메라가 렌더링하는 화면 좌측 60%) 위에 얹히도록 anchor `min(0.05,0.9) ~ max(0.55,0.98)` (사이드바가 아니라 플레이필드 위 — 장르 관례상 보스 체력바는 화면 상단에 걸쳐 보이는 게 표준).

## 8. 승패 처리 (최소)

- `BossController.OnBossDefeated` → 모든 패턴 정지(이미 4번에 포함), `Debug.Log("Boss defeated")`.
- `PlayerStats.OnGameOver`(1단계에 이미 존재, 지금까지 구독자 없음) → 신규 구독자 추가(`GameFlowController` 같은 별도 클래스 만들 필요 없이 `PlayerController`/`PlayerShooter`가 직접 구독해 `enabled=false`로 정지) + `Debug.Log("Game Over")`.

---

## 9. 검증

- `BossPhaseSO`/`BossController`의 페이즈 전환 판정(통상: HP 0, 스펠카드: HP 0 또는 타임오버)은 순수 로직이라 1단계 `PatternMath`와 같은 방식으로 EditMode 테스트 가능 — `Assets/Tests/Editor/BossPhaseLogicTests.cs`에 페이즈 전환 조건 함수를 분리해 테스트.
- Play 모드 수동 확인: 플레이어 사격이 보스 HP를 깎는지, 보스 탄이 플레이어에게만(자기 탄엔 안 맞고) 피격되는지, 페이즈 전환 시 체력바가 리셋되는지, 스펠카드 중 슬로우가 걸리는지, 타임오버/격파 둘 다로 페이즈 전환이 되는지.

## 10. 폴더 구조 (1단계에 추가되는 것만)

```
Assets/
  Scripts/
    Bullets/BulletOwner.cs
    Player/PlayerShooter.cs, IPlayerStatus.cs, SlowStatus.cs, PlayerStatusController.cs
    Bosses/BossPhaseSO.cs, BossController.cs, BossHitbox.cs
    UI/BossHudController.cs
  Data/Bosses/IceCrystalMaiden_Phase1.asset, IceCrystalMaiden_Phase2.asset
  Prefabs/Bullets/PlayerBullet.prefab, IceShard.prefab, CrystalMoon.prefab, IceNeedle.prefab
  Art/Bosses/IceCrystalMaiden/character_01.png
  Tests/Editor/BossPhaseLogicTests.cs
```

---

## 열린 질문 / 다음 단계로 이월

- 밸런싱 수치(페이즈 HP 30/50, 플레이어 탄 간격 0.1s/데미지 1, 탄속 10)는 전부 임의 추정치 — GDD 9-10 밸런싱 단계에서 조정.
- 보스 스프라이트 애니메이션(16프레임 활용)은 이번 범위 밖, 필요해지면 별도 스펙.
- 스펠카드 파쇄 보너스(점수/아이템)는 스코어링 시스템과 함께 후속.
