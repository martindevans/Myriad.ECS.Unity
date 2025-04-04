using System.Linq;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Systems
{
    /// <summary>
    /// A group of systems, added and removed to the WorldHost on the same layer when this behaviour is enabled/disabled
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public abstract class WorldSystemGroup<TData>
        : MonoBehaviour
    {
        private BaseSimulationHost<TData> _world;
        public ISystemGroup<TData> Group { get; private set; }

        protected abstract ISystemGroup<TData> CreateGroup(World world);

        protected virtual void OnEnable()
        {
            if (!_world)
            {
                // Try to find world host in this gameobject
                var found = TryGetComponent<BaseSimulationHost<TData>>(out var world);

                // Maybe somewhere in the parents?
                if (!found)
                    world = GetComponentInParent<BaseSimulationHost<TData>>();

                // Just search the whole damn scene
                if (!found)
                {
                    var worlds = FindObjectsOfType<BaseSimulationHost<TData>>(true);
                    world = worlds.Single(a => a.gameObject.layer == gameObject.layer);
                }

                _world = world;
            }

            if (Group == null)
            {
                Group = CreateGroup(_world.World);
                Group.Init();
            }

            _world.Add(Group);
        }

        protected virtual void OnDisable()
        {
            if (Group != null && !ReferenceEquals(_world, null))
                _world.Remove(Group);
        }

        protected virtual void OnDestroy()
        {
            if (Group != null && !ReferenceEquals(_world, null))
            {
                _world.Remove(Group);
                Group.Dispose();
            }

            Group = null;
            _world = null;
        }
    }
}
