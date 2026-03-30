using TMPro;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class AimHighStatDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Root panel that displays the stats")]
        public GameObject StatsPanel;

        [Tooltip("Toggle button that shows/hides the stats panel")]
        public Button ToggleButton;

        [Header("Stat Text References")]
        public TextMeshProUGUI GoldStatText;
        public TextMeshProUGUI DamageStatText;
        public TextMeshProUGUI ReloadStatText;
        public TextMeshProUGUI ClipSizeStatText;
        public TextMeshProUGUI FireRateStatText;
        public TextMeshProUGUI RangeStatText;
        public TextMeshProUGUI ShopCostStatText;

        AimHighScoreManager m_ScoreManager;
        IAimHighWeaponInventory m_WeaponInventory;

        void Awake()
        {
            m_ScoreManager = FindFirstObjectByType<AimHighScoreManager>();

            MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (int i = 0; i < allBehaviours.Length; i++)
            {
                if (allBehaviours[i] is IAimHighWeaponInventory inventory)
                {
                    m_WeaponInventory = inventory;
                    break;
                }
            }

            if (ToggleButton != null)
            {
                ToggleButton.onClick.AddListener(ToggleStats);
            }

            // Initially hidden
            if (StatsPanel != null)
            {
                StatsPanel.SetActive(false);
            }
        }

        void OnEnable()
        {
            RefreshStats();
        }

        public void ToggleStats()
        {
            if (StatsPanel != null)
            {
                bool nextState = !StatsPanel.activeSelf;
                StatsPanel.SetActive(nextState);
                
                if (nextState)
                {
                    RefreshStats();
                }
            }
        }

        public void RefreshStats()
        {
            if (m_ScoreManager != null)
            {
                if (GoldStatText != null)
                {
                    int goldPerc = Mathf.RoundToInt((m_ScoreManager.GoldMultiplier - 1f) * 100f);
                    string bonus = m_ScoreManager.FlatGoldBonus != 0 ? $" / +{m_ScoreManager.FlatGoldBonus} G" : "";
                    GoldStatText.text = $"Gold Gain: +{goldPerc}%{bonus}";
                }
 
                if (ShopCostStatText != null)
                {
                    float shopRed = (1f - m_ScoreManager.ShopRollCostMultiplier) * 100f;
                    float ammoRed = (1f - m_ScoreManager.AmmoPurchaseCostMultiplier) * 100f;
                    ShopCostStatText.text = $"Cost Red.: Roll -{shopRed:F0}% / Ammo -{ammoRed:F0}%";
                }
            }
 
            AimHighWeaponController weapon = m_WeaponInventory != null ? m_WeaponInventory.GetShopWeapon() : null;
            if (weapon != null)
            {
                if (DamageStatText != null)
                {
                    float finalDmg = weapon.GetFinalDamage();
                    string bonus = weapon.FlatDamageBonus != 0 ? $" (+{weapon.FlatDamageBonus:F0} Dmg)" : "";
                    DamageStatText.text = $"Total Damage: {finalDmg:F0}{bonus}";
                }
 
                if (ReloadStatText != null)
                {
                    int reloadPerc = Mathf.RoundToInt((weapon.ReloadSpeedMultiplier - 1f) * 100f);
                    string bonus = weapon.FlatReloadDurationReduction != 0 ? $" / -{weapon.FlatReloadDurationReduction:F1}s" : "";
                    ReloadStatText.text = $"Reload Speed: +{reloadPerc}%{bonus}";
                }
 
                if (ClipSizeStatText != null)
                {
                    int bonusArr = weapon.GetMagazineCapacity() - weapon.GetBaseClipSize();
                    string bonus = bonusArr != 0 ? $" (+{bonusArr})" : "";
                    ClipSizeStatText.text = $"Ammo Capacity: {weapon.GetMagazineCapacity()}{bonus}";
                }
 
                if (FireRateStatText != null)
                {
                    int fireratePerc = Mathf.RoundToInt((weapon.FireRateMultiplier - 1f) * 100f);
                    string bonus = weapon.FlatFireRateBonus != 0 ? $" / +{weapon.FlatFireRateBonus:F2}s" : "";
                    FireRateStatText.text = $"Fire Rate: +{fireratePerc}%{bonus}";
                }
 
                if (RangeStatText != null)
                {
                    int rangePerc = Mathf.RoundToInt((weapon.CrosshairSizeMultiplier - 1f) * 100f);
                    string bonus = weapon.FlatCrosshairSizeBonus != 0 ? $" / +{weapon.FlatCrosshairSizeBonus:F0}px" : "";
                    RangeStatText.text = $"Range/Spread: +{rangePerc}%{bonus}";
                }
            }
        }
    }
}
