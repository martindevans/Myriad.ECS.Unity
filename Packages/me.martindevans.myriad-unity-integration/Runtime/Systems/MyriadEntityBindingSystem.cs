#nullable enable

using System;
using System.Collections.Generic;
using Myriad.ECS;
using Myriad.ECS.Command;
using Myriad.ECS.Components;
using Myriad.ECS.Queries;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Components;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Systems
{
    /// <summary>
    /// Finds entities with a `MyriadEntity` component and initialises them with the entity ID and world reference.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public sealed class MyriadEntityBindingSystem<TData>
        : ISystemBefore<TData>, IMyriadEntityDestructCallback
    {
        private readonly World _world;

        private readonly CommandBuffer _cmd;

        private readonly List<IBehaviourComponent> _tempList = new();

        private readonly QueryDescription _createBindingAlive;
        private readonly QueryDescription _createBindingPhantom;
        private readonly QueryDescription _destroyBindingPhantom;

        /// <summary>
        /// Create a new MyriadEntityBindingSystem
        /// </summary>
        /// <param name="world">World to operate on</param>
        public MyriadEntityBindingSystem(World world)
        {
            _world = world;

            _cmd = new CommandBuffer(world);

            _createBindingAlive = new QueryBuilder()
                .Include<MyriadEntity>()
                .Exclude<MyriadEntityBinding>()
                .Build(world);

            _createBindingPhantom = new QueryBuilder()
                .Include<MyriadEntity>()
                .Include<Phantom>()
                .Exclude<MyriadEntityBinding>()
                .Build(world);

            _destroyBindingPhantom = new QueryBuilder()
                .Include<MyriadEntity>()
                .Include<Phantom>()
                .Include<MyriadEntityBinding>()
                .Build(world);
        }

        void IMyriadEntityDestructCallback.NotifyGameObjectDestroyed(Entity entity)
        {
            // GO was destroyed, destroy the entity
            if (entity.Exists() && !entity.IsPhantom())
            {
                _cmd.Remove<MyriadEntity>(entity);
                _cmd.Delete(entity);
            }
        }

        public void BeforeUpdate(TData data)
        {
            // 100 - enity alive
            // 010 - bound (i.e. has MyriadEntityBinding)
            // 001 - GO alive

            // Playback any queued destruction events (from `NotifyGameObjectDestroyed`)
            _cmd.Playback().Dispose();

            // Create bindings for living and dead entities (Add MyriadEntityBinding)
            // 001 - Entity dead,  unbound, GO alive 
            // 100 - Entity alive, unbound, GO dead
            // 101 - Entity alive, unbound, GO alive
            _world.Execute<CreateBinding, MyriadEntity>(new CreateBinding(_cmd, _tempList, this), _createBindingAlive);
            _world.Execute<CreateBinding, MyriadEntity>(new CreateBinding(_cmd, _tempList, this), _createBindingPhantom);
            _cmd.Playback().Dispose();

            // Destroy bindings (remove MyriadEntity, destroy GO if appropriate)
            // 010 - Entity dead,  bound, GO dead  
            // 011 - Entity dead,  bound, GO alive
            _world.Execute<DestroyBinding, MyriadEntity>(new DestroyBinding(_cmd), _destroyBindingPhantom);
            _cmd.Playback().Dispose();

            // 110 - Entity alive, bound, GO dead  (remove MyriadEntity, destroy entity if destruct mode is appropriate)
            // The `NotifyGameObjectDestroyed` callback will destroy the entity, converting this into 010
        }

        public void Update(TData data)
        {
        }

        private readonly struct CreateBinding
            : IQuery<MyriadEntity>
        {
            private readonly CommandBuffer _cmd;
            private readonly List<IBehaviourComponent> _temp;
            private readonly IMyriadEntityDestructCallback _callback;

            public CreateBinding(CommandBuffer cmd, List<IBehaviourComponent> temp, IMyriadEntityDestructCallback callback)
            {
                _cmd = cmd;
                _temp = temp;
                _callback = callback;
            }

            public void Execute(Entity e, ref MyriadEntity binding)
            {
                // Add the binding
                _cmd.Set(e, new MyriadEntityBinding(_callback));

                // Check if the gameobject is already destroyed. If so run the callback (which was missed)
                var go = binding.gameObject;
                if (!binding.IsDestroyed && go)
                {
                    // Bind entity to all child behaviours that care
                    if (e.Exists())
                    {
                        _temp.Clear();
                        go.GetComponentsInChildren(true, _temp);
                        foreach (var item in _temp)
                            item.Bind(e, _cmd);
                        _temp.Clear();

                        // Enable everything that asked to be activated when bound
                        foreach (var item in binding.EnableOnEntitySet ?? Array.Empty<GameObject>())
                            if (item)
                                item.SetActive(true);
                    }
                }
                else
                {
                    // Run the NotifyGameObjectDestroyed callback, this was missed because the GO
                    // was destroyed before this binding even ran.
                    _callback.NotifyGameObjectDestroyed(e);
                }
            }
        }

        private readonly struct DestroyBinding
            : IQuery<MyriadEntity>
        {
            private readonly CommandBuffer _cmd;

            public DestroyBinding(CommandBuffer cmd)
            {
                _cmd = cmd;
            }

            public void Execute(Entity e, ref MyriadEntity binding)
            {
                _cmd.Remove<MyriadEntity>(e);

                if ((binding.DestructMode & DestructMode.EntityDestroysGameObject) != 0)
                    Object.Destroy(binding.gameObject);
            }
        }
    }

    internal readonly struct MyriadEntityBinding
        : IComponent
    {
        private readonly IMyriadEntityDestructCallback _callback;

        public MyriadEntityBinding(IMyriadEntityDestructCallback callback)
        {
            _callback = callback;
        }

        public void NotifyGameObjectDestroyed(Entity entity)
        {
            _callback.NotifyGameObjectDestroyed(entity);
        }
    }

    internal interface IMyriadEntityDestructCallback
    {
        void NotifyGameObjectDestroyed(Entity entity);
    }
}