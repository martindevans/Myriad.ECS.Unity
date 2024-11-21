using Myriad.ECS.Systems;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    public abstract class WorldHost<TData>
        : BaseSimulationHost<TData>
    {
        public override ISystemGroup<TData> Systems => _root;

        private readonly DynamicSystemGroup<TData> _root = new("Root");

        protected abstract TData GetData();

        private TData _data;

        private void Update()
        {
            _data = GetData();
            _root.BeforeUpdate(_data);
            _root.Update(_data);
        }

        private void LateUpdate()
        {
            _root.AfterUpdate(_data);
        }

        public void Add(ISystem<TData> system)
        {
            _root.Add(system);
        }

        public bool Remove(ISystem<TData> system)
        {
            return _root.Remove(system);
        }
    }
}
