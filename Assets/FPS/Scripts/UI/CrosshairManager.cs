using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class CrosshairManager : MonoBehaviour
    {
        public Image CrosshairImage;
        public Sprite NullCrosshairSprite;
        public float CrosshairUpdateshrpness = 5f;
        public bool UseFixedCrosshair = true;
        public int FixedCrosshairSize = 32;
        public Color FixedCrosshairColor = Color.white;

        PlayerWeaponsManager m_WeaponsManager;
        PlayerCharacterController m_PlayerCharacterController;
        bool m_WasPointingAtEnemy;
        RectTransform m_CrosshairRectTransform;
        AimHighCrosshairData m_CrosshairDataDefault;
        AimHighCrosshairData m_CrosshairDataTarget;
        AimHighCrosshairData m_CurrentCrosshair;

        void Start()
        {
            m_WeaponsManager = FindFirstObjectByType<PlayerWeaponsManager>();
            m_PlayerCharacterController = m_WeaponsManager != null
                ? m_WeaponsManager.GetComponent<PlayerCharacterController>()
                : null;
            m_CrosshairRectTransform = CrosshairImage.GetComponent<RectTransform>();
            DebugUtility.HandleErrorIfNullGetComponent<RectTransform, CrosshairManager>(m_CrosshairRectTransform,
                this, CrosshairImage.gameObject);

            if (UseFixedCrosshair || m_WeaponsManager == null)
            {
                SetupFixedCrosshair();
                return;
            }

            OnWeaponChanged(m_WeaponsManager.GetActiveWeapon());

            m_WeaponsManager.OnSwitchedToWeapon += OnWeaponChanged;
        }

        void Update()
        {
            if (UseFixedCrosshair || m_WeaponsManager == null)
            {
                return;
            }

            UpdateCrosshairPointingAtEnemy(false);
            m_WasPointingAtEnemy = m_WeaponsManager.IsPointingAtEnemy;
        }

        void SetupFixedCrosshair()
        {
            CrosshairImage.enabled = NullCrosshairSprite != null;
            CrosshairImage.sprite = NullCrosshairSprite;
            CrosshairImage.color = FixedCrosshairColor;
            m_CrosshairRectTransform.sizeDelta = FixedCrosshairSize * Vector2.one;
        }

        void UpdateCrosshairPointingAtEnemy(bool force)
        {
            AimHighWeaponController activeWeapon = m_WeaponsManager.GetActiveWeapon();
            if (activeWeapon == null)
                return;

            Camera referenceCamera = m_PlayerCharacterController != null ? m_PlayerCharacterController.PlayerCamera : null;
            m_CrosshairDataDefault = activeWeapon.GetCrosshairData(false, referenceCamera);
            m_CrosshairDataTarget = activeWeapon.GetCrosshairData(true, referenceCamera);

            // Select current target based on pointing state
            AimHighCrosshairData targetData = m_WeaponsManager.IsPointingAtEnemy ? m_CrosshairDataTarget : m_CrosshairDataDefault;

            if (force || m_WeaponsManager.IsPointingAtEnemy != m_WasPointingAtEnemy)
            {
                m_CurrentCrosshair = targetData;
                CrosshairImage.sprite = m_CurrentCrosshair.CrosshairSprite;
                
                if (force)
                {
                    m_CrosshairRectTransform.sizeDelta = m_CurrentCrosshair.CrosshairSize * Vector2.one;
                    CrosshairImage.color = m_CurrentCrosshair.CrosshairColor;
                }
            }
            else
            {
                // Always sync target data pointers in case bonuses changed
                m_CurrentCrosshair = targetData;
            }

            CrosshairImage.color = m_CurrentCrosshair.CrosshairColor;
            m_CrosshairRectTransform.sizeDelta = m_CurrentCrosshair.CrosshairSize * Vector2.one;

            UpdateDisplayedCrosshairSize(activeWeapon);
        }

        void OnWeaponChanged(AimHighWeaponController newWeapon)
        {
            if (newWeapon)
            {
                CrosshairImage.enabled = true;
                Camera referenceCamera = m_PlayerCharacterController != null ? m_PlayerCharacterController.PlayerCamera : null;
                m_CrosshairDataDefault = newWeapon.GetCrosshairData(false, referenceCamera);
                m_CrosshairDataTarget = newWeapon.GetCrosshairData(true, referenceCamera);
            }
            else
            {
                if (NullCrosshairSprite)
                {
                    CrosshairImage.sprite = NullCrosshairSprite;
                }
                else
                {
                    CrosshairImage.enabled = false;
                }
            }

            UpdateCrosshairPointingAtEnemy(true);
        }

        void UpdateDisplayedCrosshairSize(AimHighWeaponController activeWeapon)
        {
            if (activeWeapon == null || m_CrosshairRectTransform == null)
            {
                return;
            }

            float displayedWidth = m_CrosshairRectTransform.rect.width * m_CrosshairRectTransform.lossyScale.x;
            activeWeapon.DisplayedCrosshairSizePixels = displayedWidth;
        }
    }
}
