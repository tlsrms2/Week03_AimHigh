using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay.AimHigh
{
    public class AimHighForwardMovingTarget : MonoBehaviour, IAimHighSpawnVolumeAware, IAimHighMovingTarget
    {
        [Tooltip("Forward movement speed inside the spawn volume")]
        public float MoveSpeed = 2f;

        [Tooltip("Extra padding kept from the front and back edge of the spawn volume")]
        public float DepthPadding = 0.25f;

        [Tooltip("Randomize the initial movement direction on spawn")]
        public bool RandomizeInitialDirection = true;

        BoxCollider m_SpawnVolume;
        float m_LocalX;
        float m_LocalY;
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
            float halfDepth = Mathf.Max(0f, (m_SpawnVolume.size.z * 0.5f) - DepthPadding);
            float nextLocalZ = localPosition.z + (MoveSpeed * m_MoveDirection * Time.deltaTime);

            if (nextLocalZ > halfDepth)
            {
                nextLocalZ = halfDepth;
                m_MoveDirection = -1;
            }
            else if (nextLocalZ < -halfDepth)
            {
                nextLocalZ = -halfDepth;
                m_MoveDirection = 1;
            }

            Vector3 clampedLocalPosition = new Vector3(m_LocalX, m_LocalY, nextLocalZ) + m_SpawnVolume.center;
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
            m_LocalY = localPosition.y;
            m_IsInitialized = true;
        }
    }
}
