using Myriad.ECS.IDs;
using Myriad.ECS.Queries;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Queries;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedParameter.Global
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable CheckNamespace
// ReSharper disable ArrangeAccessorOwnerBody

namespace Myriad.ECS.Worlds
{
    public static class WorldJobJoinExtensions
    {
        /// <summary>
        /// Given two chunk handles, schedule join work - all entities in one chunk against all entities in other.
        /// </summary>
        public interface IJoinJobQueryScheduler
        {
            /// <summary>
            /// Schedule a job to process a join between the given chunks.
            /// </summary>
            JobHandle Schedule(
                JobChunkHandle left,
                JobChunkHandle right,
                JobHandle dependsOn
            );

            /// <summary>
            /// Schedule a job to process a join of a chunk with itself.
            /// </summary>
            /// <param name="leftRight"></param>
            /// <param name="dependsOn"></param>
            /// <returns></returns>
            JobHandle Schedule(
                JobChunkHandle leftRight,
                JobHandle dependsOn
            );
        }

        /// <summary>
        /// Schedule a join query, which will schedule a job for every pair of chunks in 2 queries
        /// </summary>
        /// <typeparam name="TScheduler"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="dependsOn"></param>
        /// <param name="allowSelfJoin"></param>
        /// <returns></returns>
        public static QueryJobHandle ScheduleJoin<TScheduler>(this World world, TScheduler sched, QueryDescription left, QueryDescription right, JobHandle dependsOn = default, bool allowSelfJoin = true)
            where TScheduler : IJoinJobQueryScheduler
        {
            if (!left.Any() || !right.Any())
                return default;

            var leftChunkCount = left.CountChunks();
            var rightChunkCount = right.CountChunks();
            var totalChunkCount = leftChunkCount + rightChunkCount;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            using var safetyHandles = new NativeHashMap<(long chunk, ComponentID type), AtomicSafetyHandle>(totalChunkCount, Allocator.Temp);
#endif

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(totalChunkCount, Allocator.TempJob);

            // Store per-chunk dependecies
            using var chunkDeps = new NativeHashMap<long, JobHandle>(totalChunkCount, Allocator.Temp);

            // Initialise the root dependency with all the archetype deps
            foreach (var archetype in left.GetArchetypes())
                dependsOn = JobHandle.CombineDependencies(dependsOn, safety.GetAttachedJob(archetype.Archetype));
            foreach (var archetype in right.GetArchetypes())
                dependsOn = JobHandle.CombineDependencies(dependsOn, safety.GetAttachedJob(archetype.Archetype));

            // Do the query which will schedule the work
            world.ExecuteChunkJoin(
                new JobJoinQuery<TScheduler>(
                    allowSelfJoin,
                    sched,
                    dependsOn,
                    pins,
                    chunkDeps
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    , safetyHandles
#endif
                ),
                left,
                right
            );

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Combine together all chunk handles
            var handle = dependsOn;
            foreach (var item in chunkDeps)
                handle = JobHandle.CombineDependencies(handle, item.Value);

            // Register with the safety system
            foreach (var archetype in left.GetArchetypes())
                safety.AttachJob(archetype.Archetype, handle);
            foreach (var archetype in right.GetArchetypes())
                safety.AttachJob(archetype.Archetype, handle);

            // Return the final handle
            return new QueryJobHandle(
                handle,
                pins
            );
        }

        private struct JobJoinQuery<TScheduler>
            : IChunkJoinQuery
            where TScheduler : IJoinJobQueryScheduler
        {
            private readonly bool _allowSelfJoin;
            private readonly TScheduler _scheduler;
            private readonly JobHandle _dependsOn;

            private NativeHashMap<long, JobHandle> _chunkDependencies;

#pragma warning disable IDE0044 // Field can be made readonly
            private NativeList<GCHandle> _pins;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            private NativeHashMap<(long chunk, ComponentID type), AtomicSafetyHandle> _safetyHandles;
#endif
#pragma warning restore IDE0044

            public JobJoinQuery(
                bool allowSelfJoin,
                TScheduler scheduler,
                JobHandle dependsOn,
                NativeList<GCHandle> pins,
                NativeHashMap<long, JobHandle> chunkDependencies
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                , NativeHashMap<(long chunk, ComponentID type), AtomicSafetyHandle> safetyHandles
#endif
            )
            {
                _allowSelfJoin = allowSelfJoin;
                _scheduler = scheduler;
                _dependsOn = dependsOn;

                _pins = pins;
                _chunkDependencies = chunkDependencies;
                _safetyHandles = safetyHandles;
            }

            public void Execute(ChunkHandle left, ChunkHandle right)
            {
                // Early out if there's no work to do
                if (left.EntityCount == 0 || right.EntityCount == 0)
                    return;

                var handleLeft = new JobChunkHandle(left, _pins,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    _safetyHandles
#endif
                );

                // Get job dependency for accessing this chunk
                var dependsOn = _dependsOn;
                if (_chunkDependencies.TryGetValue(left.ChunkId, out var ldep))
                    dependsOn = JobHandle.CombineDependencies(dependsOn, ldep);

                // Check which type of join we should do. Self join or cross join.
                if (left.ChunkId == right.ChunkId)
                {
                    if (_allowSelfJoin)
                    {
                        dependsOn = _scheduler.Schedule(handleLeft, dependsOn);
                        _chunkDependencies[left.ChunkId] = dependsOn;
                    }
                }
                else
                {
                    var handleRight = new JobChunkHandle(right, _pins,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        _safetyHandles
#endif
                    );

                    // Combine dependency for right chunk
                    if (_chunkDependencies.TryGetValue(right.ChunkId, out var rdep))
                        dependsOn = JobHandle.CombineDependencies(dependsOn, rdep);

                    // Schedule the work
                    dependsOn = _scheduler.Schedule(handleLeft, handleRight, dependsOn);

                    // Update the dependency for both chunks
                    _chunkDependencies[left.ChunkId] = dependsOn;
                    _chunkDependencies[right.ChunkId] = dependsOn;
                }
            }
        }
    }
}
