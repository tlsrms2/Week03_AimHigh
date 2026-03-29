using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay.AimHigh
{
    [RequireComponent(typeof(Health))]
    public class AimHighTarget : MonoBehaviour
    {
        [Tooltip("Base score gained when this target is destroyed")]
        public int BaseScore = 10;

        [Tooltip("How much distance should amplify the gained score")]
        public float DistanceScoreFactor = 0.02f;

        [Tooltip("Flat score bonus for this target")]
        public int FlatBonus;

        [Tooltip("Legacy field kept for compatibility. Quota now comes from the gold earned this round")]
        public int QuotaGain = 10;

        [Tooltip("Gold granted when this target is destroyed")]
        public int GoldDropAmount = 5;

        [Tooltip("Offset applied to the floating gold text")]
        public Vector3 GoldPopupOffset = Vector3.up * 0.75f;

        [Tooltip("Color of the floating gold text")]
        public Color GoldPopupColor = new Color(1f, 0.85f, 0.2f, 1f);

        [Tooltip("Optional life time before the target expires")]
        public float Lifetime = 4f;

        [Tooltip("Additional multiplier applied when scored")]
        public float ScoreMultiplier = 1f;

        [Tooltip("Whether the spawner should respawn a replacement when this target expires")]
        public bool RespawnOnExpire = true;

        Health m_Health;
        AimHighScoreManager m_ScoreManager;
        AimHighTargetSpawner m_Spawner;
        float m_DestroyAt = Mathf.Infinity;
        bool m_HandledExit;
        GameObject m_LastDamageSource;

        void Awake()
        {
            m_Health = GetComponent<Health>();
            m_ScoreManager = FindFirstObjectByType<AimHighScoreManager>();
        }

        void OnEnable()
        {
            if (m_Health != null)
            {
                m_Health.CurrentHealth = m_Health.MaxHealth;
                m_Health.OnDamaged += OnDamaged;
                m_Health.OnDie += OnDie;
            }

            Damageable[] damageables = GetComponentsInChildren<Damageable>(true);
            for (int i = 0; i < damageables.Length; i++)
            {
                if (damageables[i] != null)
                {
                    damageables[i].DamageMultiplier = 1f;
                }
            }

            if (Lifetime > 0f)
            {
                m_DestroyAt = Time.time + Lifetime;
            }
        }

        void Update()
        {
            if (!m_HandledExit && Time.time >= m_DestroyAt)
            {
                HandleExit(false, null);
            }
        }

        public void SetSpawner(AimHighTargetSpawner spawner)
        {
            m_Spawner = spawner;
        }

        void OnDie()
        {
            if (!m_HandledExit)
            {
                HandleExit(true, m_LastDamageSource);
            }
        }

        void OnDamaged(float damage, GameObject damageSource)
        {
            m_LastDamageSource = damageSource;
        }

        void HandleExit(bool grantScore, GameObject damageSource)
        {
            m_HandledExit = true;

            if (grantScore && m_ScoreManager != null)
            {
                float distanceMultiplier = 1f;
                float distance = 0f;
                if (damageSource != null)
                {
                    distance = Vector3.Distance(damageSource.transform.position, transform.position);
                    distanceMultiplier += distance * DistanceScoreFactor;
                }

                m_ScoreManager.AddScore(BaseScore, distanceMultiplier, ScoreMultiplier, FlatBonus);
                int grantedGold = m_ScoreManager.AddGold(GoldDropAmount, distance, HasMovingTargetBonus());
                if (grantedGold > 0)
                {
                    AimHighFloatingText.Spawn($"+{grantedGold}", transform.position + GoldPopupOffset, GoldPopupColor);
                }
            }

            if (m_Spawner != null)
            {
                m_Spawner.NotifyTargetRemoved(this, !grantScore && RespawnOnExpire);
            }

            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (m_Health != null)
            {
                m_Health.OnDamaged -= OnDamaged;
                m_Health.OnDie -= OnDie;
            }
        }

        bool HasMovingTargetBonus()
        {
            return GetComponent<IAimHighMovingTarget>() != null;
        }
    }
}
