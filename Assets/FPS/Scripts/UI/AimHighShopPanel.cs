using TMPro;
using Unity.FPS.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class AimHighShopPanel : MonoBehaviour
    {
        [Tooltip("Panel root that opens during the shop")]
        public GameObject PanelRoot;

        [Tooltip("Text showing the next round number")]
        public TextMeshProUGUI NextRoundText;

        [Tooltip("Button used to buy ammo")]
        public Button BuyAmmoButton;

        [Tooltip("Button used to roll augment choices")]
        public Button RollAugmentButton;

        [Tooltip("Optional text showing the current augment roll cost")]
        public TextMeshProUGUI RollAugmentCostText;

        [Tooltip("Optional text showing the current ammo purchase cost")]
        public TextMeshProUGUI BuyAmmoCostText;

        [Tooltip("Button used to leave the shop")]
        public Button ExitButton;

        [Tooltip("Panel shown when augment choices are available")]
        public GameObject AugmentSelectionPanelRoot;

        [Tooltip("Buttons for the three augment choices")]
        public Button[] AugmentOptionButtons;

        [Tooltip("Labels for the augment choice buttons")]
        public TextMeshProUGUI[] AugmentOptionLabels;

        [Tooltip("Descriptions for the augment choice buttons")]
        public TextMeshProUGUI[] AugmentOptionDescriptions;

        AimHighShopManager m_ShopManager;
        AimHighScoreManager m_ScoreManager;

        void Awake()
        {
            m_ShopManager = FindFirstObjectByType<AimHighShopManager>();
            m_ScoreManager = FindFirstObjectByType<AimHighScoreManager>();
            BindButtons();

            if (m_ShopManager != null)
            {
                m_ShopManager.ShopStateChanged += Refresh;
                m_ShopManager.OffersChanged += Refresh;
            }

            EventManager.AddListener<AimHighScoreChangedEvent>(OnScoreChanged);
            Refresh();
        }

        void BindButtons()
        {
            if (BuyAmmoButton != null)
            {
                BuyAmmoButton.onClick.AddListener(BuyAmmo);
            }

            if (RollAugmentButton != null)
            {
                RollAugmentButton.onClick.AddListener(RollAugments);
            }

            if (ExitButton != null)
            {
                ExitButton.onClick.AddListener(ExitShop);
            }

            if (AugmentOptionButtons == null)
            {
                return;
            }

            for (int i = 0; i < AugmentOptionButtons.Length; i++)
            {
                if (AugmentOptionButtons[i] == null)
                {
                    continue;
                }

                int buttonIndex = i;
                AugmentOptionButtons[i].onClick.AddListener(() => ChooseAugment(buttonIndex));
            }
        }

        void OnScoreChanged(AimHighScoreChangedEvent evt)
        {
            Refresh();
        }

        public void BuyAmmo()
        {
            if (m_ShopManager != null)
            {
                m_ShopManager.TryBuyAmmo();
                Refresh();
            }
        }

        public void RollAugments()
        {
            if (m_ShopManager != null)
            {
                m_ShopManager.TryRollAugments();
                Refresh();
            }
        }

        public void ChooseAugment(int index)
        {
            if (m_ShopManager != null)
            {
                m_ShopManager.ChooseAugment(index);
                Refresh();
            }
        }

        public void ExitShop()
        {
            if (m_ShopManager != null)
            {
                m_ShopManager.ExitShop();
            }
        }

        void Refresh()
        {
            if (m_ShopManager != null && m_ShopManager.IsShopOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (PanelRoot != null)
            {
                PanelRoot.SetActive(m_ShopManager != null && m_ShopManager.IsShopOpen);
            }

            if (m_ShopManager == null)
            {
                return;
            }

            if (NextRoundText != null)
            {
                NextRoundText.text = $"Next Round {m_ShopManager.PendingNextRoundIndex}";
            }

            if (AugmentSelectionPanelRoot != null)
            {
                AugmentSelectionPanelRoot.SetActive(m_ShopManager.CurrentAugmentOffers.Count > 0);
            }

            if (BuyAmmoButton != null)
            {
                BuyAmmoButton.interactable = m_ShopManager.CanBuyAmmo();
            }

            if (BuyAmmoCostText != null)
            {
                BuyAmmoCostText.text = $"{m_ShopManager.CurrentAmmoPurchaseCost}";
            }

            if (RollAugmentButton != null && m_ScoreManager != null)
            {
                int currentRollCost = m_ShopManager.CurrentAugmentRollCost;
                RollAugmentButton.interactable =
                    m_ShopManager.IsShopOpen &&
                    m_ShopManager.CurrentAugmentOffers.Count == 0 &&
                    m_ScoreManager.Currency >= currentRollCost;
            }

            if (RollAugmentCostText != null)
            {
                RollAugmentCostText.text = $"{m_ShopManager.CurrentAugmentRollCost}";
            }

            if (m_ShopManager.IsShopOpen && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
            {
                Button defaultButton = null;
                if (AugmentSelectionPanelRoot != null && AugmentSelectionPanelRoot.activeSelf && AugmentOptionButtons != null)
                {
                    for (int i = 0; i < AugmentOptionButtons.Length; i++)
                    {
                        if (AugmentOptionButtons[i] != null && AugmentOptionButtons[i].gameObject.activeInHierarchy)
                        {
                            defaultButton = AugmentOptionButtons[i];
                            break;
                        }
                    }
                }

                if (defaultButton == null)
                {
                    defaultButton = RollAugmentButton != null && RollAugmentButton.gameObject.activeInHierarchy
                        ? RollAugmentButton
                        : BuyAmmoButton;
                }

                if (defaultButton != null)
                {
                    EventSystem.current.SetSelectedGameObject(defaultButton.gameObject);
                }
            }

            if (AugmentOptionButtons == null)
            {
                return;
            }

            for (int i = 0; i < AugmentOptionButtons.Length; i++)
            {
                bool hasOffer = i < m_ShopManager.CurrentAugmentOffers.Count;
                if (AugmentOptionButtons[i] != null)
                {
                    AugmentOptionButtons[i].gameObject.SetActive(hasOffer);
                    AugmentOptionButtons[i].interactable = hasOffer;
                }

                if (!hasOffer)
                {
                    continue;
                }

                AimHighAugmentDefinition offer = m_ShopManager.CurrentAugmentOffers[i];
                if (AugmentOptionLabels != null && i < AugmentOptionLabels.Length && AugmentOptionLabels[i] != null)
                {
                    AugmentOptionLabels[i].text = offer.DisplayName;
                }

                if (AugmentOptionDescriptions != null &&
                    i < AugmentOptionDescriptions.Length &&
                    AugmentOptionDescriptions[i] != null)
                {
                    AugmentOptionDescriptions[i].text = offer.Description;
                }
            }
        }

        void OnDestroy()
        {
            if (m_ShopManager != null)
            {
                m_ShopManager.ShopStateChanged -= Refresh;
                m_ShopManager.OffersChanged -= Refresh;
            }

            EventManager.RemoveListener<AimHighScoreChangedEvent>(OnScoreChanged);
        }
    }
}
