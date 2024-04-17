using Myriad.ECS.Systems;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    public abstract class BaseSimulationHost<TData>
        : MonoBehaviour
    {
        public abstract ISystemGroup<TData> Systems { get; }
    }
}
