using UnityEngine;

namespace Unity.FPS.Game
{
    public interface IAimHighSpawnVolumeAware
    {
        void SetSpawnVolume(BoxCollider spawnVolume);
    }
}
