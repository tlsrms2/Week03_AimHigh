using Unity.FPS.Game;
using UnityEngine;
using TMPro;

namespace Unity.FPS.UI
{
    public class AimHighEventHUDManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("ObjectiveToast view used to display event information")]
        public ObjectiveToast EventView;

        [Header("Toast Descriptions (Small UI)")]
        [TextArea(1, 2)] public string WagerToastDescription = "Complete quota quickly!";
        [TextArea(1, 2)] public string CrisisToastDescription = "Fast targets! Don't miss!";
        [TextArea(1, 2)] public string TrialToastDescription = "Hunt the special target!";

        [Header("Contract Toast Descriptions")]
        [TextArea(1, 2)] public string GreedToastDescription = "Survive the heavy quota!";
        [TextArea(1, 2)] public string HurryToastDescription = "Be quick! Time is short.";
        [TextArea(1, 2)] public string ChunkyToastDescription = "Manage your slow reloads!";

        [Header("Dialogue (Separate Large Text)")]
        [Tooltip("Separate text element for displaying dialogue/descriptions")]
        public TMPro.TextMeshProUGUI SubtitleText;

        [Header("Contract Reward UI")]
        [Tooltip("Root object for the reward pop-up (displayed at round end success)")]
        public GameObject ContractRewardRoot;

        [Tooltip("Text element for showing the reward amount")]
        public TextMeshProUGUI ContractRewardAmountText; 

        AimHighEventManager m_EventManager;
        GameFlowManager m_GameFlowManager;
        ObjectiveToast m_CurrentToast;
        bool m_WagerResultShown;

        void Awake()
        {
            m_EventManager = FindFirstObjectByType<AimHighEventManager>();
            m_GameFlowManager = FindFirstObjectByType<GameFlowManager>();
            if (m_EventManager != null)
            {
                m_EventManager.EventStateChanged += OnEventStateChanged;
            }

            EventManager.AddListener<AimHighRoundEndedEvent>(OnRoundEnded);

            if (SubtitleText != null)
            {
                SubtitleText.text = "";
            }

            m_WagerResultShown = false;
            if (EventView != null)
            {
                EventView.gameObject.SetActive(false);
            }
        }

        void OnEventStateChanged()
        {
            if (m_EventManager == null || EventView == null) return;

            // Show if it's a random event (IsEventActive is true during intro dialogue as well)
            // Or if it's a contract, show during the entire preparation and round phase.
            bool activeEvent = m_EventManager.IsEventActive;
            bool activeContractInRound = m_EventManager.HasActiveContract;

            // Reset result shown flag when a new event/contract round starts
            if (activeEvent || activeContractInRound)
            {
                m_WagerResultShown = false;
            }

            if (activeEvent || activeContractInRound)
            {
                CancelInvoke(nameof(HideToast));
                EnsureToast();
                UpdateToastStaticContent();
            }
            else if (m_CurrentToast != null)
            {
                // If Wager just completed, show reward/penalty pop-up immediately
                if (m_EventManager.WagerCompleted && !m_WagerResultShown)
                {
                    m_WagerResultShown = true;
                    ShowMoneyNotification(m_EventManager.LastWagerResultAmount, m_EventManager.WagerSuccess);
                }

                Invoke(nameof(HideToast), 0f);
            }
        }

        void Update()
        {
            if (m_CurrentToast == null || m_EventManager == null)
                return;

            if (!m_EventManager.IsEventActive && !m_EventManager.HasActiveContract)
                return;

            float timeToShow = 0f;
            string counterText = "";

            if (m_EventManager.HasActiveContract)
            {
                timeToShow = m_GameFlowManager != null ? m_GameFlowManager.AimHighTimeRemaining : 0f;
                int progress = m_GameFlowManager != null && m_GameFlowManager.AimHighScoreManager != null 
                    ? m_GameFlowManager.AimHighScoreManager.CurrentRoundQuotaProgress : 0;
                int quota = m_GameFlowManager != null ? m_GameFlowManager.AimHighCurrentQuota : 0;
                counterText = $"$ {progress} / {quota}";
            }
            else if (m_EventManager.CurrentEventType == AimHighEventType.Wager)
            {
                if (m_EventManager.WagerCompleted)
                {
                    if (!m_WagerResultShown)
                    {
                        m_WagerResultShown = true;
                        ShowMoneyNotification(m_EventManager.LastWagerResultAmount, m_EventManager.WagerSuccess);
                    }
                    return;
                }
                
                timeToShow = m_EventManager.WagerTimeRemaining;
                counterText = $"$ {m_EventManager.WagerCurrentQuotaProgress} / {m_EventManager.WagerTargetQuota}";
            }
            else if (m_EventManager.CurrentEventType == AimHighEventType.Crisis)
            {
                timeToShow = m_GameFlowManager != null ? m_GameFlowManager.AimHighTimeRemaining : 0f;
                
                int progress = m_GameFlowManager != null && m_GameFlowManager.AimHighScoreManager != null 
                    ? m_GameFlowManager.AimHighScoreManager.CurrentRoundQuotaProgress : 0;
                int quota = m_GameFlowManager != null ? m_GameFlowManager.AimHighCurrentQuota : 0;
                counterText = $"$ {progress} / {quota}";
            }
            else if (m_EventManager.CurrentEventType == AimHighEventType.Trial)
            {
                if (m_EventManager.TrialCompleted)
                {
                    return;
                }

                timeToShow = m_GameFlowManager != null ? m_GameFlowManager.AimHighTimeRemaining : 0f;
                counterText = "TARGET";
            }

            m_CurrentToast.DescriptionTextContent.text = $"Time left {timeToShow:0.0}s";
            m_CurrentToast.CounterTextContent.text = counterText;
        }

        void OnRoundEnded(AimHighRoundEndedEvent evt)
        {
            if (evt.Success && m_EventManager != null && m_EventManager.HasActiveContract)
            {
                // Request the calculation directly from the event manager to ensure we get the latest value,
                // regardless of whether its listener has fired yet.
                int reward = m_EventManager.GetCurrentContractRewardAmount();
                ShowMoneyNotification(reward, true);
            }

            if (m_CurrentToast != null)
            {
                Invoke(nameof(HideToast), 0.5f);
            }
        }

        void ShowMoneyNotification(int amount, bool success)
        {
            if (ContractRewardRoot == null || ContractRewardAmountText == null) return;

            ContractRewardAmountText.text = success ? $"+{amount}" : $"-{amount}";
            ContractRewardAmountText.color = success ? Color.yellow : Color.red;
            ContractRewardRoot.SetActive(true);
            
            CancelInvoke(nameof(HideContractReward));
            Invoke(nameof(HideContractReward), 2f);
        }

        void HideContractReward()
        {
            if (ContractRewardRoot != null) ContractRewardRoot.SetActive(false);
        }

        void EnsureToast()
        {
            if (m_CurrentToast != null || EventView == null) return;

            m_CurrentToast = EventView;
            m_CurrentToast.gameObject.SetActive(true);
            
            if (m_CurrentToast.CanvasGroup != null) m_CurrentToast.CanvasGroup.alpha = 1f;
            if (m_CurrentToast.LayoutGroup != null) m_CurrentToast.LayoutGroup.padding.left = 0;
        }

        void HideToast()
        {
            if (m_CurrentToast != null)
            {
                m_CurrentToast.gameObject.SetActive(false);
                m_CurrentToast = null;
            }
            HideSubtitle();
        }

        void UpdateToastStaticContent()
        {
            if (m_CurrentToast == null) return;

            string title = "";
            string subtitle = "";

            if (m_EventManager.HasActiveContract)
            {
                switch (m_EventManager.ActiveContractType)
                {
                    case AimHighShopManager.ContractType.Greed:
                        title = "GREED CONTRACT";
                        subtitle = GreedToastDescription;
                        break;
                    case AimHighShopManager.ContractType.Hurry:
                        title = "HURRY CONTRACT";
                        subtitle = HurryToastDescription;
                        break;
                    case AimHighShopManager.ContractType.Chunky:
                        title = "CHUNKY CONTRACT";
                        subtitle = ChunkyToastDescription;
                        break;
                }
            }
            else
            {
                switch (m_EventManager.CurrentEventType)
                {
                    case AimHighEventType.Wager:
                        title = "WAGER";
                        subtitle = WagerToastDescription;
                        break;
                    case AimHighEventType.Crisis:
                        title = "CRISIS";
                        subtitle = CrisisToastDescription;
                        break;
                    case AimHighEventType.Trial:
                        title = "TRIAL";
                        subtitle = TrialToastDescription;
                        break;
                }
            }

            m_CurrentToast.TitleTextContent.text = title;
            m_CurrentToast.DescriptionTextContent.text = ""; 
            m_CurrentToast.CounterTextContent.text = "";
            ShowSubtitle(subtitle, 0f);
        }

        void ShowSubtitle(string text, float duration)
        {
            if (SubtitleText == null) return;
            
            CancelInvoke(nameof(HideSubtitle));
            SubtitleText.text = text;
            if (duration > 0f)
            {
                Invoke(nameof(HideSubtitle), duration);
            }
        }

        void HideSubtitle()
        {
            if (SubtitleText != null)
            {
                SubtitleText.text = "";
            }
        }

        void OnDestroy()
        {
            if (m_EventManager != null)
            {
                m_EventManager.EventStateChanged -= OnEventStateChanged;
            }

            EventManager.RemoveListener<AimHighRoundEndedEvent>(OnRoundEnded);
        }
    }
}
