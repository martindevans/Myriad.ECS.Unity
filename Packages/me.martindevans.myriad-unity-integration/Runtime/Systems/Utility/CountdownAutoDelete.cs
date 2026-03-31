using System;
using Myriad.ECS;
using Myriad.ECS.Command;
using Myriad.ECS.Queries;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Systems.Utility
{
    /// <summary>
    /// Delete an entity after a countdown
    /// </summary>
    public struct CountdownAutoDelete
        : IComponent
    {
        public double Countdown;
    }

    public abstract class BaseCountdownAutoDeleteSystem<TData>
        : ISystem<TData>, ISystemQueryEntityCount
    {
        private readonly World _world;
        private readonly CommandBuffer _cmd;

        public int QueryEntityCount { get; private set; }

        protected BaseCountdownAutoDeleteSystem(World world, CommandBuffer cmd)
        {
            _world = world;
            _cmd = cmd;
        }

        protected abstract double GetDeltaTime(TData data);

        public void Update(TData data)
        {
            QueryEntityCount = _world.ExecuteChunk<Countdown, CountdownAutoDelete>(
                new Countdown(GetDeltaTime(data), _cmd)
            );
        }

        private readonly struct Countdown
            : IChunkQuery<CountdownAutoDelete>
        {
            private readonly double _deltaTime;
            private readonly CommandBuffer _cmd;

            public Countdown(double deltaTime, CommandBuffer cmd)
            {
                _deltaTime = deltaTime;
                _cmd = cmd;
            }

            public void Execute(ChunkHandle chunk, Span<CountdownAutoDelete> sds)
            {
                var entities = chunk.Entities.Span;

                for (var i = sds.Length - 1; i >= 0; i--)
                {
                    sds[i].Countdown -= _deltaTime;

                    if (sds[i].Countdown <= 0)
                        _cmd.Delete(entities[i]);
                }
            }
        }
    }

    public class GameTimeCountdownAutoDeleteSystem
        : BaseCountdownAutoDeleteSystem<GameTime>
    {
        public GameTimeCountdownAutoDeleteSystem(World world, CommandBuffer cmd)
            : base(world, cmd)
        {
        }

        protected override double GetDeltaTime(GameTime data)
        {
            return data.DeltaTime;
        }
    }
}