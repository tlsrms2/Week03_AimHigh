using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.FPS.Game
{
    public class GameFlowManager : MonoBehaviour
    {
        [Header("Parameters")]
        [Tooltip("Duration of the fade-to-black at the end of the game")]
        public float EndSceneLoadDelay = 3f;

        [Tooltip("The canvas group of the fade-to-black screen")]
        public CanvasGroup EndGameFadeCanvasGroup;

        [Header("Win")]
        [Tooltip("This string has to be the name of the scene you want to load when winning")]
        public string WinSceneName = "WinScene";

        [Tooltip("Duration of delay before the fade-to-black, if winning")]
        public float DelayBeforeFadeToBlack = 4f;

        [Tooltip("Win game message")]
        public string WinGameMessage;

        [Tooltip("Duration of delay before the win message")]
        public float DelayBeforeWinMessage = 2f;

        [Tooltip("Sound played on win")]
        public AudioClip VictorySound;

        [Header("Lose")]
        [Tooltip("This string has to be the name of the scene you want to load when losing")]
        public string LoseSceneName = "LoseScene";

        [Header("Rules")]
        [Tooltip("Whether completing objectives should trigger victory")]
        public bool UseObjectiveVictory = true;

        [Tooltip("Whether player death should trigger defeat")]
        public bool UsePlayerDeathLose = true;

        [Header("Aim High")]
        [Tooltip("Enables round-based Aim High flow")]
        public bool UseAimHighFlow;

        [Tooltip("Delay before a round starts")]
        public float AimHighDelayBeforeRoundStart = 2f;

        [Tooltip("Delay before the next round after success")]
        public float AimHighDelayBeforeNextRound = 3f;

        [Tooltip("Duration of each shooting round")]
        public float AimHighRoundDuration = 20f;

        [Tooltip("Base quota for round 1")]
        public int AimHighBaseQuota = 100;

        [Tooltip("Quota growth per round")]
        public float AimHighQuotaGrowth = 1.75f;

        [Tooltip("Reference to the score manager")]
        public AimHighScoreManager AimHighScoreManager;

        [Tooltip("Target spawner behaviours implementing IAimHighTargetSpawner")]
        public MonoBehaviour[] AimHighTargetSpawners;

        [Tooltip("Reference to the shop manager")]
        public AimHighShopManager AimHighShopManager;

        [Tooltip("Delay after the shop message before opening the shop panel")]
        public float AimHighDelayBeforeShopOpen = 3f;

        [Tooltip("Disables passive ammo refill while using Aim High flow")]
        public bool DisablePassiveAmmoRefillInAimHigh = true;

        [Tooltip("Default bullets loaded in the magazine for Aim High")]
        public int AimHighDefaultClipSize = 30;

        [Tooltip("Default reserve ammo available in Aim High")]
        public int AimHighDefaultReserveAmmo = 90;

        public bool GameIsEnding { get; private set; }
        public int AimHighCurrentRoundIndex { get; private set; }
        public int AimHighCurrentQuota { get; private set; }
        public float AimHighTimeRemaining { get; private set; }
        public bool AimHighRoundActive { get; private set; }

        float m_TimeLoadEndGameScene;
        string m_SceneToLoad;
        int m_ScheduledAimHighRoundIndex;
        IAimHighTargetSpawner[] m_AimHighSpawnerCache = new IAimHighTargetSpawner[0];
        IAimHighWeaponInventory m_AimHighWeaponInventory;

        void Awake()
        {
            EventManager.AddListener<AllObjectivesCompletedEvent>(OnAllObjectivesCompleted);
            EventManager.AddListener<PlayerDeathEvent>(OnPlayerDeath);
        }

        void Start()
        {
            AudioUtility.SetMasterVolume(1);

            if (UseAimHighFlow)
            {
                if (AimHighScoreManager == null)
                {
                    AimHighScoreManager = FindFirstObjectByType<AimHighScoreManager>();
                }

                if (AimHighShopManager == null)
                {
                    AimHighShopManager = FindFirstObjectByType<AimHighShopManager>();
                }

                MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                for (int i = 0; i < allBehaviours.Length; i++)
                {
                    if (allBehaviours[i] is IAimHighWeaponInventory inventory)
                    {
                        m_AimHighWeaponInventory = inventory;
                        break;
                    }
                }

                if (DisablePassiveAmmoRefillInAimHigh && m_AimHighWeaponInventory != null)
                {
                    m_AimHighWeaponInventory.SetPassiveAmmoRefill(false);
                }

                if (m_AimHighWeaponInventory != null)
                {
                    m_AimHighWeaponInventory.PrepareAimHighWeapon(AimHighDefaultClipSize, AimHighDefaultReserveAmmo);
                }

                CacheAimHighSpawners();
                ScheduleAimHighRound(1, AimHighDelayBeforeRoundStart);
            }
        }

        void Update()
        {
            UpdateAimHighFlow();

            if (GameIsEnding)
            {
                float timeRatio = 1 - (m_TimeLoadEndGameScene - Time.time) / EndSceneLoadDelay;
                EndGameFadeCanvasGroup.alpha = timeRatio;
                AudioUtility.SetMasterVolume(1 - timeRatio);

                if (Time.time >= m_TimeLoadEndGameScene)
                {
                    SceneManager.LoadScene(m_SceneToLoad);
                    GameIsEnding = false;
                }
            }
        }

        void UpdateAimHighFlow()
        {
            if (!UseAimHighFlow || !AimHighRoundActive || GameIsEnding)
            {
                return;
            }

            AimHighTimeRemaining -= Time.deltaTime;

            AimHighQuotaChangedEvent quotaChangedEvent = Events.AimHighQuotaChangedEvent;
            quotaChangedEvent.RoundIndex = AimHighCurrentRoundIndex;
            quotaChangedEvent.QuotaProgress = AimHighScoreManager != null ? AimHighScoreManager.CurrentRoundQuotaProgress : 0;
            quotaChangedEvent.Quota = AimHighCurrentQuota;
            quotaChangedEvent.TimeRemaining = Mathf.Max(0f, AimHighTimeRemaining);
            EventManager.Broadcast(quotaChangedEvent);

            if (AimHighTimeRemaining <= 0f)
            {
                FinishAimHighRound();
            }
        }

        void CacheAimHighSpawners()
        {
            if (AimHighTargetSpawners == null)
            {
                m_AimHighSpawnerCache = new IAimHighTargetSpawner[0];
                return;
            }

            m_AimHighSpawnerCache = new IAimHighTargetSpawner[AimHighTargetSpawners.Length];
            for (int i = 0; i < AimHighTargetSpawners.Length; i++)
            {
                m_AimHighSpawnerCache[i] = AimHighTargetSpawners[i] as IAimHighTargetSpawner;
            }
        }

        void OnAllObjectivesCompleted(AllObjectivesCompletedEvent evt)
        {
            if (UseObjectiveVictory)
            {
                EndGame(true);
            }
        }

        void OnPlayerDeath(PlayerDeathEvent evt)
        {
            if (UsePlayerDeathLose)
            {
                EndGame(false);
            }
        }

        public void ScheduleAimHighRound(int roundIndex, float delay)
        {
            CancelInvoke(nameof(BeginScheduledAimHighRound));
            m_ScheduledAimHighRoundIndex = Mathf.Max(1, roundIndex);
            Invoke(nameof(BeginScheduledAimHighRound), delay);
        }

        void BeginScheduledAimHighRound()
        {
            StartAimHighRound(m_ScheduledAimHighRoundIndex);
        }

        public void StartAimHighRound(int roundIndex)
        {
            if (!UseAimHighFlow)
            {
                return;
            }

            AimHighCurrentRoundIndex = Mathf.Max(1, roundIndex);
            AimHighCurrentQuota =
                Mathf.RoundToInt(AimHighBaseQuota * Mathf.Pow(AimHighQuotaGrowth, AimHighCurrentRoundIndex - 1));
            AimHighTimeRemaining = AimHighRoundDuration;
            AimHighRoundActive = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (m_AimHighWeaponInventory != null)
            {
                m_AimHighWeaponInventory.RefillWeaponsForNewRound();
            }

            if (AimHighScoreManager != null)
            {
                AimHighScoreManager.ResetRoundScore();
            }

            for (int i = 0; i < m_AimHighSpawnerCache.Length; i++)
            {
                if (m_AimHighSpawnerCache[i] != null)
                {
                    m_AimHighSpawnerCache[i].ClearTargets();
                    m_AimHighSpawnerCache[i].Begin();
                }
            }

            AimHighRoundStartedEvent roundStartedEvent = Events.AimHighRoundStartedEvent;
            roundStartedEvent.RoundIndex = AimHighCurrentRoundIndex;
            roundStartedEvent.Duration = AimHighRoundDuration;
            roundStartedEvent.Quota = AimHighCurrentQuota;
            EventManager.Broadcast(roundStartedEvent);

            DisplayMessageEvent displayMessageEvent = Events.DisplayMessageEvent;
            displayMessageEvent.Message = $"Round {AimHighCurrentRoundIndex} - Quota {AimHighCurrentQuota}";
            displayMessageEvent.DelayBeforeDisplay = 0f;
            EventManager.Broadcast(displayMessageEvent);
        }

        public void FinishAimHighRound()
        {
            if (!UseAimHighFlow || !AimHighRoundActive)
            {
                return;
            }

            AimHighRoundActive = false;
            for (int i = 0; i < m_AimHighSpawnerCache.Length; i++)
            {
                if (m_AimHighSpawnerCache[i] != null)
                {
                    m_AimHighSpawnerCache[i].Stop();
                }
            }

            int roundScore = AimHighScoreManager != null ? AimHighScoreManager.CurrentRoundScore : 0;
            int roundQuotaProgress = AimHighScoreManager != null ? AimHighScoreManager.CurrentRoundQuotaProgress : 0;
            bool success = roundQuotaProgress >= AimHighCurrentQuota;

            AimHighRoundEndedEvent roundEndedEvent = Events.AimHighRoundEndedEvent;
            roundEndedEvent.RoundIndex = AimHighCurrentRoundIndex;
            roundEndedEvent.Success = success;
            roundEndedEvent.Score = roundScore;
            roundEndedEvent.QuotaProgress = roundQuotaProgress;
            roundEndedEvent.Quota = AimHighCurrentQuota;
            EventManager.Broadcast(roundEndedEvent);

            DisplayMessageEvent displayMessageEvent = Events.DisplayMessageEvent;
            displayMessageEvent.Message = success
                ? $"Round {AimHighCurrentRoundIndex} clear"
                : $"Run over - quota {roundQuotaProgress}/{AimHighCurrentQuota}";
            displayMessageEvent.DelayBeforeDisplay = 0f;
            EventManager.Broadcast(displayMessageEvent);

            if (success)
            {
                DisplayMessageEvent shopDisplayMessageEvent = Events.DisplayMessageEvent;
                shopDisplayMessageEvent.Message = "Shop Time";
                shopDisplayMessageEvent.DelayBeforeDisplay = 0f;
                EventManager.Broadcast(shopDisplayMessageEvent);

                if (AimHighShopManager != null)
                {
                    AimHighShopManager.BeginShopPhase(AimHighCurrentRoundIndex + 1, AimHighDelayBeforeShopOpen);
                }
                else
                {
                    ScheduleAimHighRound(AimHighCurrentRoundIndex + 1, AimHighDelayBeforeNextRound);
                }
            }
            else
            {
                TriggerLose();
            }
        }

        public void TriggerWin()
        {
            if (!GameIsEnding)
            {
                EndGame(true);
            }
        }

        public void TriggerLose()
        {
            if (!GameIsEnding)
            {
                EndGame(false);
            }
        }

        void EndGame(bool win)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GameIsEnding = true;
            EndGameFadeCanvasGroup.gameObject.SetActive(true);
            if (win)
            {
                m_SceneToLoad = WinSceneName;
                m_TimeLoadEndGameScene = Time.time + EndSceneLoadDelay + DelayBeforeFadeToBlack;

                var audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.clip = VictorySound;
                audioSource.playOnAwake = false;
                audioSource.outputAudioMixerGroup = AudioUtility.GetAudioGroup(AudioUtility.AudioGroups.HUDVictory);
                audioSource.PlayScheduled(AudioSettings.dspTime + DelayBeforeWinMessage);

                DisplayMessageEvent displayMessage = Events.DisplayMessageEvent;
                displayMessage.Message = WinGameMessage;
                displayMessage.DelayBeforeDisplay = DelayBeforeWinMessage;
                EventManager.Broadcast(displayMessage);
            }
            else
            {
                m_SceneToLoad = LoseSceneName;
                m_TimeLoadEndGameScene = Time.time + EndSceneLoadDelay;
            }
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<AllObjectivesCompletedEvent>(OnAllObjectivesCompleted);
            EventManager.RemoveListener<PlayerDeathEvent>(OnPlayerDeath);
        }
    }
}
