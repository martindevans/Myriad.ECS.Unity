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
        private bool _hasEntity;
        public World? World { get; private set; }
        public Entity Entity { get; private set; }

        /// <summary>
        /// Destroy this gameobject when the entity is destroyed
        /// </summary>
        [SerializeField, UsedImplicitly] public bool AutoDestruct;

        /// <summary>
        /// Enable all of these gameobjects when the Entity is set
        /// </summary>
        [SerializeField, UsedImplicitly] public GameObject[]? EnableOnEntitySet;

        private void Awake()
        {
            if (EnableOnEntitySet != null && !_hasEntity)
                foreach (var item in EnableOnEntitySet)
                    item.SetActive(false);
        }

        internal void SetEntity(World world, Entity entity)
        {
            _hasEntity = true;
            World = world;
            Entity = entity;

            if (EnableOnEntitySet != null)
                foreach (var item in EnableOnEntitySet)
                    item.SetActive(true);
        }

        internal void EntityDestroyed()
        {
            _hasEntity = false;

            if (AutoDestruct)
                Destroy(gameObject);
        }

        public bool HasMyriadComponent<T>()
            where T : IComponent
        {
            return Entity!.HasComponent<T>(World!);
        }

        public ref T GetMyriadComponent<T>() 
            where T : IComponent
        {
            return ref Entity.GetComponentRef<T>(World!);
        }

        public object? GetMyriadComponent(ComponentID component)
        {
            return Entity.GetBoxedComponent(World!, component);
        }
    }
}
