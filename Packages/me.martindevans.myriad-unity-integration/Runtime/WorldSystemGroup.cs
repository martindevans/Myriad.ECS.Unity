using System.Linq;
using Myriad.ECS.Systems;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    /// <summary>
    /// A group of systems, added and removed to the WorldHost on the same layer when this behaviour is enabled/disabled
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public abstract class WorldSystemGroup<TData>
        : MonoBehaviour
    {
        private WorldHost<TData> _world;
        private ISystemGroup<TData> _group;

        protected abstract ISystemGroup<TData> Group { get; }

        private void OnEnable()
        {
            var worlds = FindObjectsOfType<WorldHost<TData>>(true);
            _world = worlds.Single(a => a.gameObject.layer == gameObject.layer);

            _group = Group;
            _world.Add(_group);
        }

        private void OnDisable()
        {
            if (_group != null && !ReferenceEquals(_world, null))
                _world.Remove(_group);

            _group = null;
            _world = null;
        }

        private void OnDestroy()
        {
            if (_group != null && !ReferenceEquals(_world, null))
                _world.Remove(_group);

            _group = null;
            _world = null;
        }
    }
}
