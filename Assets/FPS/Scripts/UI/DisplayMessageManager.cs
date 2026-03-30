using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.FPS.UI
{
    public class DisplayMessageManager : MonoBehaviour
    {
        [Tooltip("Root used to place the central game message")]
        public UITable DisplayMessageRect;

        [FormerlySerializedAs("MessagePrefab")]
        [Tooltip("Scene message object used for the central game message")]
        public NotificationToast MessageView;

        [Tooltip("If enabled, a new message replaces the current one immediately")]
        public bool ReplaceActiveMessage = true;

        [Tooltip("Anchored X position applied to the message view")]
        public float MessageAnchoredX = -350f;

        readonly Queue<PendingMessage> m_PendingMessages = new Queue<PendingMessage>();
        NotificationToast m_ActiveMessage;
        float m_NextDisplayTime;

        struct PendingMessage
        {
            public string Text;
            public float Delay;
        }

        void Awake()
        {
            EventManager.AddListener<DisplayMessageEvent>(OnDisplayMessageEvent);

            if (DisplayMessageRect != null)
            {
                NotificationToast sceneMessageView = DisplayMessageRect.GetComponentInChildren<NotificationToast>(true);
                if (sceneMessageView != null && sceneMessageView.gameObject.scene.IsValid())
                {
                    MessageView = sceneMessageView;
                }
            }

            if (MessageView != null)
            {
                if (!MessageView.gameObject.scene.IsValid())
                {
                    MessageView = Instantiate(MessageView, DisplayMessageRect.transform);
                }

                MessageView.DestroyOnComplete = false;
                MessageView.HideImmediate();
            }
        }

        public float GetDefaultToastRunTime()
        {
            if (MessageView != null)
            {
                return MessageView.TotalRunTime;
            }
            return 3f; // Fallback
        }

        void OnDisplayMessageEvent(DisplayMessageEvent evt)
        {
            if (string.IsNullOrWhiteSpace(evt.Message) || MessageView == null || DisplayMessageRect == null)
            {
                return;
            }

            PendingMessage message = new PendingMessage
            {
                Text = evt.Message,
                Delay = Mathf.Max(0f, evt.DelayBeforeDisplay)
            };

            if (ReplaceActiveMessage)
            {
                ClearActiveMessage();
                m_PendingMessages.Clear();
                m_PendingMessages.Enqueue(message);
                m_NextDisplayTime = Time.time + message.Delay;
                return;
            }

            m_PendingMessages.Enqueue(message);
            if (m_ActiveMessage == null && m_PendingMessages.Count == 1)
            {
                m_NextDisplayTime = Time.time + message.Delay;
            }
        }

        void Update()
        {
            if (m_ActiveMessage != null && !m_ActiveMessage.Initialized)
            {
                m_ActiveMessage = null;
                if (m_PendingMessages.Count > 0)
                {
                    m_NextDisplayTime = Time.time + m_PendingMessages.Peek().Delay;
                }
            }

            if (m_ActiveMessage == null)
            {
                TryShowNextMessage();
            }
        }

        void TryShowNextMessage()
        {
            if (m_PendingMessages.Count == 0 || Time.time < m_NextDisplayTime)
            {
                return;
            }

            PendingMessage message = m_PendingMessages.Dequeue();
            m_ActiveMessage = MessageView;
            m_ActiveMessage.Initialize(message.Text);
            DisplayMessageRect.UpdateTable(m_ActiveMessage.gameObject);
            ApplyMessagePosition();

            if (m_PendingMessages.Count > 0)
            {
                m_NextDisplayTime = Time.time + m_ActiveMessage.TotalRunTime + m_PendingMessages.Peek().Delay;
            }
        }

        void ApplyMessagePosition()
        {
            if (m_ActiveMessage != null && m_ActiveMessage.TryGetComponent(out RectTransform rectTransform))
            {
                Vector2 anchoredPosition = rectTransform.anchoredPosition;
                anchoredPosition.x = MessageAnchoredX;
                rectTransform.anchoredPosition = anchoredPosition;
            }
        }

        void ClearActiveMessage()
        {
            if (m_ActiveMessage != null)
            {
                m_ActiveMessage.HideImmediate();
                m_ActiveMessage = null;
            }
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<DisplayMessageEvent>(OnDisplayMessageEvent);
        }
    }
}
