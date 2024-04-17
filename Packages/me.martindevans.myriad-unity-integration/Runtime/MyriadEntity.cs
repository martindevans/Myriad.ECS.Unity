using System;
using System.Reflection;
using JetBrains.Annotations;
using Myriad.ECS;
using Myriad.ECS.IDs;
using Myriad.ECS.Worlds;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    /// <summary>
    /// Indicates that this GameObject is bound to a Myriad Entity. Attach this behaviour as a component
    /// to an entity and ensure the `MyriadEntityBindingSystem` is running in the system schedule.
    /// </summary>
    public class MyriadEntity
        : MonoBehaviour, IComponent
    {
        public World World { get; internal set; }
        public Entity Entity { get; internal set; }

        [SerializeField, UsedImplicitly] public bool AutoDestruct;

        public void EntityDestroyed()
        {
            if (AutoDestruct)
                Destroy(gameObject);
        }

        public bool HasMyriadComponent<T>()
            where T : IComponent
        {
            return World.HasComponent<T>(Entity);
        }

        public ref T GetMyriadComponent<T>() 
            where T : IComponent
        {
            return ref World.GetComponentRef<T>(Entity);
        }

        public object GetMyriadComponent(ComponentID component)
        {
            var method = GetType().GetMethod(nameof(GetMyriadComponentHelper), BindingFlags.NonPublic | BindingFlags.Instance)!;
            return method
                .MakeGenericMethod(component.Type)
                .Invoke(this, Array.Empty<object>());
        }

        [UsedImplicitly]
        private object GetMyriadComponentHelper<T>()
            where T : IComponent
        {
            if (!Entity.Exists(World) || !World.HasComponent<T>(Entity))
                return null;

            return World.GetComponentRef<T>(Entity);
        }
    }
}
