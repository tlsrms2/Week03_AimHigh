using UnityEngine;

namespace Unity.FPS.Gameplay.AimHigh
{
    public class AimHighMovingTarget : MonoBehaviour, IAimHighSpawnVolumeAware, IAimHighMovingTarget
    {
        [Tooltip("Horizontal movement speed inside the spawn volume")]
        public float MoveSpeed = 2f;

        [Tooltip("Extra padding kept from the left and right edge of the spawn volume")]
        public float HorizontalPadding = 0.25f;

        [Tooltip("Randomize the initial movement direction on spawn")]
        public bool RandomizeInitialDirection = true;

        BoxCollider m_SpawnVolume;
        float m_LocalY;
        float m_LocalZ;
        int m_MoveDirection = 1;
        bool m_IsInitialized;

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

            Vector3 clampedLocalPosition = new Vector3(nextLocalX, m_LocalY, m_LocalZ) + m_SpawnVolume.center;
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

            Vector3 localPosition = m_SpawnVolume.transform.InverseTransformPoint(transform.position) - m_SpawnVolume.center;
            m_LocalY = localPosition.y;
            m_LocalZ = localPosition.z;
            m_IsInitialized = true;
        }
    }
}
