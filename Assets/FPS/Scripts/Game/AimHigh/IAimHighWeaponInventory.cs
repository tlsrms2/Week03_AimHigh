namespace Unity.FPS.Game
{
    public interface IAimHighWeaponInventory
    {
        void SetPassiveAmmoRefill(bool enabled);
        AimHighWeaponController GetShopWeapon();
        void PrepareAimHighWeapon(int clipSize, int reserveAmmo);
        void RefillWeaponsForNewRound();
    }
}
