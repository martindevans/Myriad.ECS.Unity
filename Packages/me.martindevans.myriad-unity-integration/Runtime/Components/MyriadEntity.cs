#nullable enable

using System;
using JetBrains.Annotations;
using Myriad.ECS;
using Myriad.ECS.Command;
using Myriad.ECS.IDs;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Components
{
    /// <summary>
    /// Indicates that this GameObject is bound to a Myriad Entity. Attach this behaviour as a component
    /// to an entity and ensure the `MyriadEntityBindingSystem` is running in the system schedule.
    /// </summary>
    public sealed class MyriadEntity
        : MonoBehaviour, IComponent
    {
        private (Entity, CommandBuffer)? _binding;
        public Entity? Entity => _binding?.Item1;

        /// <summary>
        /// Set how the lifetime of gameobject and entity are bound together
        /// </summary>
        [SerializeField, UsedImplicitly]
        public DestructMode DestructMode;

        /// <summary>
        /// Enable all of these gameobjects when the Entity is set
        /// </summary>
        [SerializeField, UsedImplicitly]
        public GameObject[]? EnableOnEntitySet;

        private void Awake()
        {
            if (EnableOnEntitySet != null && !_binding.HasValue)
                foreach (var item in EnableOnEntitySet)
                    item.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_binding is var (entity, cmd))
                if ((DestructMode & DestructMode.GameObjectDestroysEntity) != DestructMode.None)
                    cmd.Remove<MyriadEntity>(entity);
        }

        internal void SetEntity(Entity entity, CommandBuffer selfDestructBuffer)
        {
            _binding = (entity, selfDestructBuffer);

            if (EnableOnEntitySet != null)
                foreach (var item in EnableOnEntitySet)
                    item.SetActive(true);
        }

        internal void EntityDestroyed()
        {
            _binding = default;

            if ((DestructMode & DestructMode.EntityDestroysGameObject) != DestructMode.None)
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
            return _binding!.Value.Item1.HasComponent<T>();
        }

        /// <summary>
        /// Get a reference to the component attached to the entity bound to this gameObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ref T GetMyriadComponent<T>() 
            where T : IComponent
        {
            return ref Entity!.Value.GetComponentRef<T>();
        }

        public object? GetMyriadComponent(ComponentID component)
        {
            return Entity!.Value.GetBoxedComponent(component);
        }
    }

    [Flags]
    public enum DestructMode
    {
        /// <summary>
        /// GameObject and entity lifetimes are not linked
        /// </summary>
        None = 0b00,

        /// <summary>
        /// When the entity is destroyed, the gameobject will be destroyed
        /// </summary>
        EntityDestroysGameObject = 0b01,

        /// <summary>
        /// When this GameObject is destroyed, the entity will be destroyed
        /// </summary>
        GameObjectDestroysEntity = 0b10,

        /// <summary>
        /// If the Entity <b>or</b> the GameObject is destroyed, the other will be destroyed
        /// </summary>
        Both = 0b11,
    }
}

