using UnityEngine;

namespace Unity.FPS.Gameplay.AimHigh
{
    public class AimHighVerticalMovingTarget : MonoBehaviour, IAimHighSpawnVolumeAware, IAimHighMovingTarget
    {
        [Tooltip("Vertical movement speed inside the spawn volume")]
        public float MoveSpeed = 2f;

        [Tooltip("Extra padding kept from the top and bottom edge of the spawn volume")]
        public float VerticalPadding = 0.25f;

        [Tooltip("Randomize the initial movement direction on spawn")]
        public bool RandomizeInitialDirection = true;

        BoxCollider m_SpawnVolume;
        float m_LocalX;
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
            float halfHeight = Mathf.Max(0f, (m_SpawnVolume.size.y * 0.5f) - VerticalPadding);
            float nextLocalY = localPosition.y + (MoveSpeed * m_MoveDirection * Time.deltaTime);

            if (nextLocalY > halfHeight)
            {
                nextLocalY = halfHeight;
                m_MoveDirection = -1;
            }
            else if (nextLocalY < -halfHeight)
            {
                nextLocalY = -halfHeight;
                m_MoveDirection = 1;
            }

            Vector3 clampedLocalPosition = new Vector3(m_LocalX, nextLocalY, m_LocalZ) + m_SpawnVolume.center;
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
            m_LocalX = localPosition.x;
            m_LocalZ = localPosition.z;
            m_IsInitialized = true;
        }
    }
}
