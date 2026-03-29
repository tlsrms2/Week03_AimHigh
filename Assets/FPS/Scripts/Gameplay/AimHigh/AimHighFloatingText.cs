using UnityEngine;

namespace Unity.FPS.Gameplay.AimHigh
{
    public class AimHighFloatingText : MonoBehaviour
    {
        public float Lifetime = 1f;
        public float RiseDistance = 0.75f;
        public AnimationCurve AlphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        TextMesh m_Text;
        Vector3 m_StartPosition;
        Camera m_Camera;
        float m_SpawnTime;
        Color m_BaseColor;

        public static void Spawn(string message, Vector3 worldPosition, Color color, float fontSize = 4f)
        {
            GameObject floatingTextObject = new GameObject("AimHighFloatingText");
            floatingTextObject.transform.position = worldPosition;

            TextMesh text = floatingTextObject.AddComponent<TextMesh>();
            text.text = message;
            text.fontSize = Mathf.RoundToInt(fontSize * 10f);
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;
            text.characterSize = 0.1f;

            AimHighFloatingText floatingText = floatingTextObject.AddComponent<AimHighFloatingText>();
            floatingText.Initialize(text, color);
        }

        void Initialize(TextMesh text, Color color)
        {
            m_Text = text;
            m_BaseColor = color;
            m_StartPosition = transform.position;
            m_SpawnTime = Time.time;
            m_Camera = Camera.main;
        }

        void Update()
        {
            if (m_Camera == null)
            {
                m_Camera = Camera.main;
            }

            float normalizedLifetime = Lifetime <= 0f ? 1f : Mathf.Clamp01((Time.time - m_SpawnTime) / Lifetime);
            transform.position = m_StartPosition + Vector3.up * (RiseDistance * normalizedLifetime);

            if (m_Camera != null)
            {
                transform.forward = m_Camera.transform.forward;
            }

            if (m_Text != null)
            {
                Color color = m_BaseColor;
                color.a = AlphaCurve.Evaluate(normalizedLifetime);
                m_Text.color = color;
            }

            if (normalizedLifetime >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
