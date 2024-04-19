using JetBrains.Annotations;
using Myriad.ECS;
using Myriad.ECS.IDs;
using Myriad.ECS.Worlds;
using UnityEngine;

#nullable enable

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    /// <summary>
    /// Indicates that this GameObject is bound to a Myriad Entity. Attach this behaviour as a component
    /// to an entity and ensure the `MyriadEntityBindingSystem` is running in the system schedule.
    /// </summary>
    public class MyriadEntity
        : MonoBehaviour, IComponent
    {
        public World? World { get; internal set; }
        public Entity Entity { get; internal set; }

        /// <summary>
        /// Destroy this gameobject when the entity is destroyed
        /// </summary>
        [SerializeField, UsedImplicitly] public bool AutoDestruct;

        public void EntityDestroyed()
        {
            if (AutoDestruct)
                Destroy(gameObject);
        }

        public bool HasMyriadComponent<T>()
            where T : IComponent
        {
            return Entity!.HasComponent<T>(World);
        }

        public ref T GetMyriadComponent<T>() 
            where T : IComponent
        {
            return ref World!.GetComponentRef<T>(Entity);
        }

        public object? GetMyriadComponent(ComponentID component)
        {
            return World?.GetBoxedComponent(Entity, component);
        }
    }
}
