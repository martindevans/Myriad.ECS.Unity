using System;
using Myriad.ECS.Systems;
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

        protected abstract ISystemGroup<TData> CreateGroup(BaseSimulationHost<TData> world);

        public void Init(BaseSimulationHost<TData> world)
        {
            if (_world != null)
                throw new InvalidOperationException("Cannot call WorldSystemGroup.Init() twice");

            _world = world;

            Group = CreateGroup(_world);
            Group.Init();
            _world.Add(Group);

            Group.Enabled = enabled;
        }

        protected virtual void OnEnable()
        {
            if (Group != null)
                Group.Enabled = true;
        }

        protected virtual void OnDisable()
        {
            if (Group != null)
                Group.Enabled = false;
        }
    }
}
