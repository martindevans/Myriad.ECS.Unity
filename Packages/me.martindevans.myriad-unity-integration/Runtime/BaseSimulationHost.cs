using Myriad.ECS.Systems;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    /// <summary>
    /// Extends BaseWorldHost with systems
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public abstract class BaseSimulationHost<TData>
        : BaseWorldHost
    {
        /// <summary>
        /// The root system group
        /// </summary>
        public ISystemGroup<TData> Systems => _root;

        /// <summary>
        /// Indicates if the systems should be disposed when OnDestroy happens
        /// </summary>
        protected virtual bool DisposeSystems => true;

        private readonly DynamicSystemGroup<TData> _root = new("Root");

        private TData _data;

        /// <summary>
        /// Get the current simulation time
        /// </summary>
        public abstract double CurrentTime { get; }

        /// <summary>
        /// Get the current simulation frame count
        /// </summary>
        public abstract ulong CurrentFrame { get; }

        protected override void Update()
        {
            base.Update();

            _data = GetData();
            _root.BeforeUpdate(_data);
            _root.Update(_data);
        }

        protected virtual void LateUpdate()
        {
            _root.AfterUpdate(_data);
        }

        protected override void OnDestroy()
        {
            if (DisposeSystems)
                Systems.Dispose();

            base.OnDestroy();
        }

        /// <summary>
        /// Add a new system to the root system group
        /// </summary>
        /// <param name="system"></param>
        public void Add(ISystem<TData> system)
        {
            _root.Add(system);
        }

        /// <summary>
        /// Remove a system from the root system group
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        public bool Remove(ISystem<TData> system)
        {
            return _root.Remove(system);
        }

        /// <summary>
        /// Get an instance of TData that will be used for the next update phase
        /// </summary>
        /// <returns></returns>
        protected abstract TData GetData();
    }
}
