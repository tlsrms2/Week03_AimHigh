using TMPro;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    [RequireComponent(typeof(FillBarColorChange))]
    public class AmmoCounter : MonoBehaviour
    {
        [Tooltip("CanvasGroup to fade the ammo UI")]
        public CanvasGroup CanvasGroup;

        [Tooltip("Image for the weapon icon")] public Image WeaponImage;

        [Tooltip("Image component for the background")]
        public Image AmmoBackgroundImage;

        [Tooltip("Image component to display fill ratio")]
        public Image AmmoFillImage;

        [Tooltip("Text for Weapon index")] 
        public TextMeshProUGUI WeaponIndexText;

        [Tooltip("Text for Bullet Counter")] 
        public TextMeshProUGUI BulletCounter;

        [Tooltip("Reload Text for Weapons with physical bullets")]
        public RectTransform Reload;

        [Header("Selection")] [Range(0, 1)] [Tooltip("Opacity when weapon not selected")]
        public float UnselectedOpacity = 0.5f;

        [Tooltip("Scale when weapon not selected")]
        public Vector3 UnselectedScale = Vector3.one * 0.8f;

        [Tooltip("Root for the control keys")] public GameObject ControlKeysRoot;

        [Header("Feedback")] [Tooltip("Component to animate the color when empty or full")]
        public FillBarColorChange FillBarColorChange;

        [Tooltip("Sharpness for the fill ratio movements")]
        public float AmmoFillMovementSharpness = 20f;

        public int WeaponCounterIndex { get; set; }

        PlayerWeaponsManager m_PlayerWeaponsManager;
        AimHighWeaponController m_Weapon;

        public void Initialize(AimHighWeaponController weapon, int weaponIndex)
        {
            m_Weapon = weapon;
            WeaponCounterIndex = weaponIndex;
            if (WeaponImage != null)
            {
                WeaponImage.gameObject.SetActive(false);
            }

            if (BulletCounter != null && BulletCounter.transform.parent != null)
            {
                BulletCounter.transform.parent.gameObject.SetActive(weapon.UseMagazineSystem);
            }

            if (BulletCounter != null)
            {
                BulletCounter.text = weapon.GetCarriedPhysicalBullets().ToString();
            }

            if (Reload != null)
            {
                Reload.gameObject.SetActive(false);
            }

            m_PlayerWeaponsManager = FindFirstObjectByType<PlayerWeaponsManager>();
            DebugUtility.HandleErrorIfNullFindObject<PlayerWeaponsManager, AmmoCounter>(m_PlayerWeaponsManager, this);

            if (WeaponIndexText != null)
            {
                WeaponIndexText.text = (WeaponCounterIndex + 1).ToString();
            }

            if (FillBarColorChange != null)
            {
                FillBarColorChange.Initialize(1f, m_Weapon.GetAmmoNeededToShoot());
            }
        }

        void Update()
        {
            if (m_Weapon == null || m_PlayerWeaponsManager == null)
            {
                return;
            }

            float currenFillRatio = m_Weapon.CurrentAmmoRatio;
            if (AmmoFillImage != null)
            {
                AmmoFillImage.fillAmount = Mathf.Lerp(AmmoFillImage.fillAmount, currenFillRatio,
                    Time.deltaTime * AmmoFillMovementSharpness);
            }

            if (BulletCounter != null)
            {
                if (m_Weapon.UseMagazineSystem)
                {
                    if (m_Weapon.IsReloading)
                    {
                        BulletCounter.text =
                            $"{m_Weapon.GetCurrentAmmo()}/{m_Weapon.GetMagazineCapacity()} | {m_Weapon.GetCarriedPhysicalBullets()} ({m_Weapon.GetReloadTimeRemaining():0.0}s)";
                    }
                    else
                    {
                        BulletCounter.text = $"{m_Weapon.GetCurrentAmmo()}/{m_Weapon.GetMagazineCapacity()} | {m_Weapon.GetCarriedPhysicalBullets()}";
                    }
                }
                else
                {
                    BulletCounter.text = m_Weapon.GetCarriedPhysicalBullets().ToString();
                }
            }

            bool isActiveWeapon = m_Weapon == m_PlayerWeaponsManager.GetActiveWeapon();

            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = Mathf.Lerp(CanvasGroup.alpha, isActiveWeapon ? 1f : UnselectedOpacity,
                    Time.deltaTime * 10);
            }

            transform.localScale = Vector3.Lerp(transform.localScale, isActiveWeapon ? Vector3.one : UnselectedScale,
                Time.deltaTime * 10);
            if (ControlKeysRoot != null)
            {
                ControlKeysRoot.SetActive(!isActiveWeapon);
            }

            if (FillBarColorChange != null)
            {
                FillBarColorChange.UpdateVisual(currenFillRatio);
            }

            if (Reload != null)
            {
                Reload.gameObject.SetActive(
                    m_Weapon.GetCarriedPhysicalBullets() > 0 &&
                    m_Weapon.GetCurrentAmmo() == 0 &&
                    m_Weapon.IsWeaponActive);
            }
        }

    }
}
