namespace Unity.FPS.Game
{
    public interface IAimHighWeaponInventory
    {
        void SetPassiveAmmoRefill(bool enabled);
        AimHighWeaponController GetShopWeapon();
        void PrepareAimHighWeapon(int clipSize, int reserveAmmo);
        void RefillWeaponsForNewRound();
        bool AddWeapon(AimHighWeaponController weaponPrefab);
        bool RemoveWeapon(AimHighWeaponController weaponInstance);
        AimHighWeaponController HasWeapon(AimHighWeaponController weaponPrefab);
        void SwitchToWeapon(AimHighWeaponController weaponInstance);
        void AddAmmoToDefaultWeapon(int amount);
    }
}
