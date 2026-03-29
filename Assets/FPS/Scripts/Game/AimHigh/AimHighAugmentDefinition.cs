  using UnityEngine;

namespace Unity.FPS.Game
{
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
        public float FloatValue = 1f;
        public int IntValue = 0;
    }
}
