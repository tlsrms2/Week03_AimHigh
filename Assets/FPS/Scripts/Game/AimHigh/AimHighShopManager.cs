using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    public class AimHighShopManager : MonoBehaviour
    {
        [Tooltip("Cost to buy an ammo refill")]
        public int AmmoPurchaseCost = 25;

        [Tooltip("Amount of ammo restored when buying ammo")]
        public int AmmoPurchaseAmount = 10;

        [Tooltip("Cost to roll augment choices")]
        public int AugmentRollCost = 50;

        [Tooltip("Possible permanent augments that can appear in the shop")]
        public List<AimHighAugmentDefinition> AvailableAugments = new List<AimHighAugmentDefinition>();

        public bool IsShopOpen { get; private set; }
        public int PendingNextRoundIndex { get; private set; }
        public IReadOnlyList<AimHighAugmentDefinition> CurrentAugmentOffers => m_CurrentAugmentOffers;
        public IReadOnlyList<AimHighAugmentDefinition> OwnedAugments => m_OwnedAugments;
        public int CurrentAugmentRollCost => GetCurrentAugmentRollCost();
        public int CurrentAmmoPurchaseCost => GetCurrentAmmoPurchaseCost();

        public event Action ShopStateChanged;
        public event Action OffersChanged;

        readonly List<AimHighAugmentDefinition> m_CurrentAugmentOffers = new List<AimHighAugmentDefinition>();
        readonly List<AimHighAugmentDefinition> m_OwnedAugments = new List<AimHighAugmentDefinition>();

        AimHighScoreManager m_ScoreManager;
        GameFlowManager m_GameFlowManager;
        IAimHighWeaponInventory m_WeaponInventory;

        void Awake()
        {
            m_ScoreManager = FindFirstObjectByType<AimHighScoreManager>();
            m_GameFlowManager = FindFirstObjectByType<GameFlowManager>();

            MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (int i = 0; i < allBehaviours.Length; i++)
            {
                if (allBehaviours[i] is IAimHighWeaponInventory inventory)
                {
                    m_WeaponInventory = inventory;
                    break;
                }
            }
        }

        public void BeginShopPhase(int nextRoundIndex, float delayBeforeOpen)
        {
            PendingNextRoundIndex = Mathf.Max(1, nextRoundIndex);
            CancelInvoke(nameof(OpenShop));
            Invoke(nameof(OpenShop), Mathf.Max(0f, delayBeforeOpen));
        }

        void OpenShop()
        {
            IsShopOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            AimHighShopStateChangedEvent shopStateChangedEvent = Events.AimHighShopStateChangedEvent;
            shopStateChangedEvent.IsOpen = true;
            shopStateChangedEvent.NextRoundIndex = PendingNextRoundIndex;
            EventManager.Broadcast(shopStateChangedEvent);

            ShopStateChanged?.Invoke();
        }

        public bool CanBuyAmmo()
        {
            return IsShopOpen &&
                   m_ScoreManager != null &&
                   m_WeaponInventory != null &&
                   m_WeaponInventory.GetShopWeapon() != null &&
                   m_ScoreManager.Currency >= CurrentAmmoPurchaseCost;
        }

        public bool TryBuyAmmo()
        {
            if (!CanBuyAmmo())
            {
                return false;
            }

            if (!m_ScoreManager.TrySpendCurrency(CurrentAmmoPurchaseCost))
            {
                return false;
            }

            m_WeaponInventory.GetShopWeapon().AddAmmo(AmmoPurchaseAmount);
            ShopStateChanged?.Invoke();
            return true;
        }

        public bool TryRollAugments()
        {
            int augmentRollCost = GetCurrentAugmentRollCost();
            if (!IsShopOpen || m_ScoreManager == null || m_ScoreManager.Currency < augmentRollCost)
            {
                return false;
            }

            if (!m_ScoreManager.TrySpendCurrency(augmentRollCost))
            {
                return false;
            }

            m_CurrentAugmentOffers.Clear();

            List<AimHighAugmentDefinition> candidates = new List<AimHighAugmentDefinition>();
            for (int i = 0; i < AvailableAugments.Count; i++)
            {
                AimHighAugmentDefinition augment = AvailableAugments[i];
                if (augment != null && !m_OwnedAugments.Contains(augment))
                {
                    candidates.Add(augment);
                }
            }

            int offerCount = Mathf.Min(3, candidates.Count);
            for (int i = 0; i < offerCount; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
                m_CurrentAugmentOffers.Add(candidates[randomIndex]);
                candidates.RemoveAt(randomIndex);
            }

            OffersChanged?.Invoke();
            ShopStateChanged?.Invoke();
            return m_CurrentAugmentOffers.Count > 0;
        }

        public bool ChooseAugment(int offerIndex)
        {
            if (!IsShopOpen || offerIndex < 0 || offerIndex >= m_CurrentAugmentOffers.Count)
            {
                return false;
            }

            AimHighAugmentDefinition chosenAugment = m_CurrentAugmentOffers[offerIndex];
            if (chosenAugment != null && !m_OwnedAugments.Contains(chosenAugment))
            {
                m_OwnedAugments.Add(chosenAugment);
                ApplyAugment(chosenAugment);
            }

            m_CurrentAugmentOffers.Clear();
            OffersChanged?.Invoke();
            ShopStateChanged?.Invoke();
            return true;
        }

        void ApplyAugment(AimHighAugmentDefinition augment)
        {
            if (augment == null)
            {
                return;
            }

            AimHighWeaponController weapon = m_WeaponInventory != null ? m_WeaponInventory.GetShopWeapon() : null;

            switch (augment.EffectType)
            {
                case AimHighAugmentEffectType.GoldGainMultiplier:
                    m_ScoreManager?.AddGoldMultiplier(augment.FloatValue);
                    break;
                case AimHighAugmentEffectType.WeaponDamageMultiplier:
                    if (weapon != null)
                    {
                        weapon.AddDamageMultiplier(augment.FloatValue);
                    }
                    break;
                case AimHighAugmentEffectType.ReloadSpeedMultiplier:
                    if (weapon != null)
                    {
                        weapon.AddReloadSpeedMultiplier(augment.FloatValue);
                    }
                    break;
                case AimHighAugmentEffectType.ClipSizeBonus:
                    if (weapon != null)
                    {
                        int clipSizeBonus = augment.IntValue > 0 ? augment.IntValue : Mathf.RoundToInt(augment.FloatValue);
                        weapon.AddClipSizeBonus(clipSizeBonus);
                    }
                    break;
                case AimHighAugmentEffectType.ProjectileRangeMultiplier:
                    if (weapon != null)
                    {
                        weapon.AddProjectileRangeMultiplier(augment.FloatValue);
                    }
                    break;
                case AimHighAugmentEffectType.ShopRollCostReduction:
                    m_ScoreManager?.AddShopRollCostReduction(augment.FloatValue);
                    break;
                case AimHighAugmentEffectType.AmmoPurchaseCostReduction:
                    m_ScoreManager?.AddAmmoPurchaseCostReduction(augment.FloatValue);
                    break;
                case AimHighAugmentEffectType.FireRateMultiplier:
                    if (weapon != null)
                    {
                        weapon.AddFireRateMultiplier(augment.FloatValue);
                    }
                    break;
                case AimHighAugmentEffectType.DistanceGoldBonusFactor:
                    m_ScoreManager?.AddDistanceGoldBonusFactor(augment.FloatValue);
                    break;
                case AimHighAugmentEffectType.MovingTargetGoldMultiplier:
                    m_ScoreManager?.AddMovingTargetGoldMultiplier(augment.FloatValue);
                    break;
            }
        }

        int GetCurrentAugmentRollCost()
        {
            if (m_ScoreManager == null)
            {
                return AugmentRollCost;
            }

            return m_ScoreManager.GetAdjustedShopRollCost(AugmentRollCost);
        }

        int GetCurrentAmmoPurchaseCost()
        {
            if (m_ScoreManager == null)
            {
                return AmmoPurchaseCost;
            }

            return m_ScoreManager.GetAdjustedAmmoPurchaseCost(AmmoPurchaseCost);
        }

        public void ExitShop()
        {
            if (!IsShopOpen)
            {
                return;
            }

            IsShopOpen = false;
            m_CurrentAugmentOffers.Clear();

            AimHighShopStateChangedEvent shopStateChangedEvent = Events.AimHighShopStateChangedEvent;
            shopStateChangedEvent.IsOpen = false;
            shopStateChangedEvent.NextRoundIndex = PendingNextRoundIndex;
            EventManager.Broadcast(shopStateChangedEvent);

            ShopStateChanged?.Invoke();
            OffersChanged?.Invoke();

            if (m_GameFlowManager != null)
            {
                m_GameFlowManager.ScheduleAimHighRound(PendingNextRoundIndex, 0f);
            }
        }
    }
}
