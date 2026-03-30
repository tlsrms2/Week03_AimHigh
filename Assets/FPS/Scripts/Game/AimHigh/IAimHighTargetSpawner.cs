using UnityEngine;

namespace Unity.FPS.Game
{
    public interface IAimHighTargetSpawner
    {
        void Begin();
        void Stop();
        void ClearTargets();
        bool GetRandomSpawnPosition(out Vector3 spawnPosition, out Quaternion spawnRotation);
        BoxCollider GetRandomAvailableSpawnVolume();
    }
}
