#nullable enable

using System;
using JetBrains.Annotations;
using Myriad.ECS;
using Myriad.ECS.Components;
using Myriad.ECS.IDs;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Systems;
using UnityEngine;
using UnityEngine.Serialization;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Components
{
    /// <summary>
    /// Indicates that this GameObject is bound to a Myriad Entity. Attach this behaviour as a component
    /// to an entity and ensure the `MyriadEntityBindingSystem` is running in the system schedule.
    /// </summary>
    public sealed class MyriadEntity
        : MonoBehaviour, IPhantomComponent, IEntityRelationComponent
    {
        private Entity? _binding;
        public Entity? Entity => _binding;

        /// <summary>
        /// Indicates how the lifetime of gameobject and entity are bound together
        /// </summary>
        public DestructMode DestructMode => _destructMode;

        [FormerlySerializedAs("DestructMode")]
        [SerializeField, UsedImplicitly]
        internal DestructMode _destructMode;

        /// <summary>
        /// Enable all of these gameobjects when the Entity is set
        /// </summary>
        [SerializeField, UsedImplicitly]
        public GameObject[]? EnableOnEntitySet;

        /// <summary>
        /// Indicates if this object is destroyed
        /// </summary>
        internal bool IsDestroyed { get; private set; }

        /// <summary>
        /// Set entity relation, when entity is constructed
        /// </summary>
        Entity IEntityRelationComponent.Target
        {
            get => _binding!.Value;
            set
            {
                if (_binding.HasValue)
                    throw new InvalidOperationException("Cannot set binding - MyriadEntity is already bound");
                _binding = value;
            }
        }

        private void Awake()
        {
            // Disable everything that wants to be enabled when bound (if not already bound)
            if (EnableOnEntitySet != null && !_binding.HasValue)
                foreach (var item in EnableOnEntitySet)
                    item.SetActive(false);
        }

        private void OnDestroy()
        {
            IsDestroyed = true;

            // No need to notify if this event will not destroy the entity
            if ((_destructMode & DestructMode.GameObjectDestroysEntity) == 0)
                return;

            // Cannot notify if the entity is not yet bound
            if (!_binding.HasValue)
                return;

            // Notification is done through the binding component, if it's not yet attached we can't notify
            var entity = _binding.Value;
            if (entity.HasComponent<MyriadEntityBinding>())
                entity.GetComponentRef<MyriadEntityBinding>().NotifyGameObjectDestroyed(entity);
        }

        #region get components
        /// <summary>
        /// Check if the Myriad Entity bound to this gameObject has a specific component.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasMyriadComponent<T>()
            where T : IComponent
        {
            return Entity?.HasComponent<T>() ?? false;
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

        /// <summary>
        /// Get a boxed component attached to the entity bound to this gameObject
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public object? GetMyriadComponent(ComponentID component)
        {
            return Entity!.Value.GetBoxedComponent(component);
        }
        #endregion
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

