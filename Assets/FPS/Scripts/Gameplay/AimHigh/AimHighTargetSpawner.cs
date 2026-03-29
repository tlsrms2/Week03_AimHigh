using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay.AimHigh
{
    [System.Serializable]
    public class AimHighSpawnVolumeRule
    {
        [Tooltip("Box collider used as a random spawn volume")]
        public BoxCollider SpawnVolume;

        [Tooltip("Target prefabs that can spawn from this volume")]
        public List<GameObject> TargetPrefabs = new List<GameObject>();

        [Tooltip("First round where this volume can spawn targets")]
        public int MinRound = 1;

        [Tooltip("Last round where this volume can spawn targets. Use 0 or less for no upper limit")]
        public int MaxRound = 0;
    }

    [System.Serializable]
    public class AimHighSpawnIntervalRule
    {
        [Tooltip("First round where this spawn interval is used")]
        public int MinRound = 1;

        [Tooltip("Last round where this spawn interval is used. Use 0 or less for no upper limit")]
        public int MaxRound = 0;

        [Tooltip("Time between spawn attempts for this round range")]
        public float SpawnInterval = 1f;
    }

    public class AimHighTargetSpawner : MonoBehaviour, IAimHighTargetSpawner
    {
        [Header("Spawn Volume")]
        [Tooltip("Spawn volumes and the round range where each one is active")]
        public List<AimHighSpawnVolumeRule> SpawnVolumes = new List<AimHighSpawnVolumeRule>();

        [Tooltip("Minimum distance kept between active targets when using random area spawn")]
        public float MinDistanceBetweenTargets = 1.5f;

        [Tooltip("How many times to try finding a valid random point before giving up")]
        public int RandomSpawnAttempts = 12;

        [Tooltip("Default time between spawn attempts when no round rule matches")]
        public float DefaultSpawnInterval = 1f;

        [Tooltip("Optional per-round spawn interval overrides")]
        public List<AimHighSpawnIntervalRule> SpawnIntervalRules = new List<AimHighSpawnIntervalRule>();

        [Tooltip("Maximum number of active targets created by this spawner")]
        public int MaxActiveTargets = 5;

        readonly List<AimHighTarget> m_ActiveTargets = new List<AimHighTarget>();
        GameFlowManager m_GameFlowManager;
        bool m_IsActive;
        float m_NextSpawnTime;

        void Awake()
        {
            m_GameFlowManager = FindFirstObjectByType<GameFlowManager>();
        }

        void Update()
        {
            if (!m_IsActive || Time.time < m_NextSpawnTime)
            {
                return;
            }

            TrySpawnTarget();
            m_NextSpawnTime = Time.time + GetCurrentSpawnInterval();
        }

        public void Begin()
        {
            m_IsActive = true;
            m_NextSpawnTime = Time.time;
        }

        public void Stop()
        {
            m_IsActive = false;
        }

        public void ClearTargets()
        {
            for (int i = m_ActiveTargets.Count - 1; i >= 0; i--)
            {
                if (m_ActiveTargets[i] != null)
                {
                    Destroy(m_ActiveTargets[i].gameObject);
                }
            }

            m_ActiveTargets.Clear();
        }

        public void NotifyTargetRemoved(AimHighTarget target, bool shouldRespawn)
        {
            m_ActiveTargets.Remove(target);

            if (shouldRespawn && m_IsActive)
            {
                TrySpawnTarget();
            }
        }

        void TrySpawnTarget()
        {
            m_ActiveTargets.RemoveAll(target => target == null);

            if (m_ActiveTargets.Count >= MaxActiveTargets)
            {
                return;
            }

            Vector3 spawnPosition;
            Quaternion spawnRotation;
            AimHighSpawnVolumeRule selectedVolume;
            if (!TryGetRandomAreaSpawnPose(out selectedVolume, out spawnPosition, out spawnRotation))
            {
                return;
            }

            if (!TryGetRandomPrefab(selectedVolume, out GameObject prefab))
            {
                return;
            }
            GameObject targetInstance = Instantiate(prefab, spawnPosition, spawnRotation);
            AimHighTarget target = targetInstance.GetComponent<AimHighTarget>();
            if (target != null)
            {
                target.SetSpawner(this);
                m_ActiveTargets.Add(target);
            }

            IAimHighSpawnVolumeAware[] volumeAwareComponents =
                targetInstance.GetComponents<IAimHighSpawnVolumeAware>();
            for (int i = 0; i < volumeAwareComponents.Length; i++)
            {
                volumeAwareComponents[i].SetSpawnVolume(selectedVolume.SpawnVolume);
            }
        }

        bool TryGetRandomAreaSpawnPose(out AimHighSpawnVolumeRule selectedVolume, out Vector3 spawnPosition, out Quaternion spawnRotation)
        {
            selectedVolume = GetRandomAvailableVolume();
            if (selectedVolume == null || selectedVolume.SpawnVolume == null)
            {
                spawnPosition = transform.position;
                spawnRotation = transform.rotation;
                return false;
            }

            BoxCollider spawnVolume = selectedVolume.SpawnVolume;
            spawnRotation = spawnVolume.transform.rotation;

            for (int attempt = 0; attempt < Mathf.Max(1, RandomSpawnAttempts); attempt++)
            {
                Vector3 localOffset = new Vector3(
                    Random.Range(-spawnVolume.size.x * 0.5f, spawnVolume.size.x * 0.5f),
                    Random.Range(-spawnVolume.size.y * 0.5f, spawnVolume.size.y * 0.5f),
                    Random.Range(-spawnVolume.size.z * 0.5f, spawnVolume.size.z * 0.5f));

                Vector3 candidatePosition = spawnVolume.transform.TransformPoint(spawnVolume.center + localOffset);
                if (IsFarEnoughFromActiveTargets(candidatePosition))
                {
                    spawnPosition = candidatePosition;
                    return true;
                }
            }

            spawnPosition = spawnVolume.bounds.center;
            return false;
        }

        AimHighSpawnVolumeRule GetRandomAvailableVolume()
        {
            if (SpawnVolumes == null || SpawnVolumes.Count == 0)
            {
                return null;
            }

            int currentRound = m_GameFlowManager != null ? m_GameFlowManager.AimHighCurrentRoundIndex : 1;
            List<AimHighSpawnVolumeRule> availableVolumes = new List<AimHighSpawnVolumeRule>();
            for (int i = 0; i < SpawnVolumes.Count; i++)
            {
                AimHighSpawnVolumeRule volumeRule = SpawnVolumes[i];
                if (volumeRule == null || volumeRule.SpawnVolume == null)
                {
                    continue;
                }

                if (!HasValidTargetPrefabs(volumeRule))
                {
                    continue;
                }

                bool roundAllowed = currentRound >= Mathf.Max(1, volumeRule.MinRound) &&
                                    (volumeRule.MaxRound <= 0 || currentRound <= volumeRule.MaxRound);
                if (roundAllowed)
                {
                    availableVolumes.Add(volumeRule);
                }
            }

            if (availableVolumes.Count == 0)
            {
                return null;
            }

            return availableVolumes[Random.Range(0, availableVolumes.Count)];
        }

        bool HasValidTargetPrefabs(AimHighSpawnVolumeRule volumeRule)
        {
            if (volumeRule == null || volumeRule.TargetPrefabs == null)
            {
                return false;
            }

            volumeRule.TargetPrefabs.RemoveAll(prefab => prefab == null);
            return volumeRule.TargetPrefabs.Count > 0;
        }

        bool TryGetRandomPrefab(AimHighSpawnVolumeRule volumeRule, out GameObject prefab)
        {
            prefab = null;
            if (!HasValidTargetPrefabs(volumeRule))
            {
                return false;
            }

            int startIndex = Random.Range(0, volumeRule.TargetPrefabs.Count);
            for (int i = 0; i < volumeRule.TargetPrefabs.Count; i++)
            {
                GameObject candidatePrefab =
                    volumeRule.TargetPrefabs[(startIndex + i) % volumeRule.TargetPrefabs.Count];
                if (candidatePrefab != null)
                {
                    prefab = candidatePrefab;
                    return true;
                }
            }

            return false;
        }

        float GetCurrentSpawnInterval()
        {
            int currentRound = m_GameFlowManager != null ? m_GameFlowManager.AimHighCurrentRoundIndex : 1;
            for (int i = 0; i < SpawnIntervalRules.Count; i++)
            {
                AimHighSpawnIntervalRule intervalRule = SpawnIntervalRules[i];
                if (intervalRule == null)
                {
                    continue;
                }

                bool roundAllowed = currentRound >= Mathf.Max(1, intervalRule.MinRound) &&
                                    (intervalRule.MaxRound <= 0 || currentRound <= intervalRule.MaxRound);
                if (roundAllowed)
                {
                    return Mathf.Max(0.01f, intervalRule.SpawnInterval);
                }
            }

            return Mathf.Max(0.01f, DefaultSpawnInterval);
        }

        bool IsFarEnoughFromActiveTargets(Vector3 candidatePosition)
        {
            if (MinDistanceBetweenTargets <= 0f)
            {
                return true;
            }

            for (int i = 0; i < m_ActiveTargets.Count; i++)
            {
                AimHighTarget activeTarget = m_ActiveTargets[i];
                if (activeTarget == null)
                {
                    continue;
                }

                if (Vector3.Distance(candidatePosition, activeTarget.transform.position) < MinDistanceBetweenTargets)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
