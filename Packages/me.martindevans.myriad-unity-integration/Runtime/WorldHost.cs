using Myriad.ECS.Command;
using Myriad.ECS.Systems;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    public abstract class WorldHost<TData>
        : BaseSimulationHost<TData>
    {
        private CommandBuffer _cmd;
        /// <summary>
        /// A shared command buffer which will be executed before updates every frame.
        /// </summary>
        public CommandBuffer CommandBuffer
        {
            get
            {
                if (_cmd == null)
                    _cmd = new CommandBuffer(World);
                return _cmd;
            }
        }

        public override ISystemGroup<TData> Systems => _root;

        private readonly DynamicSystemGroup<TData> _root = new("Root");

        protected abstract TData GetData();

        private TData _data;

        protected virtual void Update()
        {
            CommandBuffer.Playback().Dispose();

            _data = GetData();
            _root.BeforeUpdate(_data);
            _root.Update(_data);
        }

        protected virtual void LateUpdate()
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
