using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay.AimHigh
{
    public class AimHighSidewaysTarget : MonoBehaviour, IAimHighSpawnVolumeAware, IAimHighMovingTarget
    {
        [Tooltip("Horizontal (Local X) movement speed inside the spawn volume")]
        public float MoveSpeed = 5f;

        [Tooltip("Extra padding kept from the left and right edges of the spawn volume")]
        public float HorizontalPadding = 0.1f;

        [Tooltip("Randomize the initial movement direction on spawn")]
        public bool RandomizeInitialDirection = true;

        BoxCollider m_SpawnVolume;
        float m_LocalY;
        float m_LocalZ;
        int m_MoveDirection = 1;
        bool m_IsInitialized;
        Rigidbody m_Rigidbody;

        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            if (m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = true;
            }
        }

        void Start()
        {
            if (RandomizeInitialDirection)
            {
                m_MoveDirection = Random.value < 0.5f ? -1 : 1;
            }
        }

        void Update()
        {
            if (!m_IsInitialized || m_SpawnVolume == null)
            {
                return;
            }

            // Get current local position relative to the volume center
            Vector3 localPosition = m_SpawnVolume.transform.InverseTransformPoint(transform.position) - m_SpawnVolume.center;
            
            float halfWidth = Mathf.Max(0f, (m_SpawnVolume.size.x * 0.5f) - HorizontalPadding);
            float nextLocalX = localPosition.x + (MoveSpeed * m_MoveDirection * Time.deltaTime);

            if (nextLocalX > halfWidth)
            {
                nextLocalX = halfWidth;
                m_MoveDirection = -1;
            }
            else if (nextLocalX < -halfWidth)
            {
                nextLocalX = -halfWidth;
                m_MoveDirection = 1;
            }

            // Reconstruct local position keeping Y and Z fixed
            Vector3 clampedLocalPosition = new Vector3(nextLocalX, m_LocalY, m_LocalZ) + m_SpawnVolume.center;
            
            // Apply world position by transforming back
            transform.position = m_SpawnVolume.transform.TransformPoint(clampedLocalPosition);
        }

        public void SetSpawnVolume(BoxCollider spawnVolume)
        {
            m_SpawnVolume = spawnVolume;
            if (m_SpawnVolume == null)
            {
                m_IsInitialized = false;
                return;
            }

            // Store the initial Y and Z local positions so we only move on X
            Vector3 localPosition = m_SpawnVolume.transform.InverseTransformPoint(transform.position) - m_SpawnVolume.center;
            m_LocalY = localPosition.y;
            m_LocalZ = localPosition.z;
            m_IsInitialized = true;
        }
    }
}
