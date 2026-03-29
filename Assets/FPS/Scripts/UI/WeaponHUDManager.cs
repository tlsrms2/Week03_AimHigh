using System.Collections.Generic;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;

namespace Unity.FPS.UI
{
    public class WeaponHUDManager : MonoBehaviour
    {
        [Tooltip("UI panel containing the layoutGroup for displaying weapon ammo")]
        public RectTransform AmmoPanel;

        PlayerWeaponsManager m_PlayerWeaponsManager;
        List<AmmoCounter> m_AmmoCounters = new List<AmmoCounter>();
        List<AmmoCounter> m_AmmoCounterViews = new List<AmmoCounter>();

        void Start()
        {
            m_PlayerWeaponsManager = FindFirstObjectByType<PlayerWeaponsManager>();
            DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, WeaponHUDManager>(m_PlayerWeaponsManager,
                this);

            CacheAmmoCounterViews();

            AimHighWeaponController activeWeapon = m_PlayerWeaponsManager.GetActiveWeapon();
            if (activeWeapon)
            {
                AddWeapon(activeWeapon, m_PlayerWeaponsManager.ActiveWeaponIndex);
                ChangeWeapon(activeWeapon);
            }

            m_PlayerWeaponsManager.OnAddedWeapon += AddWeapon;
            m_PlayerWeaponsManager.OnRemovedWeapon += RemoveWeapon;
            m_PlayerWeaponsManager.OnSwitchedToWeapon += ChangeWeapon;
        }

        void CacheAmmoCounterViews()
        {
            m_AmmoCounterViews.Clear();

            if (AmmoPanel == null)
            {
                return;
            }

            AmmoCounter[] views = AmmoPanel.GetComponentsInChildren<AmmoCounter>(true);
            for (int i = 0; i < views.Length; i++)
            {
                AmmoCounter view = views[i];
                m_AmmoCounterViews.Add(view);
                view.gameObject.SetActive(false);
            }
        }

        void AddWeapon(AimHighWeaponController newWeapon, int weaponIndex)
        {
            AmmoCounter newAmmoCounter = GetAvailableAmmoCounterView();
            if (newAmmoCounter == null)
            {
                return;
            }

            newAmmoCounter.gameObject.SetActive(true);
            newAmmoCounter.Initialize(newWeapon, weaponIndex);
            m_AmmoCounters.Add(newAmmoCounter);
        }

        void RemoveWeapon(AimHighWeaponController newWeapon, int weaponIndex)
        {
            int foundCounterIndex = -1;
            for (int i = 0; i < m_AmmoCounters.Count; i++)
            {
                if (m_AmmoCounters[i].WeaponCounterIndex == weaponIndex)
                {
                    foundCounterIndex = i;
                    m_AmmoCounters[i].gameObject.SetActive(false);
                }
            }

            if (foundCounterIndex >= 0)
            {
                m_AmmoCounters.RemoveAt(foundCounterIndex);
            }
        }

        void ChangeWeapon(AimHighWeaponController weapon)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(AmmoPanel);
        }

        AmmoCounter GetAvailableAmmoCounterView()
        {
            for (int i = 0; i < m_AmmoCounterViews.Count; i++)
            {
                if (!m_AmmoCounterViews[i].gameObject.activeSelf && !m_AmmoCounters.Contains(m_AmmoCounterViews[i]))
                {
                    return m_AmmoCounterViews[i];
                }
            }

            return null;
        }
    }
}
