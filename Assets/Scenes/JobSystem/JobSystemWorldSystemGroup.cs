using System;
using Myriad.ECS.Queries;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Assets.Scenes.JobSystem
{
    public class JobSystemWorldSystemGroup
        : WorldSystemGroup<GameTime>
    {
        public int Count = 1000;

        protected override ISystemGroup<GameTime> CreateGroup(BaseSimulationHost<GameTime> world)
        {
            var rng = new Random();
            var cmd = world.World.GetCommandBuffer();
            for (var i = 0; i < Count; i++)
            {
                var eb = cmd.Create().Set(new DemoComponent());

                if (rng.NextDouble() < 0.2)
                    eb.Set(new GenericDemoComponent<int>());
                if (rng.NextDouble() < 0.2)
                    eb.Set(new GenericDemoComponent<float>());
                if (rng.NextDouble() < 0.2)
                    eb.Set(new GenericDemoComponent<long>());
                if (rng.NextDouble() < 0.2)
                    eb.Set(new GenericDemoComponent<decimal>());
                if (rng.NextDouble() < 0.2)
                    eb.Set(new GenericDemoComponent<double>());
            }

            cmd.Playback().Dispose();

            return new SystemGroup<GameTime>(
                "test",
                new DoStuffBasic(world.World),
                new DoStuffInJob(world.World)
            );
        }
    }

    public class DoStuffBasic
        : ISystem<GameTime>
    {
        private readonly World _world;

        public DoStuffBasic(World world)
        {
            _world = world;
        }

        public void Update(GameTime data)
        {
            _world.Query((ref DemoComponent c) =>
            {
                c.Value++;
            });
        }
    }

    public class DoStuffInJob
        : ISystem<GameTime>
    {
        private readonly World _world;

        private QueryDescription _query;

        public DoStuffInJob(World world)
        {
            _world = world;
            _query = new QueryBuilder().Include<DemoComponent>().Build(world);
        }

        public void Update(GameTime data)
        {
            using var handle = _world.Schedule<JobScheduler, DemoComponent>(new JobScheduler(), ref _query);
            handle.Complete();

            // It's safe to wait on the handle multiple times
            handle.Handle.Complete();
            handle.Complete();
            // ReSharper disable once DisposeOnUsingVariable
            handle.Dispose();
            handle.Handle.Complete();
            handle.Complete();
            // ReSharper disable once DisposeOnUsingVariable
            handle.Dispose();
        }

        private struct JobScheduler
            : WorldJobExtensions.IJobQueryScheduler<DemoComponent>
        {
            public JobHandle Schedule(WorldJobExtensions.JobChunkHandle chunk, NativeArray<DemoComponent> t0, JobHandle dependsOn)
            {
                return new JobWork(t0).Schedule(t0.Length, 32, dependsOn);
            }
        }

        [BurstCompile]
        private readonly struct JobWork
            : IJobParallelFor
        {
            private readonly NativeArray<DemoComponent> _demos;

            public JobWork(NativeArray<DemoComponent> demos)
            {
                _demos = demos;
            }

            public void Execute(int index)
            {
                _demos.AsSpan()[index].Value++;
            }
        }
    }
}