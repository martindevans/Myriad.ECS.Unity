using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    public abstract class BaseSimulationHost<TData>
        : MonoBehaviour
    {
        public abstract ISystemGroup<TData> Systems { get; }

        public abstract World World { get; }
    }
}
