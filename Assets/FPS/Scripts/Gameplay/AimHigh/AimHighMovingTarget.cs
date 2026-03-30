using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.Gameplay.AimHigh
{
    public class AimHighMovingTarget : MonoBehaviour, IAimHighSpawnVolumeAware, IAimHighMovingTarget
    {
        [Header("Movement Settings")]
        [Tooltip("How fast the target moves between waypoints")]
        public float MovementSpeed = 5f;

        [Tooltip("Minimum wait time at each waypoint before picking a new one")]
        public float MinWaitTime = 0.1f;

        [Tooltip("Maximum wait time at each waypoint")]
        public float MaxWaitTime = 0.5f;

        [Tooltip("How close the target needs to be to a waypoint before considering it reached")]
        public float ReachThreshold = 0.1f;

        BoxCollider m_MovementZone;
        Vector3 m_TargetTargetPosition;
        float m_NextMoveTime;
        bool m_IsWaiting;
        Rigidbody m_Rigidbody;

        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody>();
            if (m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = true;
            }
        }

        public void SetSpawnVolume(BoxCollider spawnVolume)
        {
            m_MovementZone = spawnVolume;
            PickInitialWaypoint();
        }

        void PickInitialWaypoint()
        {
            if (m_MovementZone == null) return;
            m_TargetTargetPosition = GetRandomPointInZone();
            m_IsWaiting = false;
        }

        void Update()
        {
            if (m_MovementZone == null)
            {
                return;
            }

            if (m_IsWaiting)
            {
                if (Time.time >= m_NextMoveTime)
                {
                    m_IsWaiting = false;
                    m_TargetTargetPosition = GetRandomPointInZone();
                }
                return;
            }

            // Move towards the target position
            transform.position = Vector3.MoveTowards(transform.position, m_TargetTargetPosition, MovementSpeed * Time.deltaTime);

            // Check if reached
            if (Vector3.Distance(transform.position, m_TargetTargetPosition) <= ReachThreshold)
            {
                m_IsWaiting = true;
                m_NextMoveTime = Time.time + Random.Range(MinWaitTime, MaxWaitTime);
            }
        }

        Vector3 GetRandomPointInZone()
        {
            if (m_MovementZone == null) return transform.position;

            Vector3 size = m_MovementZone.size;
            Vector3 center = m_MovementZone.center;

            Vector3 localOffset = new Vector3(
                Random.Range(-size.x * 0.5f, size.x * 0.5f),
                Random.Range(-size.y * 0.5f, size.y * 0.5f),
                Random.Range(-size.z * 0.5f, size.z * 0.5f)
            );

            return m_MovementZone.transform.TransformPoint(center + localOffset);
        }
    }
}
