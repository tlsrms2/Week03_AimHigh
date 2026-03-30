using TMPro;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

namespace Unity.FPS.UI
{
    public class AimHighHUD : MonoBehaviour
    {
        [Tooltip("Text for the current round")]
        public TextMeshProUGUI RoundText;

        [Tooltip("Text for the current score")]
        public TextMeshProUGUI ScoreText;

        [Tooltip("Text for the current quota")]
        public TextMeshProUGUI QuotaText;

        [Tooltip("Text for the round timer")]
        public TextMeshProUGUI TimerText;

        [Tooltip("Text for the current multiplier")]
        public TextMeshProUGUI MultiplierText;

        [Tooltip("Text for current currency")]
        public TextMeshProUGUI CurrencyText;

        [Tooltip("Text for current magazine ammo")]
        public TextMeshProUGUI AmmoText;

        PlayerWeaponsManager m_PlayerWeaponsManager;

        void Awake()
        {
            m_PlayerWeaponsManager = FindFirstObjectByType<PlayerWeaponsManager>();
            EventManager.AddListener<AimHighRoundStartedEvent>(OnRoundStarted);
            EventManager.AddListener<AimHighQuotaChangedEvent>(OnQuotaChanged);
            EventManager.AddListener<AimHighScoreChangedEvent>(OnScoreChanged);
        }

        void Update()
        {
            if (AmmoText == null || m_PlayerWeaponsManager == null)
            {
                return;
            }

            AimHighWeaponController activeWeapon = m_PlayerWeaponsManager.GetActiveWeapon();
            if (activeWeapon == null)
            {
                AmmoText.text = "-- / --";
                return;
            }

            if (activeWeapon.UseMagazineSystem)
            {
                if (activeWeapon.IsReloading)
                {
                    AmmoText.text =
                        $"{activeWeapon.GetCurrentAmmo()} / {activeWeapon.GetCarriedPhysicalBullets()}  RELOAD {activeWeapon.GetReloadTimeRemaining():0.0}s";
                }
                else
                {
                    AmmoText.text = $"{activeWeapon.GetCurrentAmmo()} / {activeWeapon.GetCarriedPhysicalBullets()}";
                }
            }
            else
            {
                AmmoText.text = $"{activeWeapon.GetCurrentAmmo()}";
            }
        }

        void OnRoundStarted(AimHighRoundStartedEvent evt)
        {
            if (RoundText != null)
            {
                RoundText.text = $"ROUND {evt.RoundIndex}";
            }
        }

        void OnQuotaChanged(AimHighQuotaChangedEvent evt)
        {
            if (QuotaText != null)
            {
                QuotaText.text = $"$ {evt.QuotaProgress} / {evt.Quota}";
            }

            if (TimerText != null)
            {
                TimerText.text = $"{evt.TimeRemaining:0.0}s";
            }
        }

        void OnScoreChanged(AimHighScoreChangedEvent evt)
        {
            if (ScoreText != null)
            {
                ScoreText.text = $"SCORE {evt.RoundScore}";
            }

            if (MultiplierText != null)
            {
                MultiplierText.text = $"x{evt.Multiplier:0.00}";
            }

            if (CurrencyText != null)
            {
                CurrencyText.text = $"${evt.Currency}";
            }
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<AimHighRoundStartedEvent>(OnRoundStarted);
            EventManager.RemoveListener<AimHighQuotaChangedEvent>(OnQuotaChanged);
            EventManager.RemoveListener<AimHighScoreChangedEvent>(OnScoreChanged);
        }
    }
}
