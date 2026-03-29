using System;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Game
{
    public enum AimHighWeaponShootType
    {
        Manual,
        Automatic,
    }

    [Serializable]
    public struct AimHighCrosshairData
    {
        public Sprite CrosshairSprite;
        public int CrosshairSize;
        public Color CrosshairColor;
    }

    [RequireComponent(typeof(AudioSource))]
    public class AimHighWeaponController : MonoBehaviour
    {
        [Header("Information")]
        public string WeaponName;
        public AimHighCrosshairData CrosshairDataDefault;
        public AimHighCrosshairData CrosshairDataTargetInSight;

        [Header("Internal References")]
        public GameObject WeaponRoot;
        public Transform WeaponMuzzle;

        [Header("Shoot Parameters")]
        public AimHighWeaponShootType ShootType = AimHighWeaponShootType.Automatic;
        public ProjectileBase ProjectilePrefab;
        public float DelayBetweenShots = 0.1f;
        public float BulletSpreadAngle = 0f;
        public int BulletsPerShot = 1;
        public float ProjectileSpeedMultiplier = 4f;

        [Header("Crosshair")]
        [Tooltip("When enabled, the configured crosshair size becomes the source of truth and shot spread is derived from it")]
        public bool SyncSpreadToCrosshairSize = true;

        [Tooltip("Fallback size to use when exact screen-space conversion is unavailable")]
        public int FallbackCrosshairSize = 32;

        [Header("Damage")]
        public float BaseDamageMultiplier = 1f;
        [Tooltip("Base multiplier applied to the crosshair size and attack radius. 1.0 means 100%")]
        public float BaseProjectileRangeMultiplier = 1f;

        [Header("Ammo")]
        public int ClipSize = 30;
        public int MaxReserveAmmo = 90;
        public float ReloadDuration = 1f;

        [Header("Audio & Visual")]
        public GameObject MuzzleFlashPrefab;
        public bool UnparentMuzzleFlash;
        public AudioClip ShootSfx;

        [Header("Debug")]
        [Tooltip("Draw the camera center ray and actual shot direction for debugging")]
        public bool DrawDebugShotRays;

        [Tooltip("How long the debug rays stay visible")]
        public float DebugRayDuration = 1.5f;

        [Tooltip("Length of the debug rays")]
        public float DebugRayLength = 100f;

        [Tooltip("Color for the center-of-screen ray")]
        public Color DebugCenterRayColor = Color.yellow;

        [Tooltip("Color for the actual shot ray")]
        public Color DebugShotRayColor = Color.red;

        public UnityAction OnShoot;
        public event Action OnShootProcessed;

        public GameObject Owner { get; set; }
        public GameObject SourcePrefab { get; set; }
        public Camera ReferenceCamera { get; set; }
        public float DisplayedCrosshairSizePixels { get; set; }
        public bool IsWeaponActive { get; private set; }
        public bool IsReloading { get; private set; }
        public bool IsCharging => false;
        public bool UseMagazineSystem => true;
        public bool HasPhysicalBullets => true;
        public float CurrentAmmoRatio { get; private set; }
        public Vector3 MuzzleWorldVelocity { get; private set; }
        public float DamageMultiplier { get; private set; }
        public float ReloadSpeedMultiplier { get; private set; }
        public float CrosshairSizeMultiplier { get; private set; }
        public float FireRateMultiplier { get; private set; }
        public int CurrentClipSize => Mathf.Max(1, ClipSize + m_AdditionalClipSize);

        AudioSource m_ShootAudioSource;
        float m_LastTimeShot = Mathf.NegativeInfinity;
        float m_ReloadEndTime = Mathf.NegativeInfinity;
        int m_CurrentAmmoInClip;
        int m_CurrentReserveAmmo;
        int m_AdditionalClipSize;
        Vector3 m_LastMuzzlePosition;

        void Awake()
        {
            m_ShootAudioSource = GetComponent<AudioSource>();
            DebugUtility.HandleErrorIfNullGetComponent<AudioSource, AimHighWeaponController>(m_ShootAudioSource, this,
                gameObject);

            DamageMultiplier = Mathf.Max(0.01f, BaseDamageMultiplier);
            ReloadSpeedMultiplier = 1f;
            CrosshairSizeMultiplier = Mathf.Max(1f, BaseProjectileRangeMultiplier);
            FireRateMultiplier = 1f;

            m_CurrentAmmoInClip = CurrentClipSize;
            m_CurrentReserveAmmo = Mathf.Max(0, MaxReserveAmmo);
            CurrentAmmoRatio = (float)m_CurrentAmmoInClip / CurrentClipSize;
            m_LastMuzzlePosition = WeaponMuzzle != null ? WeaponMuzzle.position : transform.position;
        }

        void Update()
        {
            if (WeaponMuzzle != null && Time.deltaTime > 0f)
            {
                MuzzleWorldVelocity = (WeaponMuzzle.position - m_LastMuzzlePosition) / Time.deltaTime;
                m_LastMuzzlePosition = WeaponMuzzle.position;
            }

            if (IsReloading && Time.time >= m_ReloadEndTime)
            {
                FinishReload();
            }
            else if (!IsReloading && m_CurrentAmmoInClip <= 0 && m_CurrentReserveAmmo > 0)
            {
                BeginAutoReload();
            }
        }

        public void ShowWeapon(bool show)
        {
            if (WeaponRoot != null)
            {
                WeaponRoot.SetActive(show);
            }

            IsWeaponActive = show;
        }

        public int GetCurrentAmmo()
        {
            return m_CurrentAmmoInClip;
        }

        public int GetMagazineCapacity()
        {
            return CurrentClipSize;
        }

        public int GetCarriedPhysicalBullets()
        {
            return m_CurrentReserveAmmo;
        }

        public float GetReloadTimeRemaining()
        {
            if (!IsReloading)
            {
                return 0f;
            }

            return Mathf.Max(0f, m_ReloadEndTime - Time.time);
        }

        public float GetAmmoNeededToShoot()
        {
            return CurrentClipSize <= 0 ? 1f : 1f / CurrentClipSize;
        }

        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            if (IsReloading)
            {
                return false;
            }

            switch (ShootType)
            {
                case AimHighWeaponShootType.Manual:
                    return inputDown && TryShoot();
                case AimHighWeaponShootType.Automatic:
                    return inputHeld && TryShoot();
                default:
                    return false;
            }
        }

        public void AddAmmo(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            m_CurrentReserveAmmo = Mathf.Clamp(m_CurrentReserveAmmo + Mathf.RoundToInt(amount), 0, MaxReserveAmmo);
        }

        public void FillAmmo()
        {
            m_CurrentAmmoInClip = CurrentClipSize;
            m_CurrentReserveAmmo = MaxReserveAmmo;
            UpdateAmmoRatio();
        }

        public void AddDamageMultiplier(float amount)
        {
            DamageMultiplier = Mathf.Max(0.01f, DamageMultiplier + amount);
        }

        public void AddReloadSpeedMultiplier(float amount)
        {
            ReloadSpeedMultiplier = Mathf.Max(0.1f, ReloadSpeedMultiplier + amount);
        }

        public void AddClipSizeBonus(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            m_AdditionalClipSize += amount;
            m_CurrentAmmoInClip = Mathf.Min(CurrentClipSize, m_CurrentAmmoInClip + amount);
            UpdateAmmoRatio();
        }

        public void AddFireRateMultiplier(float amount)
        {
            FireRateMultiplier = Mathf.Max(0.1f, FireRateMultiplier + amount);
        }

        public void AddProjectileRangeMultiplier(float amount)
        {
            CrosshairSizeMultiplier = Mathf.Max(1f, CrosshairSizeMultiplier + amount);
            DisplayedCrosshairSizePixels = 0f;
        }

        public void SetMagazineDefaults(int clipSize, int reserveAmmo)
        {
            ClipSize = Mathf.Max(1, clipSize);
            MaxReserveAmmo = Mathf.Max(0, reserveAmmo);
            m_AdditionalClipSize = 0;
            DamageMultiplier = Mathf.Max(0.01f, BaseDamageMultiplier);
            ReloadSpeedMultiplier = 1f;
            CrosshairSizeMultiplier = Mathf.Max(1f, BaseProjectileRangeMultiplier);
            FireRateMultiplier = 1f;
            DisplayedCrosshairSizePixels = 0f;
            m_CurrentAmmoInClip = CurrentClipSize;
            m_CurrentReserveAmmo = MaxReserveAmmo;
            IsReloading = false;
            m_ReloadEndTime = Mathf.NegativeInfinity;
            UpdateAmmoRatio();
        }

        public void RefillMagazineFromReserve()
        {
            int missingAmmo = CurrentClipSize - m_CurrentAmmoInClip;
            if (missingAmmo <= 0 || m_CurrentReserveAmmo <= 0)
            {
                return;
            }

            int ammoToLoad = Mathf.Min(m_CurrentReserveAmmo, missingAmmo);
            m_CurrentAmmoInClip += ammoToLoad;
            m_CurrentReserveAmmo -= ammoToLoad;
            UpdateAmmoRatio();
        }

        bool TryShoot()
        {
            if (WeaponMuzzle == null ||
                ProjectilePrefab == null ||
                m_CurrentAmmoInClip <= 0 ||
                m_LastTimeShot + GetCurrentDelayBetweenShots() >= Time.time)
            {
                return false;
            }

            Camera referenceCamera = GetReferenceCamera();
            bool hasLockedTarget = TryGetDirectionToTargetInsideCrosshair(referenceCamera, WeaponMuzzle,
                out Vector3 lockedDirection, out Damageable lockedDamageable);

            for (int i = 0; i < Mathf.Max(1, BulletsPerShot); i++)
            {
                Vector3 shotDirection = hasLockedTarget ? lockedDirection : GetShotDirectionWithinSpread(WeaponMuzzle);
                DrawDebugRays(shotDirection);
                ProjectileBase newProjectile = Instantiate(ProjectilePrefab, WeaponMuzzle.position,
                    Quaternion.LookRotation(shotDirection));
                ApplyProjectileSpeedBoost(newProjectile);
                newProjectile.Shoot(this);
            }

            if (hasLockedTarget && lockedDamageable != null && lockedDamageable.Health != null)
            {
                lockedDamageable.Health.Kill();
            }

            if (MuzzleFlashPrefab != null)
            {
                GameObject muzzleFlashInstance = Instantiate(MuzzleFlashPrefab, WeaponMuzzle.position,
                    WeaponMuzzle.rotation, WeaponMuzzle);
                if (UnparentMuzzleFlash)
                {
                    muzzleFlashInstance.transform.SetParent(null);
                }

                Destroy(muzzleFlashInstance, 2f);
            }

            if (ShootSfx != null)
            {
                m_ShootAudioSource.PlayOneShot(ShootSfx);
            }

            m_CurrentAmmoInClip = Mathf.Max(0, m_CurrentAmmoInClip - 1);
            m_LastTimeShot = Time.time;
            UpdateAmmoRatio();

            OnShoot?.Invoke();
            OnShootProcessed?.Invoke();
            return true;
        }

        void BeginAutoReload()
        {
            if (IsReloading || m_CurrentReserveAmmo <= 0 || m_CurrentAmmoInClip > 0)
            {
                return;
            }

            IsReloading = true;
            float reloadDuration = Mathf.Max(0.01f, ReloadDuration / ReloadSpeedMultiplier);
            m_ReloadEndTime = Time.time + reloadDuration;
        }

        float GetCurrentDelayBetweenShots()
        {
            return Mathf.Max(0.01f, DelayBetweenShots / Mathf.Max(0.1f, FireRateMultiplier));
        }

        void FinishReload()
        {
            int missingAmmo = CurrentClipSize - m_CurrentAmmoInClip;
            int ammoToLoad = Mathf.Min(m_CurrentReserveAmmo, missingAmmo);
            m_CurrentAmmoInClip += ammoToLoad;
            m_CurrentReserveAmmo -= ammoToLoad;
            IsReloading = false;
            UpdateAmmoRatio();
        }

        void UpdateAmmoRatio()
        {
            CurrentAmmoRatio = CurrentClipSize <= 0 ? 0f : (float)m_CurrentAmmoInClip / CurrentClipSize;
        }

        public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
        {
            Camera referenceCamera = GetReferenceCamera();
            if (SyncSpreadToCrosshairSize && referenceCamera != null)
            {
                if (TryGetDirectionToTargetInsideCrosshair(referenceCamera, shootTransform, out Vector3 lockedDirection))
                {
                    return lockedDirection;
                }

                Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Ray shotRay = referenceCamera.ScreenPointToRay(screenCenter);
                Vector3 targetPoint = shotRay.origin + (shotRay.direction * 1000f);
                return (targetPoint - shootTransform.position).normalized;
            }

            float spreadAngleDegrees = GetCurrentSpreadAngle(referenceCamera);
            float spreadAngleRatio = spreadAngleDegrees / 180f;
            return Vector3.Slerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);
        }

        public bool HasTargetInsideCrosshair(Camera referenceCamera)
        {
            if (referenceCamera == null)
            {
                return false;
            }

            Transform shootTransform = WeaponMuzzle != null ? WeaponMuzzle : transform;
            return TryGetDirectionToTargetInsideCrosshair(referenceCamera, shootTransform, out _, out _);
        }

        public AimHighCrosshairData GetCrosshairData(bool targetInSight, Camera referenceCamera = null)
        {
            AimHighCrosshairData data = targetInSight ? CrosshairDataTargetInSight : CrosshairDataDefault;
            if (SyncSpreadToCrosshairSize)
            {
                data.CrosshairSize = GetScaledCrosshairSize(data.CrosshairSize);
            }

            return data;
        }

        public float GetCurrentSpreadAngle(Camera referenceCamera = null)
        {
            if (!SyncSpreadToCrosshairSize)
            {
                return BulletSpreadAngle;
            }

            int crosshairSize = GetConfiguredCrosshairSize();
            if (referenceCamera == null || Screen.height <= 0)
            {
                return BulletSpreadAngle;
            }

            float spreadRadiusPixels = crosshairSize * 0.5f;
            float halfVerticalFovRadians = referenceCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            if (spreadRadiusPixels <= 0f || halfVerticalFovRadians <= 0f)
            {
                return 0f;
            }

            float tangent = (spreadRadiusPixels / (Screen.height * 0.5f)) * Mathf.Tan(halfVerticalFovRadians);
            return Mathf.Atan(tangent) * Mathf.Rad2Deg;
        }

        int GetConfiguredCrosshairSize()
        {
            if (DisplayedCrosshairSizePixels > 0f)
            {
                return Mathf.Max(1, Mathf.RoundToInt(DisplayedCrosshairSizePixels));
            }

            return GetScaledCrosshairSize(CrosshairDataDefault.CrosshairSize);
        }

        int GetScaledCrosshairSize(int authoredSize)
        {
            int baseSize = authoredSize > 0 ? authoredSize : FallbackCrosshairSize;
            return Mathf.Max(1, Mathf.RoundToInt(baseSize * CrosshairSizeMultiplier));
        }

        bool TryGetDirectionToTargetInsideCrosshair(Camera referenceCamera, Transform shootTransform, out Vector3 direction)
        {
            return TryGetDirectionToTargetInsideCrosshair(referenceCamera, shootTransform, out direction, out _);
        }

        bool TryGetDirectionToTargetInsideCrosshair(Camera referenceCamera, Transform shootTransform, out Vector3 direction,
            out Damageable lockedDamageable)
        {
            direction = shootTransform.forward;
            lockedDamageable = null;

            Damageable[] damageables = FindObjectsByType<Damageable>(FindObjectsSortMode.None);
            if (damageables == null || damageables.Length == 0)
            {
                return false;
            }

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float crosshairRadius = GetConfiguredCrosshairSize() * 0.5f;
            float bestScore = float.PositiveInfinity;
            Vector3 bestTargetPoint = Vector3.zero;
            bool foundTarget = false;

            for (int i = 0; i < damageables.Length; i++)
            {
                Damageable damageable = damageables[i];
                if (damageable == null || !damageable.isActiveAndEnabled)
                {
                    continue;
                }

                Collider[] colliders = damageable.GetComponentsInChildren<Collider>();
                for (int j = 0; j < colliders.Length; j++)
                {
                    Collider candidateCollider = colliders[j];
                    if (candidateCollider == null || !candidateCollider.enabled)
                    {
                        continue;
                    }

                    if (!IsColliderInsideCrosshair(referenceCamera, candidateCollider, screenCenter, crosshairRadius))
                    {
                        continue;
                    }

                    Vector3 targetPoint = candidateCollider.bounds.center;
                    Vector3 cameraToTarget = targetPoint - referenceCamera.transform.position;
                    float targetDistance = cameraToTarget.magnitude;
                    if (targetDistance <= 0f)
                    {
                        continue;
                    }

                    if (Physics.Raycast(referenceCamera.transform.position, cameraToTarget.normalized, out RaycastHit hit,
                            targetDistance + 0.01f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider != candidateCollider && hit.collider.GetComponentInParent<Damageable>() != damageable)
                        {
                            continue;
                        }
                    }

                    Vector3 screenPoint = referenceCamera.WorldToScreenPoint(targetPoint);
                    float score = (new Vector2(screenPoint.x, screenPoint.y) - screenCenter).sqrMagnitude;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestTargetPoint = targetPoint;
                        lockedDamageable = damageable;
                        foundTarget = true;
                    }
                }
            }

            if (!foundTarget)
            {
                return false;
            }

            direction = (bestTargetPoint - shootTransform.position).normalized;
            return true;
        }

        void ApplyProjectileSpeedBoost(ProjectileBase projectile)
        {
            if (projectile == null || ProjectileSpeedMultiplier <= 0f)
            {
                return;
            }

            Component[] components = projectile.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component.GetType().Name != "ProjectileStandard")
                {
                    continue;
                }

                var speedField = component.GetType().GetField("Speed");
                if (speedField != null && speedField.FieldType == typeof(float))
                {
                    float currentSpeed = (float)speedField.GetValue(component);
                    speedField.SetValue(component, currentSpeed * ProjectileSpeedMultiplier);
                }

                break;
            }
        }

        bool IsColliderInsideCrosshair(Camera referenceCamera, Collider candidateCollider, Vector2 screenCenter, float crosshairRadius)
        {
            Vector3 screenPoint = referenceCamera.WorldToScreenPoint(candidateCollider.bounds.center);
            if (screenPoint.z <= 0f)
            {
                return false;
            }

            float distanceToCenter = Vector2.Distance(screenCenter, new Vector2(screenPoint.x, screenPoint.y));
            return distanceToCenter <= crosshairRadius;
        }

        Camera GetReferenceCamera()
        {
            if (ReferenceCamera != null)
            {
                return ReferenceCamera;
            }

            if (Owner != null)
            {
                Camera ownerCamera = Owner.GetComponentInChildren<Camera>();
                if (ownerCamera != null)
                {
                    return ownerCamera;
                }
            }

            return Camera.main;
        }

        void DrawDebugRays(Vector3 shotDirection)
        {
            if (!DrawDebugShotRays)
            {
                return;
            }

            Camera referenceCamera = GetReferenceCamera();
            if (referenceCamera != null)
            {
                Debug.DrawRay(referenceCamera.transform.position, referenceCamera.transform.forward * DebugRayLength,
                    DebugCenterRayColor, DebugRayDuration);
            }

            if (WeaponMuzzle != null)
            {
                Debug.DrawRay(WeaponMuzzle.position, shotDirection * DebugRayLength, DebugShotRayColor,
                    DebugRayDuration);
            }
        }

        int GetExactCrosshairSize(Camera referenceCamera)
        {
            if (referenceCamera == null || Screen.height <= 0)
            {
                return GetConfiguredCrosshairSize();
            }

            float halfVerticalFovRadians = referenceCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            if (halfVerticalFovRadians <= 0f)
            {
                return GetConfiguredCrosshairSize();
            }

            float spreadAngle = SyncSpreadToCrosshairSize ? GetCurrentSpreadAngle(referenceCamera) : BulletSpreadAngle;
            float spreadRadiusPixels =
                Mathf.Tan(spreadAngle * Mathf.Deg2Rad) / Mathf.Tan(halfVerticalFovRadians) * (Screen.height * 0.5f);

            return Mathf.Max(1, Mathf.RoundToInt(spreadRadiusPixels * 2f));
        }
    }
}
