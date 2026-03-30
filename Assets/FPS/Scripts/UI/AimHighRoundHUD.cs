using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.UI
{
    public class AimHighRoundHUD : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("ObjectiveToast view used to display regular round information")]
        public ObjectiveToast RoundView;

        [Header("Toast Description (Small UI)")]
        [TextArea(1, 2)] public string RoundToastDescription = "Reach the quota!";

        AimHighEventManager m_EventManager;
        ObjectiveToast m_CurrentToast;

        int m_CurrentRound;
        int m_CurrentQuota;
        int m_CurrentQuotaProgress;
        float m_TimeRemaining;

        void Awake()
        {
            if (RoundView != null)
            {
                RoundView.gameObject.SetActive(false);
            }
            
            m_EventManager = FindFirstObjectByType<AimHighEventManager>();

            EventManager.AddListener<AimHighRoundStartedEvent>(OnRoundStarted);
            EventManager.AddListener<AimHighQuotaChangedEvent>(OnQuotaChanged);
            EventManager.AddListener<AimHighRoundEndedEvent>(OnRoundEnded);
        }

        void OnRoundStarted(AimHighRoundStartedEvent evt)
        {
            if (RoundView != null)
            {
                RoundView.gameObject.SetActive(false);
            }
            m_CurrentToast = null;

            m_CurrentRound = evt.RoundIndex;
            m_CurrentQuota = evt.Quota;
            m_CurrentQuotaProgress = 0;
            m_TimeRemaining = evt.Duration;

            if (ShouldHideForEvent())
            {
                HideToast();
                return;
            }

            EnsureToast();
            UpdateToastContent();
        }

        void OnQuotaChanged(AimHighQuotaChangedEvent evt)
        {
            m_CurrentRound = evt.RoundIndex;

            m_CurrentQuota = evt.Quota;
            m_CurrentQuotaProgress = evt.QuotaProgress;
            m_TimeRemaining = evt.TimeRemaining;

            if (ShouldHideForEvent())
            {
                HideToast();
                return;
            }

            EnsureToast();
            UpdateToastContent();
        }

        void OnRoundEnded(AimHighRoundEndedEvent evt)
        {
            m_CurrentRound = evt.RoundIndex;

            if (m_CurrentToast != null)
            {
                Invoke(nameof(HideToast), 0.5f);
            }
        }

        bool ShouldHideForEvent()
        {
            if (m_EventManager == null) return false;
            return m_EventManager.IsEventActive || m_EventManager.HasActiveContract;
        }

        void EnsureToast()
        {
            if (m_CurrentToast != null || RoundView == null) return;

            m_CurrentToast = RoundView;
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
        }

        void UpdateToastContent()
        {
            if (m_CurrentToast == null) return;

            m_CurrentToast.TitleTextContent.text = $"ROUND {m_CurrentRound}";
            m_CurrentToast.DescriptionTextContent.text = $"Time left {m_TimeRemaining:0.0}s"; 
            m_CurrentToast.CounterTextContent.text = $"$ {m_CurrentQuotaProgress} / {m_CurrentQuota}";
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<AimHighRoundStartedEvent>(OnRoundStarted);
            EventManager.RemoveListener<AimHighQuotaChangedEvent>(OnQuotaChanged);
            EventManager.RemoveListener<AimHighRoundEndedEvent>(OnRoundEnded);
        }
    }
}
