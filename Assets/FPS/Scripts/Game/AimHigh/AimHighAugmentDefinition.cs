  using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    public enum AimHighAugmentRarity
    {
        Normal,
        Rare,
        Epic,
        Legendary
    }

    [System.Serializable]
    public struct AimHighAugmentRarityData
    {
        public AimHighAugmentRarity Rarity;
        public float FloatValue;
        public int IntValue;
        [Range(0f, 1f)] public float Weight;

        public AimHighAugmentRarityData(AimHighAugmentRarity rarity)
        {
            Rarity = rarity;
            FloatValue = 0f;
            IntValue = 0;
            Weight = 0.25f;
        }
    }

    public enum AimHighAugmentEffectType
    {
        GoldGainMultiplier,
        WeaponDamageMultiplier,
        ReloadSpeedMultiplier,
        ClipSizeBonus,
        ProjectileRangeMultiplier,
        ShopRollCostReduction,
        AmmoPurchaseCostReduction,
        FireRateMultiplier,
        DistanceGoldBonusFactor,
        MovingTargetGoldMultiplier
    }

    [CreateAssetMenu(fileName = "AimHighAugment", menuName = "Aim High/Augment Definition")]
    public class AimHighAugmentDefinition : ScriptableObject
    {
        public string Id = "augment_id";
        public string DisplayName = "New Augment";

        [TextArea] public string Description = "Permanent bonus for the current run";

        public AimHighAugmentEffectType EffectType;

        [Header("Rarity Settings")]
        public List<AimHighAugmentRarityData> Rarities = new List<AimHighAugmentRarityData>
        {
            new AimHighAugmentRarityData(AimHighAugmentRarity.Normal) { Weight = 0.5f },
            new AimHighAugmentRarityData(AimHighAugmentRarity.Rare) { Weight = 0.3f },
            new AimHighAugmentRarityData(AimHighAugmentRarity.Epic) { Weight = 0.15f },
            new AimHighAugmentRarityData(AimHighAugmentRarity.Legendary) { Weight = 0.05f }
        };

        [Header("Visuals (Shared colors per rarity or unique?)")]
        public Color NormalColor = Color.white;
        public Color RareColor = Color.blue;
        public Color EpicColor = Color.magenta;
        public Color LegendaryColor = Color.yellow;

        public AimHighAugmentRarityData GetRarityData(AimHighAugmentRarity rarity)
        {
            for (int i = 0; i < Rarities.Count; i++)
            {
                if (Rarities[i].Rarity == rarity) return Rarities[i];
            }
            return new AimHighAugmentRarityData(rarity);
        }

        public Color GetColor(AimHighAugmentRarity rarity)
        {
            switch (rarity)
            {
                case AimHighAugmentRarity.Normal: return NormalColor;
                case AimHighAugmentRarity.Rare: return RareColor;
                case AimHighAugmentRarity.Epic: return EpicColor;
                case AimHighAugmentRarity.Legendary: return LegendaryColor;
                default: return Color.white;
            }
        }
    }

    [System.Serializable]
    public class AimHighAugmentInstance
    {
        public AimHighAugmentDefinition Definition;
        public AimHighAugmentRarity Rarity;

        public float FloatValue => Definition.GetRarityData(Rarity).FloatValue;
        public int IntValue => Definition.GetRarityData(Rarity).IntValue;
        public Color RarityColor => Definition.GetColor(Rarity);

        public AimHighAugmentInstance(AimHighAugmentDefinition definition, AimHighAugmentRarity rarity)
        {
            Definition = definition;
            Rarity = rarity;
        }
    }
}
