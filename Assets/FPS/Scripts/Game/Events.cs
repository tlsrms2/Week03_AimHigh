using UnityEngine;

namespace Unity.FPS.Game
{
    // The Game Events used across the Game.
    // Anytime there is a need for a new event, it should be added here.

    public static class Events
    {
        public static ObjectiveUpdateEvent ObjectiveUpdateEvent = new ObjectiveUpdateEvent();
        public static AllObjectivesCompletedEvent AllObjectivesCompletedEvent = new AllObjectivesCompletedEvent();
        public static GameOverEvent GameOverEvent = new GameOverEvent();
        public static PlayerDeathEvent PlayerDeathEvent = new PlayerDeathEvent();
        public static EnemyKillEvent EnemyKillEvent = new EnemyKillEvent();
        public static PickupEvent PickupEvent = new PickupEvent();
        public static AmmoPickupEvent AmmoPickupEvent = new AmmoPickupEvent();
        public static DamageEvent DamageEvent = new DamageEvent();
        public static DisplayMessageEvent DisplayMessageEvent = new DisplayMessageEvent();
        public static AimHighRoundStartedEvent AimHighRoundStartedEvent = new AimHighRoundStartedEvent();
        public static AimHighRoundEndedEvent AimHighRoundEndedEvent = new AimHighRoundEndedEvent();
        public static AimHighScoreChangedEvent AimHighScoreChangedEvent = new AimHighScoreChangedEvent();
        public static AimHighQuotaChangedEvent AimHighQuotaChangedEvent = new AimHighQuotaChangedEvent();
        public static AimHighShopStateChangedEvent AimHighShopStateChangedEvent = new AimHighShopStateChangedEvent();
    }

    public class ObjectiveUpdateEvent : GameEvent
    {
        public Objective Objective;
        public string DescriptionText;
        public string CounterText;
        public bool IsComplete;
        public string NotificationText;
    }

    public class AllObjectivesCompletedEvent : GameEvent { }

    public class GameOverEvent : GameEvent
    {
        public bool Win;
    }

    public class PlayerDeathEvent : GameEvent { }

    public class EnemyKillEvent : GameEvent
    {
        public GameObject Enemy;
        public int RemainingEnemyCount;
    }

    public class PickupEvent : GameEvent
    {
        public GameObject Pickup;
    }

    public class AmmoPickupEvent : GameEvent
    {
        public WeaponController Weapon;
    }

    public class DamageEvent : GameEvent
    {
        public GameObject Sender;
        public float DamageValue;
    }

    public class DisplayMessageEvent : GameEvent
    {
        public string Message;
        public float DelayBeforeDisplay;
    }

    public class AimHighRoundStartedEvent : GameEvent
    {
        public int RoundIndex;
        public float Duration;
        public int Quota;
    }

    public class AimHighRoundEndedEvent : GameEvent
    {
        public int RoundIndex;
        public bool Success;
        public int Score;
        public int QuotaProgress;
        public int Quota;
    }

    public class AimHighScoreChangedEvent : GameEvent
    {
        public int RoundScore;
        public int TotalScore;
        public int Currency;
        public float Multiplier;
    }

    public class AimHighQuotaChangedEvent : GameEvent
    {
        public int RoundIndex;
        public int QuotaProgress;
        public int Quota;
        public float TimeRemaining;
    }

    public class AimHighShopStateChangedEvent : GameEvent
    {
        public bool IsOpen;
        public int NextRoundIndex;
    }
}
