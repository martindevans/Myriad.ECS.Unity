using Myriad.ECS;
using Myriad.ECS.Command;
using Myriad.ECS.Components;
using Myriad.ECS.Queries;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    public class MyriadEntityBindingSystem<TData>
        : BaseSystem<TData>
    {
        private readonly World _world;
        private readonly CommandBuffer _cmd;

        private readonly QueryDescription _initQuery;
        private readonly QueryDescription _destroyQuery;

        public MyriadEntityBindingSystem(World world)
        {
            _world = world;
            _cmd = new CommandBuffer(world);

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

        public override void Update(TData data)
        {
            _world.Execute<InitBinding, MyriadEntity>(new InitBinding(_world, _cmd), _initQuery);
            _world.Execute<DestroyBinding, MyriadEntity>(new DestroyBinding(_cmd), _destroyQuery);
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
                binding.Entity = e;
                binding.World = _world;
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