using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.FPS.Game
{
    public class AimHighScoreManager : MonoBehaviour
    {
        [FormerlySerializedAs("BaseMultiplier")]
        [Tooltip("Base multiplier applied to score gain")]
        public float BaseQuotaMultiplier = 1f;

        [Tooltip("Base multiplier applied to gold gain")]
        public float BaseGoldMultiplier = 1f;

        public int CurrentRoundScore { get; private set; }
        public int CurrentRoundQuotaProgress { get; private set; }
        public int TotalScore { get; private set; }
        public int Currency { get; private set; }
        public float QuotaMultiplier { get; private set; }
        public float GoldMultiplier { get; private set; }
        public int FlatGoldBonus { get; private set; }
        public float ShopRollCostMultiplier { get; private set; } = 1f;
        public float AmmoPurchaseCostMultiplier { get; private set; } = 1f;
        public float DistanceGoldBonusFactor { get; private set; }
        public float MovingTargetGoldMultiplier { get; private set; } = 1f;

        void Awake()
        {
            QuotaMultiplier = Mathf.Max(1f, BaseQuotaMultiplier);
            GoldMultiplier = Mathf.Max(1f, BaseGoldMultiplier);
            BroadcastScoreChanged();
        }

        public void ResetRoundScore()
        {
            CurrentRoundScore = 0;
            CurrentRoundQuotaProgress = 0;
            BroadcastScoreChanged();
        }

        public int AddScore(int baseScore, float distanceMultiplier = 1f, float extraMultiplier = 1f, int flatBonus = 0)
        {
            float computedScore = baseScore;
            computedScore *= Mathf.Max(1f, distanceMultiplier);
            computedScore *= Mathf.Max(1f, extraMultiplier);
            computedScore *= QuotaMultiplier;

            int finalScore = Mathf.Max(1, Mathf.RoundToInt(computedScore) + flatBonus);
            CurrentRoundScore += finalScore;
            TotalScore += finalScore;
            BroadcastScoreChanged();
            return finalScore;
        }

        public int AddQuotaProgress(int baseQuotaGain)
        {
            int computedQuota = Mathf.Max(0, Mathf.RoundToInt(baseQuotaGain * Mathf.Max(1f, QuotaMultiplier)));
            int finalQuota = Mathf.Max(1, computedQuota);
            CurrentRoundQuotaProgress += finalQuota;
            BroadcastScoreChanged();
            return finalQuota;
        }

        public int AddGold(int baseGold, float distance = 0f, bool isMovingTarget = false)
        {
            if (baseGold <= 0)
            {
                return 0;
            }

            // 1. 기본 골드 계산 (전역 골드 배수 적용)
            float computedGold = baseGold * GoldMultiplier;

            // 2. 거리 보너스만 '퍼센트(곱연산)'로 적용
            float distanceFactor = 1f + (Mathf.Sqrt(Mathf.Max(0f, distance)) * Mathf.Max(0f, DistanceGoldBonusFactor));
            computedGold *= distanceFactor;

            // 3. 움직이는 타겟 보너스를 '절대값'으로 더함
            if (isMovingTarget)
            {
                // 참고: 기존 변수명은 Multiplier지만, 이제는 절대값(예: +10G)으로 더해집니다.
                computedGold += MovingTargetGoldMultiplier; 
            }

            // 4. 고정 보너스를 '절대값'으로 더함
            computedGold += FlatGoldBonus;

            // 최종 정산
            int finalGold = Mathf.Max(1, Mathf.RoundToInt(computedGold));
            Currency += finalGold;
            CurrentRoundQuotaProgress += finalGold;
            BroadcastScoreChanged();
            
            return finalGold;
        }

        public bool TrySpendCurrency(int amount)
        {
            if (amount <= 0 || Currency < amount)
            {
                return false;
            }

            Currency -= amount;
            BroadcastScoreChanged();
            return true;
        }

        public int SubtractCurrency(int amount)
        {
            if (amount <= 0) return 0;
            int actualSubtract = Mathf.Min(Currency, amount);
            Currency -= actualSubtract;
            BroadcastScoreChanged();
            return actualSubtract;
        }

        public void AddDirectCurrency(int amount)
        {
            if (amount <= 0) return;
            Currency += amount;
            BroadcastScoreChanged();
        }

        public void AddQuotaMultiplier(float amount)
        {
            QuotaMultiplier = Mathf.Max(1f, QuotaMultiplier + amount);
            BroadcastScoreChanged();
        }

        public void AddGoldMultiplier(float amount)
        {
            GoldMultiplier = Mathf.Max(1f, GoldMultiplier + amount);
            BroadcastScoreChanged();
        }

        public void AddFlatGoldBonus(int amount)
        {
            FlatGoldBonus += amount;
            BroadcastScoreChanged();
        }

        public void AddShopRollCostReduction(float amount)
        {
            ShopRollCostMultiplier = Mathf.Clamp(ShopRollCostMultiplier - amount, 0.1f, 1f);
            BroadcastScoreChanged();
        }

        public int GetAdjustedShopRollCost(int baseCost)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseCost * ShopRollCostMultiplier));
        }

        public void AddAmmoPurchaseCostReduction(float amount)
        {
            AmmoPurchaseCostMultiplier = Mathf.Clamp(AmmoPurchaseCostMultiplier - amount, 0.1f, 1f);
            BroadcastScoreChanged();
        }

        public int GetAdjustedAmmoPurchaseCost(int baseCost)
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseCost * AmmoPurchaseCostMultiplier));
        }

        public void AddDistanceGoldBonusFactor(float amount)
        {
            DistanceGoldBonusFactor = Mathf.Max(0f, DistanceGoldBonusFactor + amount);
            BroadcastScoreChanged();
        }

        public void AddMovingTargetGoldMultiplier(float amount)
        {
            MovingTargetGoldMultiplier = Mathf.Max(1f, MovingTargetGoldMultiplier + amount);
            BroadcastScoreChanged();
        }

        void BroadcastScoreChanged()
        {
            AimHighScoreChangedEvent scoreChangedEvent = Events.AimHighScoreChangedEvent;
            scoreChangedEvent.RoundScore = CurrentRoundScore;
            scoreChangedEvent.TotalScore = TotalScore;
            scoreChangedEvent.Currency = Currency;
            scoreChangedEvent.Multiplier = QuotaMultiplier;
            EventManager.Broadcast(scoreChangedEvent);
        }
    }
}
