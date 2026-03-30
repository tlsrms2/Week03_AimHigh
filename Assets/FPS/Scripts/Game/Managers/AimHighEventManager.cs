using System;
using UnityEngine;

namespace Unity.FPS.Game
{
    public enum AimHighEventType
    {
        None,
        Wager,
        Crisis,
        Trial
    }

    public class AimHighEventManager : MonoBehaviour
    {
        [Header("Event Probabilities")]
        [Tooltip("Base probability for any event to happen (0-1)")]
        [Range(0f, 1f)] public float EventTriggerChance = 0.3f;

        [Tooltip("Minimum round index required for any random event to trigger")]
        public int MinRoundForEvents = 3;
        
        [Tooltip("Relative weight for Wager event")]
        public float WagerWeight = 1f;

        [Tooltip("Relative weight for Crisis event (formerly Trial)")]
        public float CrisisWeight = 1f;

        [Tooltip("Relative weight for the NEW Trial event (Hunt a specific target)")]
        public float TrialWeight = 1f;

        [Header("Contract Settings")]
        public Color ContractSubtitleColor = new Color(1f, 0.5f, 0f, 1f); // Orange

        public AimHighShopManager.ContractType ActiveContractType { get; private set; }
        public float ActiveContractPenalty { get; private set; }
        public float ActiveContractReward { get; private set; }
        public bool HasActiveContract => ActiveContractType != AimHighShopManager.ContractType.None;
        public int LastContractRewardAmount { get; private set; }

        [Header("Wager Settings")]
        [Tooltip("Time limit to complete round quota")]
        public float WagerTimeLimit = 10f;
        
        [Tooltip("Percentage of current money to gain on success")]
        [Range(0f, 1f)] public float WagerRewardPercentage = 0.2f;

        [Tooltip("Percentage of current money to lose on failure")]
        [Range(0f, 1f)] public float WagerPenaltyPercentage = 0.5f;

        [Header("Crisis Settings")]
        [Tooltip("Money penalty per missed target")]
        public int CrisisPenaltyPerMiss = 10;
        
        [Tooltip("Minimum spawn interval multiplier during crisis")]
        public float CrisisMinSpawnMultiplier = 0.3f;

        [Tooltip("How fast the spawn interval decreases (Multiplier goes from 1 to Min over this time)")]
        public float CrisisDifficultyRampTime = 20f;

        [Header("Trial Settings (New)")]
        [Tooltip("The enemy target prefab to hunt during the Trial")]
        public GameObject TrialTargetPrefab;

        [Tooltip("Optional: Specific spawn volume for the Trial target. If null, a random volume from active spawners will be used.")]
        public BoxCollider TrialSpawnVolume;

        [Tooltip("The weapon granted for the next round if the player kills the Trial Target")]
        public AimHighWeaponController TrialRewardWeaponPrefab;

        [Header("Event Dialogues")]
        [TextArea(3, 5)]
        public string WagerIntroMessage = "내기를 시작하지.\n{0}초 안에 할당량을 채워보라고.\n실패하면 소지금의 {1}%를 잃는다!";
        [TextArea(3, 5)]
        public string CrisisIntroMessage = "위기가 닥쳤다! 타겟들이 점점 빨리 나올 것이다.\n타겟을 놓칠 때마다 돈을 잃게 될 거다!";
        [TextArea(3, 5)]
        public string TrialIntroMessage = "시련이 다가온다.\n특별한 적을 처치하면 다음 라운드 동안 특수 샷건을 보급하겠다.";

        [Header("Contract Intro Messages")]
        [TextArea(3, 5)]
        public string GreedIntroMessage = "자신만만한 모양이군.\n할당량이 늘어난 이번 라운드를 버텨내면 큰 보상을 주지.";
        [TextArea(3, 5)]
        public string HurryIntroMessage = "서둘러야 할 거다!\n더 짧아진 시간 안에 목표를 달성해라.";
        [TextArea(3, 5)]
        public string ChunkyIntroMessage = "무기가 무겁게 느껴질 거다.\n느려진 장전 속도를 이겨내고 승리해라.";

        public bool IsEventActive { get; private set; }
        public AimHighEventType CurrentEventType { get; private set; }
        
        // Expose Wager data for UI
        public float WagerTimeRemaining { get; private set; }
        public float WagerTimeLimitTotal => WagerTimeLimit;
        public int WagerTargetQuota { get; private set; }
        public int WagerCurrentQuotaProgress { get; private set; }
        public bool WagerCompleted => m_WagerCompleted;
        public bool WagerSuccess { get; private set; }
        public int LastWagerResultAmount { get; private set; }

        public event Action EventStateChanged;
        
        // Trial specific states
        public bool TrialTargetKilled { get; private set; }
        public bool TrialCompleted { get; private set; }

        GameFlowManager m_GameFlowManager;
        AimHighScoreManager m_ScoreManager;
        
        int m_PendingRoundIndex;
        int m_StoredCurrency;
        bool m_WagerCompleted;
        float m_CrisisStartTime;
        
        GameObject m_SpawnedTrialTarget;
        AimHighWeaponController m_GrantedTrialWeaponInstance;
        int m_WeaponRemovalRoundIndex = -1;
        bool m_TrialRewardEarned = false;
        bool m_PendingContractCleanup = false;

        void Awake()
        {
            m_GameFlowManager = FindFirstObjectByType<GameFlowManager>();
            m_ScoreManager = FindFirstObjectByType<AimHighScoreManager>();

            EventManager.AddListener<AimHighRoundStartedEvent>(OnRoundStarted);
            EventManager.AddListener<AimHighRoundEndedEvent>(OnRoundEnded);
        }

        public bool TryStartEvent(int roundIndex)
        {
            if (roundIndex < MinRoundForEvents)
            {
                return false;
            }

            if (HasActiveContract)
            {
                return false;
            }

            if (UnityEngine.Random.value > EventTriggerChance)
            {
                return false;
            }

            IsEventActive = true;
            m_PendingRoundIndex = roundIndex;

            // Broadcast early for HUD visibility
            AimHighRoundStartedEvent earlyStartEvent = Events.AimHighRoundStartedEvent;
            earlyStartEvent.RoundIndex = roundIndex;
            EventManager.Broadcast(earlyStartEvent);

            CurrentEventType = RollEventType();

            string message = "<color=red>돌발 이벤트 발생</color>";
            
            DisplayMessageEvent displayEvent = Events.DisplayMessageEvent;
            displayEvent.DelayBeforeDisplay = 0f;
            displayEvent.Message = message;
            EventManager.Broadcast(displayEvent);

            Invoke(nameof(StartPendingRound), 6f);
            
            EventStateChanged?.Invoke();
            return true;
        }

        public void ScheduleRoundIntro(int roundIndex)
        {
            m_PendingRoundIndex = roundIndex;

            // 1. Calculate quota early so HUD and Objectives can show info during dialogue
            int predictedQuota = 0;
            if (m_GameFlowManager != null)
            {
                predictedQuota = m_GameFlowManager.PredictRoundQuota(roundIndex);
                predictedQuota = Mathf.RoundToInt(predictedQuota * GetContractQuotaMultiplier());
            }

            // Broadcast early with rich info to wake up HUD and ObjectiveToast
            AimHighRoundStartedEvent earlyStartEvent = Events.AimHighRoundStartedEvent;
            earlyStartEvent.RoundIndex = roundIndex;
            earlyStartEvent.Quota = predictedQuota;
            EventManager.Broadcast(earlyStartEvent);

            AimHighQuotaChangedEvent earlyQuotaEvent = Events.AimHighQuotaChangedEvent;
            earlyQuotaEvent.Quota = predictedQuota;
            earlyQuotaEvent.QuotaProgress = 0;
            earlyQuotaEvent.TimeRemaining = m_GameFlowManager ? m_GameFlowManager.AimHighRoundDuration : 60f;
            EventManager.Broadcast(earlyQuotaEvent);

            // 2. Check for Random Events first
            bool hasRandomEvent = TryStartEvent(roundIndex);
            if (hasRandomEvent) return; 

            // 3. Check for Contracts if no random event
            if (HasActiveContract)
            {
                DisplayMessageEvent displayEvent = Events.DisplayMessageEvent;
                displayEvent.DelayBeforeDisplay = 0f;
                displayEvent.Message = "<color=red>Chaos 이벤트 발생</color>";
                EventManager.Broadcast(displayEvent);
                Invoke(nameof(StartPendingRound), 6f);
                return;
            }

            // 4. No special dialogue, just start directly (but GameFlowManager will also do 5s prep)
            StartPendingRound();
        }

        void StartPendingRound()
        {
            if (m_GameFlowManager) m_GameFlowManager.StartAimHighRound(m_PendingRoundIndex);
        }

        AimHighEventType RollEventType()
        {
            float total = WagerWeight + CrisisWeight + TrialWeight;
            if (total <= 0f) return AimHighEventType.None;
            
            float roll = UnityEngine.Random.Range(0f, total);
            if (roll < WagerWeight) return AimHighEventType.Wager;
            roll -= WagerWeight;
            if (roll < CrisisWeight) return AimHighEventType.Crisis;
            return AimHighEventType.Trial;
        }

        void OnRoundStarted(AimHighRoundStartedEvent evt)
        {
            // 0. Cleanup previous contract if pending
            if (m_PendingContractCleanup)
            {
                ActiveContractType = AimHighShopManager.ContractType.None;
                ActiveContractPenalty = 0f;
                ActiveContractReward = 0f;
                // We keep LastContractRewardAmount until the next reward is calculated 
                // so visualizers can still see what was earned in the summary.
                m_PendingContractCleanup = false;
            }

            // 1. Check if we reached the removal round
            if (m_WeaponRemovalRoundIndex > 0 && evt.RoundIndex >= m_WeaponRemovalRoundIndex)
            {
                var pm = FindInterface<IAimHighWeaponInventory>();
                if (pm != null && m_GrantedTrialWeaponInstance != null)
                {
                    pm.RemoveWeapon(m_GrantedTrialWeaponInstance);
                }
                m_GrantedTrialWeaponInstance = null;
                m_WeaponRemovalRoundIndex = -1;
            }

            // 2. Grant weapon if trial was WON in previous round
            if (m_TrialRewardEarned && TrialRewardWeaponPrefab != null)
            {
                var pm = FindInterface<IAimHighWeaponInventory>();
                if (pm != null)
                {
                    if (pm.AddWeapon(TrialRewardWeaponPrefab))
                    {
                        m_GrantedTrialWeaponInstance = pm.HasWeapon(TrialRewardWeaponPrefab);
                        if (m_GrantedTrialWeaponInstance != null)
                        {
                            pm.SwitchToWeapon(m_GrantedTrialWeaponInstance);
                            // It will be removed at the start of the round AFTER this one
                            m_WeaponRemovalRoundIndex = evt.RoundIndex + 1;
                        }
                    }
                }
                m_TrialRewardEarned = false;
            }

            if (!IsEventActive) return;

            if (CurrentEventType == AimHighEventType.Wager)
            {
                WagerTimeRemaining = WagerTimeLimit;
                WagerTargetQuota = evt.Quota;
                WagerCurrentQuotaProgress = 0;
                m_WagerCompleted = false;
                WagerSuccess = false;
                m_StoredCurrency = m_ScoreManager ? m_ScoreManager.Currency : 0;
                EventStateChanged?.Invoke();
            }
            else if (CurrentEventType == AimHighEventType.Crisis)
            {
                m_CrisisStartTime = Time.time;
            }
            else if (CurrentEventType == AimHighEventType.Trial)
            {
                TrialTargetKilled = false;
                TrialCompleted = false;
                if (TrialTargetPrefab != null)
                {
                    SpawnTrialTarget();
                }
            }
        }
        
        void SpawnTrialTarget()
        {
            BoxCollider activeVolume = TrialSpawnVolume;
            
            // Fallback: search for any spawner and pick a volume
            if (activeVolume == null)
            {
                var spawner = FindInterface<IAimHighTargetSpawner>();
                if (spawner != null)
                {
                    activeVolume = spawner.GetRandomAvailableSpawnVolume();
                }
            }

            Vector3 spawnPos = transform.position + Vector3.forward * 5f;
            Quaternion spawnRot = Quaternion.identity;

            if (activeVolume != null)
            {
                spawnRot = activeVolume.transform.rotation;
                Vector3 localOffset = new Vector3(
                    UnityEngine.Random.Range(-activeVolume.size.x * 0.5f, activeVolume.size.x * 0.5f),
                    UnityEngine.Random.Range(-activeVolume.size.y * 0.5f, activeVolume.size.y * 0.5f),
                    UnityEngine.Random.Range(-activeVolume.size.z * 0.5f, activeVolume.size.z * 0.5f));

                spawnPos = activeVolume.transform.TransformPoint(activeVolume.center + localOffset);
            }

            m_SpawnedTrialTarget = Instantiate(TrialTargetPrefab, spawnPos, spawnRot);
            
            // Ensure the target knows its volume so its movement script works (MovingTarget, SidewaysTarget, etc.)
            if (activeVolume != null)
            {
                foreach(var aware in m_SpawnedTrialTarget.GetComponentsInChildren<IAimHighSpawnVolumeAware>())
                {
                    aware.SetSpawnVolume(activeVolume);
                }
            }

            // Set up listener for trial target death
            if (m_SpawnedTrialTarget.TryGetComponent(out Health targetHealth))
            {
                targetHealth.OnDie += OnTrialTargetDied;
            }
        }
        
        void OnTrialTargetDied()
        {
            TrialTargetKilled = true;
            CompleteTrial();
        }

        public bool InhibitRegularSpawning => IsEventActive && CurrentEventType == AimHighEventType.Trial;

        void CompleteTrial()
        {
            if (TrialCompleted) return;
            TrialCompleted = true;
            
            if (TrialTargetKilled)
            {
                m_TrialRewardEarned = true;
            }
            
            EventStateChanged?.Invoke();

            if (m_GameFlowManager != null)
            {
                m_GameFlowManager.FinishAimHighRound();
            }
        }

        T FindInterface<T>() where T : class
        {
            MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (int i = 0; i < allBehaviours.Length; i++)
            {
                if (allBehaviours[i] is T interfaceImpl)
                {
                    return interfaceImpl;
                }
            }
            return null;
        }

        void Update()
        {
            if (!IsEventActive || m_GameFlowManager == null || !m_GameFlowManager.AimHighRoundActive)
                return;

            if (CurrentEventType == AimHighEventType.Wager && !m_WagerCompleted)
            {
                WagerTimeRemaining -= Time.deltaTime;
                WagerCurrentQuotaProgress = m_ScoreManager ? m_ScoreManager.CurrentRoundQuotaProgress : 0;

                if (WagerCurrentQuotaProgress >= WagerTargetQuota)
                {
                    CompleteWager(true);
                }
                else if (WagerTimeRemaining <= 0f)
                {
                    CompleteWager(false);
                }
            }
        }

        void CompleteWager(bool success)
        {
            m_WagerCompleted = true;
            WagerSuccess = success;
            WagerTimeRemaining = 0f;

            if (m_ScoreManager != null)
            {
                float ratio = success ? WagerRewardPercentage : WagerPenaltyPercentage;
                int amount = Mathf.RoundToInt(m_ScoreManager.Currency * ratio);
                LastWagerResultAmount = amount;
                if (success)
                {
                    m_ScoreManager.AddDirectCurrency(amount);
                }
                else
                {
                    m_ScoreManager.TrySpendCurrency(amount);
                }
            }
            
            // End the event immediately as per user request
            IsEventActive = false;
            CurrentEventType = AimHighEventType.None;

            EventStateChanged?.Invoke();
        }

        public void ReportMissedTarget(Vector3 worldPosition)
        {
            if (!IsEventActive || CurrentEventType != AimHighEventType.Crisis) return;

            if (m_ScoreManager)
            {
                m_ScoreManager.SubtractCurrency(CrisisPenaltyPerMiss);
                
                // Always show the full penalty amount as red text to indicate the miss penalty.
                // Red "-[Penalty]" will appear where the target was.
                AimHighFloatingText.Spawn($"-{CrisisPenaltyPerMiss}", worldPosition + Vector3.up * 1.5f, Color.red);
            }
        }

        public float GetCrisisSpawnMultiplier()
        {
            if (!IsEventActive || CurrentEventType != AimHighEventType.Crisis)
                return 1f;

            float elapsed = Time.time - m_CrisisStartTime;
            float t = Mathf.Clamp01(elapsed / CrisisDifficultyRampTime);
            return Mathf.Lerp(1f, CrisisMinSpawnMultiplier, t);
        }

        public int GetCurrentContractRewardAmount()
        {
            if (!HasActiveContract || m_ScoreManager == null) return 0;
            // Use CeilToInt to ensure that any positive reward percentage results in at least 1 gold
            return Mathf.CeilToInt(m_ScoreManager.Currency * ActiveContractReward);
        }

        public void OnRoundEnded(AimHighRoundEndedEvent evt)
        {
            if (HasActiveContract && evt.Success)
            {
                if (m_ScoreManager != null)
                {
                    int rewardAmount = GetCurrentContractRewardAmount();
                    m_ScoreManager.AddDirectCurrency(rewardAmount);
                    LastContractRewardAmount = rewardAmount;
                }
            }
            
            // Note: We no longer clear contracts here, to let HUD process rewards.
            // They will be cleared when the next round starts in OnRoundStarted.

            if (HasActiveContract)
            {
                m_PendingContractCleanup = true;
            }

            if (!IsEventActive) return;
            
            if (CurrentEventType == AimHighEventType.Wager && !m_WagerCompleted)
            {
                CompleteWager(false);
            }
            else if (CurrentEventType == AimHighEventType.Trial && !TrialCompleted)
            {
                // Round ended and target wasn't killed
                CompleteTrial();
            }

            IsEventActive = false;
            CurrentEventType = AimHighEventType.None;

            EventStateChanged?.Invoke();
        }


        public void ActivateContract(AimHighShopManager.ContractType type, float penalty, float reward)
        {
            ActiveContractType = type;
            ActiveContractPenalty = penalty;
            ActiveContractReward = reward;
            EventStateChanged?.Invoke();
        }

        public float GetContractQuotaMultiplier()
        {
            if (ActiveContractType == AimHighShopManager.ContractType.Greed) return 1f + ActiveContractPenalty;
            return 1f;
        }

        public float ApplyContractTimePenalty(float baseTime)
        {
            if (ActiveContractType == AimHighShopManager.ContractType.Hurry) return Mathf.Max(5f, baseTime - ActiveContractPenalty);
            return baseTime;
        }

        public float ApplyContractReloadPenalty(float baseReloadDuration)
        {
            if (ActiveContractType == AimHighShopManager.ContractType.Chunky) return baseReloadDuration + ActiveContractPenalty;
            return baseReloadDuration;
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<AimHighRoundStartedEvent>(OnRoundStarted);
            EventManager.RemoveListener<AimHighRoundEndedEvent>(OnRoundEnded);
        }
    }
}
