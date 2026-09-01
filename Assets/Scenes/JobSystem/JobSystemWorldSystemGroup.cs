using Myriad.ECS.Queries;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Queries;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using static Myriad.ECS.Worlds.WorldJobJoinExtensions;
using EntityId = Myriad.ECS.EntityId;

namespace Assets.Scenes.JobSystem
{
    public class JobSystemWorldSystemGroup
        : WorldSystemGroup<GameTime>
    {
        protected override ISystemGroup<GameTime> CreateGroup(BaseSimulationHost<GameTime> world)
        {
            var gate = new QueryJobHandleCompletionGateUpdate<GameTime>();

            return new SystemGroup<GameTime>(
                "test",
                new DoStuffBasic(world.World),
                new DoStuffBasic(world.World),
                new DoStuffBasic(world.World),
                new DoStuffBasic(world.World),
                new DoStuffInJob(world.World, gate),
                new DoStuffBasic(world.World),
                new DoStuffBasic(world.World),
                new DoJoinInJob(world.World, gate),
                new DoStuffBasic(world.World),
                new DoStuffBasic(world.World),
                gate
            );
        }
    }

    public class DoStuffBasic
        : ISystem<GameTime>, ISystemQueryEntityCount, ISystemQueryChunkCount, ISystemQueryArchetypeCount
    {
        private readonly World _world;
        private QueryDescription _queryCache;

        public int QueryEntityCount { get; private set; }
        public int QueryChunkCount { get; private set; }
        public int QueryArchetypeCount { get; private set; }

        public DoStuffBasic(World world)
        {
            _world = world;
        }

        public void Update(GameTime data)
        {
            QueryEntityCount = _world.Query((ref DemoComponent c) =>
            {
                c.Value++;
            }, ref _queryCache);

            QueryChunkCount = _queryCache?.CountChunks() ?? 0;
            QueryArchetypeCount = _queryCache?.CountArchetypes() ?? 0;
        }
    }

    public class DoStuffInJob
        : ISystem<GameTime>, ISystemQueryEntityCount
    {
        private readonly World _world;
        private readonly IQueryJobHandleCompletionGate _gate;

        private QueryDescription _query;

        public int QueryEntityCount { get; private set; }

        public DoStuffInJob(World world, IQueryJobHandleCompletionGate gate)
        {
            _world = world;
            _gate = gate;
            _query = new QueryBuilder().Include<DemoComponent>().Build(world);
        }

        public void Update(GameTime data)
        {
            QueryEntityCount = _query.Count();

            var handle = _world.Schedule<JobScheduler, DemoComponent>(new JobScheduler(), ref _query);
            _gate.AddHandle(handle);
        }

        private struct JobScheduler
            : WorldJobExtensions.IJobQueryScheduler<DemoComponent>
        {
            public JobHandle Schedule(JobChunkHandle chunk, NativeArray<DemoComponent> t0, JobHandle dependsOn)
            {
                var ent = chunk.GetEntityArray();
                var arr = chunk.GetComponentArray<GenericDemoComponent<ulong>>();

                dependsOn = new JobWork(t0, arr, ent).Schedule(t0.Length, 32, dependsOn);

                dependsOn = ent.Dispose(dependsOn);
                dependsOn = arr.Dispose(dependsOn);

                return dependsOn;
            }
        }

        [BurstCompile]
        private readonly struct JobWork
            : IJobParallelFor
        {
            private readonly NativeArray<DemoComponent> _demos;
            private readonly NativeArray<GenericDemoComponent<ulong>> _arr;
            private readonly NativeArray<EntityId> _ent;

            public JobWork(NativeArray<DemoComponent> demos, NativeArray<GenericDemoComponent<ulong>> arr, NativeArray<EntityId> ent)
            {
                _demos = demos;
                _arr = arr;
                _ent = ent;
            }

            public void Execute(int index)
            {
                _demos.AsSpan()[index].Value++;
                _demos.AsSpan()[index].Value += _ent[index].ID;
            }
        }
    }

    public class DoJoinInJob
        : ISystem<GameTime>, ISystemQueryEntityCount, ISystemQueryScheduledJobCount
    {
        private readonly World _world;
        private readonly IQueryJobHandleCompletionGate _gate;

        private readonly QueryDescription _left;
        private readonly QueryDescription _right;

        public int QueryEntityCount { get; private set; }
        public int QueryJobCount { get; private set; }

        public DoJoinInJob(World world, IQueryJobHandleCompletionGate gate)
        {
            _world = world;
            _gate = gate;

            _left = new QueryBuilder().Include<DemoComponent, GenericDemoComponent<long>, GenericDemoComponent<double>>().Build(world);
            _right = new QueryBuilder().Include< GenericDemoComponent<float>, GenericDemoComponent<decimal>, GenericDemoComponent<int>>().Build(world);
        }

        public void Update(GameTime data)
        {
            var scheduler = new JobScheduler();
            var handle = _world.ScheduleJoin(
                ref scheduler,
                _left,
                _right
            );
            _gate.AddHandle(handle);

            QueryEntityCount = _left.Count() * _right.Count();
            QueryJobCount = scheduler.ScheduledJobCount;
        }

        private struct JobScheduler
            : IJoinJobQueryScheduler
        {
            public int ScheduledJobCount { get; private set; }

            public JobHandle Schedule(JobChunkHandle left, JobChunkHandle right, JobHandle dependsOn)
            {
                ScheduledJobCount++;

                var leftSrc = left.GetComponentArray<GenericDemoComponent<long>>();
                var rightDst = right.GetComponentArray<GenericDemoComponent<decimal>>();

                dependsOn = new JobJoinWork(
                    leftSrc,
                    rightDst
                ).Schedule(rightDst.Length, 32, dependsOn);

                dependsOn = leftSrc.Dispose(dependsOn);
                dependsOn = rightDst.Dispose(dependsOn);

                return dependsOn;
            }

            public JobHandle Schedule(JobChunkHandle leftRight, JobHandle dependsOn)
            {
                ScheduledJobCount++;

                var src = leftRight.GetComponentArray<GenericDemoComponent<long>>();
                var dst = leftRight.GetComponentArray<GenericDemoComponent<decimal>>();

                dependsOn = new JobSelfJoinWork(
                    src,
                    dst
                ).Schedule(leftRight.EntityCount, 32, dependsOn);

                dependsOn = src.Dispose(dependsOn);
                dependsOn = dst.Dispose(dependsOn);

                return dependsOn;
            }
        }

        [BurstCompile]
        private struct JobJoinWork
            : IJobParallelFor
        {
            [ReadOnly] private readonly NativeArray<GenericDemoComponent<long>> _src;
            private NativeArray<GenericDemoComponent<decimal>> _dst;

            public JobJoinWork(NativeArray<GenericDemoComponent<long>> src, NativeArray<GenericDemoComponent<decimal>> dst)
            {
                _src = src;
                _dst = dst;
            }

            public void Execute(int index)
            {
                var sum = 0L;
                for (var i = 0; i < _src.Length; i++)
                    sum += _src[i].Value;
                _dst[index] = new() { Value = sum };
            }
        }

        [BurstCompile]
        private struct JobSelfJoinWork
            : IJobParallelFor
        {
            [ReadOnly] private readonly NativeArray<GenericDemoComponent<long>> _src;
            private NativeArray<GenericDemoComponent<decimal>> _dst;

            public JobSelfJoinWork(NativeArray<GenericDemoComponent<long>> src, NativeArray<GenericDemoComponent<decimal>> dst)
            {
                _src = src;
                _dst = dst;
            }

            public void Execute(int index)
            {
                var sum = 0L;
                for (var i = 0; i < _src.Length; i++)
                    sum += _src[i].Value;
                _dst[index] = new() { Value = sum };
            }
        }
    }
}