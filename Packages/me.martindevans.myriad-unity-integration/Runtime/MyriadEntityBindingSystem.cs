using Myriad.ECS;
using Myriad.ECS.Command;
using Myriad.ECS.Components;
using Myriad.ECS.Queries;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;

#nullable enable

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    /// <summary>
    /// Finds entities with a `MyriadEntity` component and initialises them with the entity ID and world reference.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public class MyriadEntityBindingSystem<TData>
        : BaseSystem<TData>, ISystemBefore<TData>, ISystemAfter<TData>
    {
        private readonly World _world;

        private readonly bool _executeCommand;
        private readonly CommandBuffer _cmd;

        private readonly QueryDescription _initQuery;
        private readonly QueryDescription _destroyQuery;

        /// <summary>
        /// Create a new MyriadEntityBindingSystem
        /// </summary>
        /// <param name="world">World to operate on</param>
        /// <param name="cmd">Optionally, the command buffer to queue changes into</param>
        public MyriadEntityBindingSystem(World world, CommandBuffer? cmd = null)
        {
            _world = world;

            _executeCommand = cmd == null;
            _cmd = cmd ?? new CommandBuffer(world);

            _initQuery = new QueryBuilder()
                .Include<MyriadEntity>()
                .Exclude<Bound>()
                .Build(world);

            _destroyQuery = new QueryBuilder()
                .Include<MyriadEntity>()
                .Include<Bound>()
                .Include<Phantom>()
                .Build(world);
        }

        public void BeforeUpdate(TData data)
        {
            _world.Execute<InitBinding, MyriadEntity>(new InitBinding(_world, _cmd), _initQuery);
            if (_executeCommand)
                _cmd.Playback().Dispose();
        }

        public override void Update(TData data)
        {
        }

        public void AfterUpdate(TData data)
        {
            _world.Execute<DestroyBinding, MyriadEntity>(new DestroyBinding(_cmd), _destroyQuery);
            if (_executeCommand)
                _cmd.Playback().Dispose();
        }

        private struct Bound
            : IPhantomComponent
        {
        }

        private readonly struct InitBinding
            : IQuery1<MyriadEntity>
        {
            private readonly World _world;
            private readonly CommandBuffer _cmd;

            public InitBinding(World world, CommandBuffer cmd)
            {
                _world = world;
                _cmd = cmd;
            }

            public void Execute(Entity e, ref MyriadEntity binding)
            {
                binding.SetEntity(_world, e);
                _cmd.Set(e, new Bound());
            }
        }

        private readonly struct DestroyBinding
            : IQuery1<MyriadEntity>
        {
            private readonly CommandBuffer _cmd;

            public DestroyBinding(CommandBuffer cmd)
            {
                _cmd = cmd;
            }

            public void Execute(Entity e, ref MyriadEntity binding)
            {
                binding.EntityDestroyed();
                _cmd.Remove<Bound>(e);
            }
        }
    }
}