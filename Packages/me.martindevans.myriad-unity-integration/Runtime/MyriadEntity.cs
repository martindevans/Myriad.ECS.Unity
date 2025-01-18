#nullable enable

using JetBrains.Annotations;
using Myriad.ECS;
using Myriad.ECS.IDs;
using Myriad.ECS.Worlds;
using UnityEngine;
using UnityEngine.Serialization;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    /// <summary>
    /// Indicates that this GameObject is bound to a Myriad Entity. Attach this behaviour as a component
    /// to an entity and ensure the `MyriadEntityBindingSystem` is running in the system schedule.
    /// </summary>
    public sealed class MyriadEntity
        : MonoBehaviour, IComponent
    {
        private bool _hasEntity;
        public World? World { get; private set; }
        public Entity Entity { get; private set; }

        /// <summary>
        /// Destroy this gameobject when the entity is destroyed
        /// </summary>
        [FormerlySerializedAs("AutoDestruct"), SerializeField, UsedImplicitly] public bool AutoDestructGameObject;

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

            if (AutoDestructGameObject)
                Destroy(gameObject);
        }

        /// <summary>
        /// Check if the Myriad Entity bound to this gameObject has a specific component.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasMyriadComponent<T>()
            where T : IComponent
        {
            return Entity.HasComponent<T>();
        }

        /// <summary>
        /// Get a reference to the component attached to the entity bound to this gameObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ref T GetMyriadComponent<T>() 
            where T : IComponent
        {
            return ref Entity.GetComponentRef<T>();
        }

        public object? GetMyriadComponent(ComponentID component)
        {
            return Entity.GetBoxedComponent(component);
        }
    }
}

