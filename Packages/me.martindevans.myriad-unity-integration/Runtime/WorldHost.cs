using Myriad.ECS.Systems;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    public abstract class WorldHost<TData>
        : BaseSimulationHost<TData>
    {
        public override ISystemGroup<TData> Systems => _root;

        private readonly DynamicSystemGroup<TData> _root = new("Root");

        protected abstract TData GetData();

        private void Update()
        {
            var data = GetData();
            _root.BeforeUpdate(data);
            _root.Update(data);
        }

        private void LateUpdate()
        {
            var data = GetData();
            _root.AfterUpdate(data);
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
