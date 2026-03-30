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
        public int AmmoPurchaseAmount = 30;

        [Tooltip("Cost to roll augment choices")]
        public int AugmentRollCost = 50;

        [Tooltip("Amount current augment roll cost increases after each roll")]
        public int AugmentRollPriceIncreasePerRoll = 25;

        [Tooltip("Amount ammo purchase cost increases after each purchase")]
        public int AmmoPriceIncreasePerPurchase = 10;

        [Tooltip("Possible permanent augments that can appear in the shop")]
        public List<AimHighAugmentDefinition> AvailableAugments = new List<AimHighAugmentDefinition>();

        [System.Serializable]
        public class AimHighContractTemplate
        {
            [TextArea(2, 4)] public string Description = "Next round penalty text...";
            public float Reward = 0.2f;
            public float PenaltyValue = 0.5f;
        }

        [Header("Contract Settings")]
        [Tooltip("Chance for a special contract offer to appear in the shop")]
        [Range(0f, 1f)] public float ContractSpawnChance = 0.3f;

        [Tooltip("Minimum round index to start offering contracts")]
        public int MinRoundForContracts = 2;

        [Header("Contract Templates (Logic & Text)")]
        public AimHighContractTemplate GreedSettings;
        public AimHighContractTemplate HurrySettings;
        public AimHighContractTemplate ChunkySettings;

        public enum ContractType { None, Greed, Hurry, Chunky }
        public class ContractOffer
        {
            public ContractType Type;
            public string Title;
            public string Description;
            public float Reward;
            public float PenaltyValue;
        }

        public bool IsShopOpen { get; private set; }
        public int PendingNextRoundIndex { get; private set; }
        public IReadOnlyList<AimHighAugmentInstance> CurrentAugmentOffers => m_CurrentAugmentOffers;
        public IReadOnlyList<AimHighAugmentInstance> OwnedAugments => m_OwnedAugments;
        public int CurrentAugmentRollCost => GetCurrentAugmentRollCost();
        public int CurrentAmmoPurchaseCost => GetCurrentAmmoPurchaseCost();
        public ContractOffer CurrentContractOffer => m_CurrentContractOffer;
        public bool ContractAcceptedThisShop { get; private set; }
        public ContractOffer LastAcceptedContractOffer { get; private set; }

        public event Action ShopStateChanged;
        public event Action OffersChanged;
        public event Action ContractAccepted;

        readonly List<AimHighAugmentInstance> m_CurrentAugmentOffers = new List<AimHighAugmentInstance>();
        readonly List<AimHighAugmentInstance> m_OwnedAugments = new List<AimHighAugmentInstance>();
        ContractOffer m_CurrentContractOffer;

        int m_AmmoPurchaseCount = 0;
        int m_AugmentRollCount = 0;

        AimHighScoreManager m_ScoreManager;
        GameFlowManager m_GameFlowManager;
        AimHighEventManager m_EventManager;
        IAimHighWeaponInventory m_WeaponInventory;

        void Awake()
        {
            m_ScoreManager = FindFirstObjectByType<AimHighScoreManager>();
            m_GameFlowManager = FindFirstObjectByType<GameFlowManager>();
            m_EventManager = FindFirstObjectByType<AimHighEventManager>();

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
            ContractAcceptedThisShop = false;
            LastAcceptedContractOffer = null;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            RollContract();

            AimHighShopStateChangedEvent shopStateChangedEvent = Events.AimHighShopStateChangedEvent;
            shopStateChangedEvent.IsOpen = true;
            shopStateChangedEvent.NextRoundIndex = PendingNextRoundIndex;
            EventManager.Broadcast(shopStateChangedEvent);

            ShopStateChanged?.Invoke();
        }

        void RollContract()
        {
            m_CurrentContractOffer = null;
            if (PendingNextRoundIndex < MinRoundForContracts) return;
            if (UnityEngine.Random.value > ContractSpawnChance) return;

            m_CurrentContractOffer = new ContractOffer();
            int type = UnityEngine.Random.Range(1, 4);
            switch (type)
            {
                case 1:
                    m_CurrentContractOffer.Type = ContractType.Greed;
                    m_CurrentContractOffer.Title = "GREED";
                    m_CurrentContractOffer.Description = GreedSettings.Description;
                    m_CurrentContractOffer.Reward = GreedSettings.Reward;
                    m_CurrentContractOffer.PenaltyValue = GreedSettings.PenaltyValue;
                    break;
                case 2:
                    m_CurrentContractOffer.Type = ContractType.Hurry;
                    m_CurrentContractOffer.Title = "HURRY";
                    m_CurrentContractOffer.Description = HurrySettings.Description;
                    m_CurrentContractOffer.Reward = HurrySettings.Reward;
                    m_CurrentContractOffer.PenaltyValue = HurrySettings.PenaltyValue;
                    break;
                case 3:
                    m_CurrentContractOffer.Type = ContractType.Chunky;
                    m_CurrentContractOffer.Title = "CHUNKY";
                    m_CurrentContractOffer.Description = ChunkySettings.Description;
                    m_CurrentContractOffer.Reward = ChunkySettings.Reward;
                    m_CurrentContractOffer.PenaltyValue = ChunkySettings.PenaltyValue;
                    break;
            }
        }

        public void AcceptContract()
        {
            if (m_CurrentContractOffer == null || m_EventManager == null) return;

            // Signal the Event Manager about the active contract
            m_EventManager.ActivateContract(m_CurrentContractOffer.Type, m_CurrentContractOffer.PenaltyValue, m_CurrentContractOffer.Reward);
            
            ContractAcceptedThisShop = true;
            LastAcceptedContractOffer = m_CurrentContractOffer;
            m_CurrentContractOffer = null;

            ContractAccepted?.Invoke();
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

            m_WeaponInventory.AddAmmoToDefaultWeapon(AmmoPurchaseAmount);
            m_AmmoPurchaseCount++;
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
                if (augment != null)
                {
                    candidates.Add(augment);
                }
            }

            int offerCount = Mathf.Min(3, candidates.Count);
            for (int i = 0; i < offerCount; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
                AimHighAugmentDefinition chosenDef = candidates[randomIndex];
                
                // Roll for rarity
                AimHighAugmentRarity chosenRarity = RollRarity(chosenDef);
                m_CurrentAugmentOffers.Add(new AimHighAugmentInstance(chosenDef, chosenRarity));
                
                candidates.RemoveAt(randomIndex);
            }

            OffersChanged?.Invoke();
            ShopStateChanged?.Invoke();
            return m_CurrentAugmentOffers.Count > 0;
        }

        AimHighAugmentRarity RollRarity(AimHighAugmentDefinition definition)
        {
            float totalWeight = 0f;
            for (int i = 0; i < definition.Rarities.Count; i++)
            {
                totalWeight += definition.Rarities[i].Weight;
            }

            if (totalWeight <= 0f) return AimHighAugmentRarity.Normal;

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float currentSum = 0f;

            for (int i = 0; i < definition.Rarities.Count; i++)
            {
                currentSum += definition.Rarities[i].Weight;
                if (randomValue <= currentSum)
                {
                    return definition.Rarities[i].Rarity;
                }
            }

            return AimHighAugmentRarity.Normal;
        }

        bool IsOwned(AimHighAugmentDefinition definition)
        {
            for (int i = 0; i < m_OwnedAugments.Count; i++)
            {
                if (m_OwnedAugments[i].Definition == definition) return true;
            }
            return false;
        }

        public bool ChooseAugment(int offerIndex)
        {
            if (!IsShopOpen || offerIndex < 0 || offerIndex >= m_CurrentAugmentOffers.Count)
            {
                return false;
            }

            AimHighAugmentInstance chosenAugment = m_CurrentAugmentOffers[offerIndex];
            if (chosenAugment != null)
            {
                m_OwnedAugments.Add(chosenAugment);
                ApplyAugment(chosenAugment);
            }

            m_CurrentAugmentOffers.Clear();
            OffersChanged?.Invoke();
            m_AugmentRollCount++;
            ShopStateChanged?.Invoke();
            return true;
        }

        void ApplyAugment(AimHighAugmentInstance augment)
        {
            if (augment == null)
            {
                return;
            }

            AimHighWeaponController weapon = m_WeaponInventory != null ? m_WeaponInventory.GetShopWeapon() : null;

            switch (augment.Definition.EffectType)
            {
                case AimHighAugmentEffectType.GoldGainMultiplier:
                    if (m_ScoreManager != null)
                    {
                        if (augment.FloatValue != 0f) m_ScoreManager.AddGoldMultiplier(augment.FloatValue);
                        else if (augment.IntValue != 0) m_ScoreManager.AddFlatGoldBonus(augment.IntValue);
                    }
                    break;
                case AimHighAugmentEffectType.WeaponDamageMultiplier:
                    if (weapon != null)
                    {
                        if (augment.FloatValue != 0f) weapon.AddDamageMultiplier(augment.FloatValue);
                        else if (augment.IntValue != 0) weapon.AddFlatDamageBonus(augment.IntValue);
                    }
                    break;
                case AimHighAugmentEffectType.ReloadSpeedMultiplier:
                    if (weapon != null)
                    {
                        if (augment.FloatValue != 0f) weapon.AddReloadSpeedMultiplier(augment.FloatValue);
                        else if (augment.IntValue != 0) weapon.AddFlatReloadDurationReduction(augment.IntValue);
                    }
                    break;
                case AimHighAugmentEffectType.ClipSizeBonus:
                    if (weapon != null)
                    {
                        if (augment.FloatValue != 0f)
                        {
                            int bonus = Mathf.RoundToInt(weapon.GetBaseClipSize() * augment.FloatValue);
                            weapon.AddClipSizeBonus(bonus);
                        }
                        else if (augment.IntValue != 0)
                        {
                            weapon.AddClipSizeBonus(augment.IntValue);
                        }
                    }
                    break;
                case AimHighAugmentEffectType.ProjectileRangeMultiplier:
                    if (weapon != null)
                    {
                        if (augment.FloatValue != 0f) weapon.AddProjectileRangeMultiplier(augment.FloatValue);
                        else if (augment.IntValue != 0) weapon.AddFlatProjectileRangeBonus(augment.IntValue);
                    }
                    break;
                case AimHighAugmentEffectType.ShopRollCostReduction:
                    if (m_ScoreManager != null)
                    {
                        if (augment.FloatValue != 0f) m_ScoreManager.AddShopRollCostReduction(augment.FloatValue);
                        else if (augment.IntValue != 0) m_ScoreManager.AddShopRollCostReduction((float)augment.IntValue / 100f);
                    }
                    break;
                case AimHighAugmentEffectType.AmmoPurchaseCostReduction:
                    if (m_ScoreManager != null)
                    {
                        if (augment.FloatValue != 0f) m_ScoreManager.AddAmmoPurchaseCostReduction(augment.FloatValue);
                        else if (augment.IntValue != 0) m_ScoreManager.AddAmmoPurchaseCostReduction((float)augment.IntValue / 100f);
                    }
                    break;
                case AimHighAugmentEffectType.FireRateMultiplier:
                    if (weapon != null)
                    {
                        if (augment.FloatValue != 0f) weapon.AddFireRateMultiplier(augment.FloatValue);
                        else if (augment.IntValue != 0) weapon.AddFlatFireRateBonus(augment.IntValue);
                    }
                    break;
                case AimHighAugmentEffectType.DistanceGoldBonusFactor:
                    if (m_ScoreManager != null)
                    {
                        if (augment.FloatValue != 0f) m_ScoreManager.AddDistanceGoldBonusFactor(augment.FloatValue);
                        else if (augment.IntValue != 0) m_ScoreManager.AddDistanceGoldBonusFactor((float)augment.IntValue);
                    }
                    break;
                case AimHighAugmentEffectType.MovingTargetGoldMultiplier:
                    if (m_ScoreManager != null)
                    {
                        if (augment.FloatValue != 0f) m_ScoreManager.AddMovingTargetGoldMultiplier(augment.FloatValue);
                        else if (augment.IntValue != 0) m_ScoreManager.AddMovingTargetGoldMultiplier((float)augment.IntValue);
                    }
                    break;
            }
        }

        int GetCurrentAugmentRollCost()
        {
            int baseCostWithIncrease = AugmentRollCost + (m_AugmentRollCount * AugmentRollPriceIncreasePerRoll);
            if (m_ScoreManager == null)
            {
                return baseCostWithIncrease;
            }

            return m_ScoreManager.GetAdjustedShopRollCost(baseCostWithIncrease);
        }

        int GetCurrentAmmoPurchaseCost()
        {
            int baseCostWithIncrease = AmmoPurchaseCost + (m_AmmoPurchaseCount * AmmoPriceIncreasePerPurchase);
            if (m_ScoreManager == null)
            {
                return baseCostWithIncrease;
            }

            return m_ScoreManager.GetAdjustedAmmoPurchaseCost(baseCostWithIncrease);
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

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (m_EventManager != null)
            {
                m_EventManager.ScheduleRoundIntro(PendingNextRoundIndex);
            }
            else if (m_GameFlowManager != null)
            {
                m_GameFlowManager.ScheduleAimHighRound(PendingNextRoundIndex, 0f);
            }
        }
    }
}
