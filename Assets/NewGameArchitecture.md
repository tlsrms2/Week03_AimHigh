# Aim High 설계도

## 1. 프로젝트 개요

이 프로젝트는 기존 FPS Microgame을 기반으로 하되, 실제 게임플레이는 전통적인 FPS가 아니라 `고정형 에임 슈팅 로그라이크`로 재구성한다.

- 플레이어는 맵을 돌아다니지 않는다.
- 플레이어는 한 지점에 고정된 상태로 `시점 이동`과 `발사`만 수행한다.
- 화면에 생성되는 다양한 과녁을 빠르게 정확히 맞춰 점수를 획득한다.
- 라운드 종료 후 상점에서 강화 요소를 구매하며 점수 상승 시너지를 만든다.
- 라운드가 진행될수록 목표 점수와 난이도가 크게 증가한다.

핵심 레퍼런스:

- `AimLabs`: 피지컬 기반 조준/반응 슈팅
- `Balatro`: 라운드 기반 성장, 배수, 시너지, 점수 펌핑

## 2. 핵심 플레이 루프

한 사이클은 아래 순서로 진행된다.

1. `Round Intro`
2. `Random Event Apply`
3. `Shooting Phase`
4. `Round Result`
5. `Shop Phase`
6. `Next Round` 또는 `Game Over`

### 2.1 Round Intro

- 현재 라운드 번호 표시
- 이번 라운드 목표 점수 표시
- 이번 라운드 돌발 이벤트 표시
- 짧은 대기 후 슈팅 페이즈 진입

### 2.2 Shooting Phase

- 과녁이 정해진 규칙으로 생성된다.
- 플레이어는 과녁을 맞춰 점수를 얻는다.
- 거리, 과녁 종류, 약점 부위, 시너지 효과에 따라 최종 점수가 계산된다.
- 제한 시간 종료 또는 특별 조건 만족 시 라운드 종료

### 2.3 Round Result

- 목표 점수 달성 여부 판정
- 획득 점수 및 재화 정산
- 보너스 보상 적용

### 2.4 Shop Phase

- 재화를 사용해 업그레이드/패시브/특수 시너지 구매
- 다음 라운드의 성능 빌드업

## 3. 승패 구조

### 3.1 승리 판정

- 해당 라운드 종료 시 `현재 라운드 점수 >= 라운드 목표 점수`

### 3.2 패배 판정

- 라운드 종료 시 목표 점수를 달성하지 못함

### 3.3 장기 목표

- 가능한 오래 생존
- 가능한 높은 라운드 도달
- 가능한 높은 최종 점수 달성

## 4. 게임 상태 설계

```csharp
public enum GameState
{
    Boot,
    MainMenu,
    RoundIntro,
    Shooting,
    RoundResult,
    Shop,
    GameOver
}
```

상태 전이는 아래 흐름을 따른다.

`Boot -> MainMenu -> RoundIntro -> Shooting -> RoundResult -> Shop -> RoundIntro`

실패 시:

`Shooting -> RoundResult -> GameOver`

## 5. 시스템 아키텍처

새 게임 로직은 기존 `Assets/FPS` 내부를 직접 오염시키기보다 별도 폴더에 구축한다.

권장 폴더 구조:

```text
Assets/
  GameAimHigh/
    Scripts/
      Core/
      Input/
      Round/
      Targets/
      Shooting/
      Scoring/
      Shop/
      Modifiers/
      UI/
      Data/
    ScriptableObjects/
      Targets/
      Shop/
      Modifiers/
      Rounds/
    Prefabs/
      Targets/
      UI/
      Effects/
    Scenes/
      MainGame.unity
```

## 6. 매니저 설계

## 6.1 GameManager

역할:

- 게임의 최상위 상태 관리
- 각 매니저 초기화 순서 제어
- 상태 전이 요청 처리

주요 책임:

- 현재 `GameState` 보관
- 씬 진입 시 초기화
- 라운드 진입 및 종료 흐름 연결

예상 핵심 메서드:

```csharp
void StartGame();
void ChangeState(GameState newState);
void EnterRound(int roundIndex);
void EnterShop();
void EndGame();
```

## 6.2 RoundManager

역할:

- 라운드 데이터 생성 및 진행 관리

주요 책임:

- 현재 라운드 번호 관리
- 제한 시간 관리
- 목표 점수 계산
- 라운드 시작/종료 이벤트 발행
- 돌발 이벤트 선택 요청

예상 핵심 메서드:

```csharp
void StartRound(int roundIndex);
void FinishRound();
bool IsRoundSuccess();
int CalculateTargetScore(int roundIndex);
```

## 6.3 ScoreManager

역할:

- 점수 계산과 배수 시스템 담당

주요 책임:

- 현재 라운드 점수
- 총 누적 점수
- 현재 배수값
- 콤보, 보너스, 치명타, 거리 보정 적용

예상 핵심 메서드:

```csharp
void ResetRoundScore();
void AddScore(TargetScoreContext context);
int GetCurrentRoundScore();
float GetCurrentMultiplier();
```

## 6.4 TargetManager

역할:

- 과녁 스폰과 활성 타겟 목록 관리

주요 책임:

- 스폰 주기 관리
- 활성 타겟 개수 제한
- 타겟 제거 및 재생성
- 라운드별 출현 테이블 적용

예상 핵심 메서드:

```csharp
void BeginSpawning();
void StopSpawning();
void RegisterTarget(Target target);
void UnregisterTarget(Target target);
Target SpawnRandomTarget();
```

## 6.5 ShopManager

역할:

- 상점 아이템 생성과 구매 처리

주요 책임:

- 재화 확인
- 아이템 목록 생성
- 구매 가능 여부 판단
- 구매 효과 적용

예상 핵심 메서드:

```csharp
void OpenShop();
void CloseShop();
bool TryPurchase(ShopItemDefinition item);
List<ShopItemDefinition> GenerateShopItems();
```

## 6.6 ModifierManager

역할:

- 돌발 이벤트와 영구 업그레이드 효과를 적용

주요 책임:

- 라운드 시작 시 이벤트 선택
- 영구 버프/디버프 유지
- 타겟, 점수, 입력, UI에 효과 전파

예상 핵심 메서드:

```csharp
void ApplyRoundModifier(ModifierDefinition modifier);
void ApplyPermanentModifier(ModifierDefinition modifier);
float ModifyScore(float baseScore, TargetScoreContext context);
```

## 6.7 UIManager

역할:

- HUD와 라운드/상점 UI 전체 관리

주요 책임:

- 현재 점수 표시
- 목표 점수 표시
- 라운드 경고 메시지 표시
- 상점 UI 오픈/닫기
- 게임오버 화면 표시

## 7. 플레이어 구조

새 기획에서는 플레이어 이동이 핵심이 아니므로 기존 FPS 캐릭터 대신 `고정형 에임 컨트롤러`를 사용한다.

## 7.1 AimController

역할:

- 플레이어의 카메라 시점 회전 담당

주요 책임:

- 마우스 X/Y 입력 반영
- 회전 제한
- 민감도 적용

예상 핵심 메서드:

```csharp
void HandleLookInput(Vector2 lookInput);
```

## 7.2 PlayerShooter

역할:

- 발사 처리와 히트 판정 담당

주요 책임:

- 클릭 입력 처리
- 히트스캔 또는 발사체 방식 지원
- 타겟 맞춤 판정
- 발사 이펙트/사운드 호출

예상 핵심 메서드:

```csharp
void TryShoot();
bool PerformHitScan(out RaycastHit hit);
```

## 8. 타겟 시스템 설계

## 8.1 Target 베이스 클래스

모든 과녁은 공통 베이스 클래스를 사용한다.

공통 속성:

- 기본 점수
- 최대 체력
- 남은 체력
- 생존 시간
- 거리 점수 가중치
- 특수 효과 여부

예상 필드:

```csharp
public int BaseScore;
public int MaxHealth;
public float Lifetime;
public float DistanceWeight;
public bool IsSpecialTarget;
```

예상 핵심 메서드:

```csharp
void Initialize(TargetDefinition definition);
void TakeHit(HitContext hitContext);
void Die();
int CalculateBaseReward(HitContext hitContext);
```

## 8.2 타겟 종류

초기 구현 후보:

- `BasicTarget`
  일반 점수 타겟
- `TankTarget`
  체력이 높은 타겟
- `BonusTarget`
  추가 점수 또는 재화 지급
- `PenaltyTarget`
  잘못 맞추면 패널티 부여
- `BurstTarget`
  터지며 주변 효과 발생

## 8.3 Hitbox 구조

선택적으로 부위별 가중치를 둔다.

- `Center Hitbox`: 고배점
- `Outer Hitbox`: 기본 배점
- `WeakPoint`: 특수 효과 발동

## 9. 점수 계산 설계

점수는 단순 정수 가산이 아니라 계산 파이프라인을 둔다.

기본 식 예시:

```text
최종 점수 =
기본 점수
x 거리 배수
x 부위 배수
x 현재 배수
x 시너지 보정
+ 즉시 보너스
```

예시 컨텍스트:

```csharp
public struct TargetScoreContext
{
    public Target Target;
    public float Distance;
    public float HitboxMultiplier;
    public bool IsCritical;
    public bool IsSpecialTrigger;
}
```

## 10. 돌발 이벤트 설계

라운드 시작 전에 1개의 돌발 이벤트를 선택 적용한다.

예시:

- 모든 과녁 크기 감소
- 특수 과녁 출현율 증가
- 일정 시간 화면 흔들림
- 좌우 반전
- 빠른 타겟만 등장
- 보너스 타겟 대량 등장

돌발 이벤트는 `ModifierDefinition` 기반 데이터로 관리한다.

## 11. 상점 설계

상점은 라운드 종료 후 진입한다.

아이템 타입 예시:

- `Multiplier Upgrade`
  기본 점수 배수 증가
- `Critical Upgrade`
  약점 적중 보너스 증가
- `Economy Upgrade`
  재화 획득량 증가
- `Spawn Manipulation`
  특정 과녁 출현율 증가
- `Special Synergy`
  특정 과녁 연계 보너스 추가

상점 설계 원칙:

- 한 아이템은 명확한 효과를 가진다.
- 수치는 작아도 조합 시 강력해야 한다.
- 플레이어가 점수 빌드를 "조립"하는 느낌이 들어야 한다.

## 12. 데이터 설계

데이터는 가능하면 `ScriptableObject`로 관리한다.

## 12.1 TargetDefinition

```csharp
[CreateAssetMenu]
public class TargetDefinition : ScriptableObject
{
    public string Id;
    public GameObject Prefab;
    public int BaseScore;
    public int MaxHealth;
    public float Lifetime;
    public float SpawnWeight;
    public bool IsSpecialTarget;
}
```

## 12.2 ShopItemDefinition

```csharp
[CreateAssetMenu]
public class ShopItemDefinition : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public string Description;
    public int Cost;
    public ShopItemType ItemType;
    public float Value;
}
```

## 12.3 ModifierDefinition

```csharp
[CreateAssetMenu]
public class ModifierDefinition : ScriptableObject
{
    public string Id;
    public string Title;
    public string Description;
    public ModifierType ModifierType;
    public float Value;
    public bool IsTemporary;
}
```

## 13. 이벤트 구조

기존 FPS의 `EventManager` 패턴은 유지 가능하다.

새 게임에서 필요한 주요 이벤트 예시:

- `RoundStartedEvent`
- `RoundEndedEvent`
- `TargetSpawnedEvent`
- `TargetDestroyedEvent`
- `TargetHitEvent`
- `ScoreChangedEvent`
- `CurrencyChangedEvent`
- `ShopOpenedEvent`
- `ItemPurchasedEvent`
- `GameStateChangedEvent`

## 14. MVP 범위

첫 번째 플레이 가능한 버전은 아래만 구현한다.

### 14.1 MVP 목표

- 고정형 카메라 조준
- 마우스 클릭 발사
- 기본 과녁 생성/피격/삭제
- 점수 획득
- 라운드 진행
- 목표 점수 체크
- 간단한 상점

### 14.2 MVP 제외 항목

- 복잡한 시너지
- 특수 이펙트 다수
- 많은 과녁 종류
- 메타 진행
- 저장 시스템

## 15. 개발 순서

추천 구현 순서:

1. `AimController`
2. `PlayerShooter`
3. `Target`
4. `TargetManager`
5. `ScoreManager`
6. `RoundManager`
7. `UIManager`
8. `ShopManager`
9. `ModifierManager`

이 순서를 따르면 가장 빠르게 "재미 여부"를 검증할 수 있다.

## 16. 기존 프로젝트와의 연결 원칙

재사용 권장:

- EventManager 패턴
- 일부 UI 토스트/메시지 구조
- AudioManager / AudioUtility
- WeaponController 일부 로직
- Input System 연결부

대체 또는 신규 구현 권장:

- PlayerCharacterController
- Enemy AI 전체
- NavMesh 기반 시스템
- Pickup 중심 구조
- Objective 중심 승리 구조

## 17. 구현 시 주의점

- 기존 FPS 씬과 새 게임 씬은 분리한다.
- 기존 스크립트에 새 기획 로직을 억지로 끼워 넣지 않는다.
- 점수 계산은 처음부터 데이터 중심으로 설계한다.
- 라운드 수치와 상점 수치는 코드 하드코딩보다 데이터 에셋으로 뺀다.
- MVP 단계에서는 "손맛"과 "점수 상승 쾌감" 검증이 최우선이다.

## 18. 다음 액션

이 문서를 기준으로 바로 시작할 작업은 아래 순서다.

1. `Assets/GameAimHigh/` 폴더 구조 생성
2. `MainGame.unity` 씬 생성
3. `GameManager`, `RoundManager`, `ScoreManager` 골격 생성
4. `AimController`, `PlayerShooter` 구현
5. `BasicTarget`, `TargetManager` 구현
6. 점수 HUD 연결

이후 플레이 가능한 첫 프로토타입을 만든 뒤, 상점과 시너지 시스템을 확장한다.
