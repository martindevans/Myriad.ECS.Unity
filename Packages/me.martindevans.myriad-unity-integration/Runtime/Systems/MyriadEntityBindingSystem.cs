#nullable enable

using System.Collections.Generic;
using Myriad.ECS;
using Myriad.ECS.Command;
using Myriad.ECS.Components;
using Myriad.ECS.Queries;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Components;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Systems
{
    /// <summary>
    /// Finds entities with a `MyriadEntity` component and initialises them with the entity ID and world reference.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public sealed class MyriadEntityBindingSystem<TData>
        : ISystemBefore<TData>, ISystemAfter<TData>
    {
        private readonly World _world;

        private readonly CommandBuffer _cmd;
        private readonly CommandBuffer _destructBuffer;

        private readonly QueryDescription _initQuery;
        private readonly QueryDescription _destroyQuery;
        private readonly QueryDescription _destroyQueryWithGo;
        private readonly List<IBehaviourComponent> _tempList = new();
        
        /// <summary>
        /// Create a new MyriadEntityBindingSystem
        /// </summary>
        /// <param name="world">World to operate on</param>
        public MyriadEntityBindingSystem(World world)
        {
            _world = world;

            _cmd = new CommandBuffer(world);
            _destructBuffer = new CommandBuffer(world);

            _initQuery = new QueryBuilder()
                .Include<MyriadEntity>()
                .Exclude<Bound>()
                .Build(world);

            // Find dead entities with a binding, then destroy the GameObject
            _destroyQueryWithGo = new QueryBuilder()
                .Include<MyriadEntity>()
                .Include<Bound>()
                .Include<Phantom>()
                .Build(world);

            // Find **live** entities without a GameObject, but with a binding. This means it
            // had a `MyriadEntity` GameObject in the past but it removed itself.
            _destroyQuery = new QueryBuilder()
                .Include<Bound>()
                .Exclude<MyriadEntity>()
                .Build(world);
        }

        public void BeforeUpdate(TData data)
        {
            _world.Execute<InitBinding, MyriadEntity>(new InitBinding(_cmd, _tempList, _destructBuffer), _initQuery);
            _cmd.Playback().Dispose();
        }

        public void Update(TData data)
        {
        }

        public void AfterUpdate(TData data)
        {
            // Execute destruct buffer. GameObjects that were being destroyed will have queued up removal of MyriadEntity here.
            _destructBuffer.Playback().Dispose();

            _world.Execute<DestroyBindingGo, MyriadEntity>(new DestroyBindingGo(_cmd), _destroyQueryWithGo);
            _world.Execute(new DestroyBinding(_cmd), _destroyQuery);
            _cmd.Playback().Dispose();
        }

        private struct Bound
            : IPhantomComponent
        {
        }

        private readonly struct InitBinding
            : IQuery<MyriadEntity>
        {
            private readonly CommandBuffer _cmd;
            private readonly List<IBehaviourComponent> _temp;
            private readonly CommandBuffer _destructBuffer;

            public InitBinding(CommandBuffer cmd, List<IBehaviourComponent> temp, CommandBuffer destructBuffer)
            {
                _cmd = cmd;
                _temp = temp;
                _destructBuffer = destructBuffer;
            }

            public void Execute(Entity e, ref MyriadEntity binding)
            {
                binding.SetEntity(e, _destructBuffer);
                _cmd.Set(e, new Bound());

                // Find all `IBehaviourComponent`s and bind them to the entity
                _temp.Clear();
                binding.gameObject.GetComponentsInChildren(true, _temp);
                foreach (var item in _temp)
                    item.Bind(e, _cmd);
                _temp.Clear();
            }
        }

        private readonly struct DestroyBindingGo
            : IQuery<MyriadEntity>
        {
            private readonly CommandBuffer _cmd;

            public DestroyBindingGo(CommandBuffer cmd)
            {
                _cmd = cmd;
            }

            public void Execute(Entity e, ref MyriadEntity binding)
            {
                binding.EntityDestroyed();
                _cmd.Remove<Bound>(e);
            }
        }

        private readonly struct DestroyBinding
            : IQuery
        {
            private readonly CommandBuffer _cmd;

            public DestroyBinding(CommandBuffer cmd)
            {
                _cmd = cmd;
            }

            public void Execute(Entity e)
            {
                _cmd.Remove<Bound>(e);
                _cmd.Delete(e);
            }
        }
    }
}