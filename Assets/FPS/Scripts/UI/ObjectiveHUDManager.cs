using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.UI
{
    public class ObjectiveHUDManager : MonoBehaviour
    {
        [Tooltip("UI panel containing the layoutGroup for displaying the current round goal")]
        public RectTransform ObjectivePanel;

        [Tooltip("Scene objective card used to display the current round goal")]
        public ObjectiveToast ObjectiveView;

        [Tooltip("Description shown when a round begins")]
        public string RoundDescription = "Reach the quota before time runs out.";

        ObjectiveToast m_CurrentToast;
        int m_CurrentRound;
        int m_CurrentQuota;
        int m_CurrentQuotaProgress;

        void Awake()
        {
            if (ObjectiveView == null && ObjectivePanel != null)
            {
                ObjectiveView = ObjectivePanel.GetComponentInChildren<ObjectiveToast>(true);
            }

            if (ObjectiveView != null)
            {
                ObjectiveView.gameObject.SetActive(true);
                ForceObjectiveViewVisible();
            }

            EventManager.AddListener<AimHighRoundStartedEvent>(OnRoundStarted);
            EventManager.AddListener<AimHighQuotaChangedEvent>(OnQuotaChanged);
            EventManager.AddListener<AimHighScoreChangedEvent>(OnScoreChanged);
            EventManager.AddListener<AimHighRoundEndedEvent>(OnRoundEnded);
        }

        void OnRoundStarted(AimHighRoundStartedEvent evt)
        {
            m_CurrentRound = evt.RoundIndex;
            m_CurrentQuota = evt.Quota;
            m_CurrentQuotaProgress = 0;

            EnsureToast();
            UpdateToastTitle();
            UpdateToastDescription(RoundDescription);
            UpdateToastCounter();
        }

        void OnQuotaChanged(AimHighQuotaChangedEvent evt)
        {
            m_CurrentRound = evt.RoundIndex;

            m_CurrentQuota = evt.Quota;
            m_CurrentQuotaProgress = evt.QuotaProgress;
            EnsureToast();
            UpdateToastDescription($"Time left {evt.TimeRemaining:0.0}s");
            UpdateToastCounter();
        }

        void OnScoreChanged(AimHighScoreChangedEvent evt)
        {
            if (m_CurrentToast == null)
            {
                return;
            }
        }

        void OnRoundEnded(AimHighRoundEndedEvent evt)
        {
            m_CurrentRound = evt.RoundIndex;

            EnsureToast();
            m_CurrentQuotaProgress = evt.QuotaProgress;
            m_CurrentQuota = evt.Quota;
            UpdateToastCounter();
            UpdateToastDescription(evt.Success ? "Quota reached. Prepare for the next round." : "Quota missed.");
        }

        void EnsureToast()
        {
            if (m_CurrentToast != null)
            {
                return;
            }

            if (ObjectiveView == null)
            {
                return;
            }

            ObjectiveView.gameObject.SetActive(true);
            m_CurrentToast = ObjectiveView;
            ForceObjectiveViewVisible();

            if (ObjectivePanel != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(ObjectivePanel);
            }
        }

        void UpdateToastTitle()
        {
            if (m_CurrentToast == null)
            {
                return;
            }

            m_CurrentToast.TitleTextContent.text = $"ROUND {m_CurrentRound}";
        }

        void UpdateToastDescription(string description)
        {
            if (m_CurrentToast == null)
            {
                return;
            }

            m_CurrentToast.DescriptionTextContent.text = description;
            RebuildToastLayout();
        }

        void UpdateToastCounter()
        {
            if (m_CurrentToast == null)
            {
                return;
            }

            m_CurrentToast.CounterTextContent.text = $"$ {m_CurrentQuotaProgress} / {m_CurrentQuota}";
            RebuildToastLayout();
        }

        void RebuildToastLayout()
        {
            if (m_CurrentToast != null && m_CurrentToast.TryGetComponent(out RectTransform toastRect))
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(toastRect);
            }
        }

        void ForceObjectiveViewVisible()
        {
            if (ObjectiveView == null)
            {
                return;
            }

            if (ObjectiveView.CanvasGroup != null)
            {
                ObjectiveView.CanvasGroup.alpha = 1f;
            }

            if (ObjectiveView.LayoutGroup != null)
            {
                ObjectiveView.LayoutGroup.padding.left = 0;
            }
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<AimHighRoundStartedEvent>(OnRoundStarted);
            EventManager.RemoveListener<AimHighQuotaChangedEvent>(OnQuotaChanged);
            EventManager.RemoveListener<AimHighScoreChangedEvent>(OnScoreChanged);
            EventManager.RemoveListener<AimHighRoundEndedEvent>(OnRoundEnded);
        }
    }
}
