using System;
using Myriad.ECS.Queries;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
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
    public static class WorldJobExtensions
    {
        /// <summary>
        /// A handle for a Unity job based Myriad query. <b>MUST</b> be waited on at least once for correctness!
        /// </summary>
        public struct QueryJobHandle
            : IDisposable
        {
            private JobHandle _jobHandle;
            private NativeList<GCHandle> _pins;

            public bool IsCompleted => _jobHandle.IsCompleted;

            public JobHandle Handle => _jobHandle;

            internal QueryJobHandle(JobHandle handle, NativeList<GCHandle> pins)
            {
                _jobHandle = handle;
                _pins = pins;
            }

            public void Complete()
            {
                _jobHandle.Complete();

                if (_pins.IsCreated)
                {
                    for (var i = 0; i < _pins.Length; i++)
                        _pins[i].Free();
                    _pins.Dispose();
                }
            }

            public void Dispose()
            {
                Complete();
            }
        }

        public ref struct JobChunkHandle
        {
            private readonly ChunkHandle _handle;
            private NativeList<GCHandle> _pins;

            public int EntityCount => _handle.EntityCount;

            public JobChunkHandle(ChunkHandle handle, NativeList<GCHandle> pins)
            {
                _handle = handle;
                _pins = pins;
            }

            /// <summary>Test if this chunk contains a specific component</summary>
            /// <typeparam name="T">Component type</typeparam>
            /// <returns></returns>
            public bool HasComponent<T>()
                where T : IComponent
            {
                return _handle.HasComponent<T>();
            }

            /// <summary>
            /// Get a native array with a view of component data that can be passed into a job.
            /// Array will automatically be disposed after job is complete
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public NativeArray<T> GetComponentArray<T>()
                where T : struct, IComponent
            {
                // Pin array for component
                var array = _handle.Danger().GetComponentArray<T>();
                var pin = GCHandle.Alloc(array, GCHandleType.Pinned);
                _pins.Add(pin);

                unsafe
                {
                    // Wrap as native array
                    var nArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                        (void*)pin.AddrOfPinnedObject(), _handle.EntityCount, Allocator.None
                    );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle(
                        ref nArray,
                        AtomicSafetyHandle.Create()
                    );
#endif

                    return nArray;
                }
            }
        }

        public interface IJobQueryScheduler<T0>
            where T0 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0>
            : IChunkQuery<T0>
            where T0 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0>
            where T0 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 1, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0>,
                T0
            >(new JobQuery<TScheduler, T0>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1>
            : IChunkQuery<T0, T1>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 2, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1>,
                T0, T1
            >(new JobQuery<TScheduler, T0, T1>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2>
            : IChunkQuery<T0, T1, T2>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 3, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2>,
                T0, T1, T2
            >(new JobQuery<TScheduler, T0, T1, T2>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2, T3>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                NativeArray<T3> t3,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2, T3>
            : IChunkQuery<T0, T1, T2, T3>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2,
                Span<T3> t3
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();
                var nArray3 = jobChunkHandle.GetComponentArray<T3>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    nArray3,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);
                jHandle = nArray3.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2, T3>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2, T3>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 4, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2, T3>,
                T0, T1, T2, T3
            >(new JobQuery<TScheduler, T0, T1, T2, T3>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2, T3, T4>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                NativeArray<T3> t3,
                NativeArray<T4> t4,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2, T3, T4>
            : IChunkQuery<T0, T1, T2, T3, T4>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2,
                Span<T3> t3,
                Span<T4> t4
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();
                var nArray3 = jobChunkHandle.GetComponentArray<T3>();
                var nArray4 = jobChunkHandle.GetComponentArray<T4>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    nArray3,
                    nArray4,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);
                jHandle = nArray3.Dispose(jHandle);
                jHandle = nArray4.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2, T3, T4>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2, T3, T4>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 5, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2, T3, T4>,
                T0, T1, T2, T3, T4
            >(new JobQuery<TScheduler, T0, T1, T2, T3, T4>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2, T3, T4, T5>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                NativeArray<T3> t3,
                NativeArray<T4> t4,
                NativeArray<T5> t5,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2, T3, T4, T5>
            : IChunkQuery<T0, T1, T2, T3, T4, T5>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2,
                Span<T3> t3,
                Span<T4> t4,
                Span<T5> t5
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();
                var nArray3 = jobChunkHandle.GetComponentArray<T3>();
                var nArray4 = jobChunkHandle.GetComponentArray<T4>();
                var nArray5 = jobChunkHandle.GetComponentArray<T5>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    nArray3,
                    nArray4,
                    nArray5,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);
                jHandle = nArray3.Dispose(jHandle);
                jHandle = nArray4.Dispose(jHandle);
                jHandle = nArray5.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2, T3, T4, T5>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2, T3, T4, T5>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 6, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2, T3, T4, T5>,
                T0, T1, T2, T3, T4, T5
            >(new JobQuery<TScheduler, T0, T1, T2, T3, T4, T5>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                NativeArray<T3> t3,
                NativeArray<T4> t4,
                NativeArray<T5> t5,
                NativeArray<T6> t6,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6>
            : IChunkQuery<T0, T1, T2, T3, T4, T5, T6>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2,
                Span<T3> t3,
                Span<T4> t4,
                Span<T5> t5,
                Span<T6> t6
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();
                var nArray3 = jobChunkHandle.GetComponentArray<T3>();
                var nArray4 = jobChunkHandle.GetComponentArray<T4>();
                var nArray5 = jobChunkHandle.GetComponentArray<T5>();
                var nArray6 = jobChunkHandle.GetComponentArray<T6>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    nArray3,
                    nArray4,
                    nArray5,
                    nArray6,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);
                jHandle = nArray3.Dispose(jHandle);
                jHandle = nArray4.Dispose(jHandle);
                jHandle = nArray5.Dispose(jHandle);
                jHandle = nArray6.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2, T3, T4, T5, T6>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2, T3, T4, T5, T6>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 7, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6>,
                T0, T1, T2, T3, T4, T5, T6
            >(new JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                NativeArray<T3> t3,
                NativeArray<T4> t4,
                NativeArray<T5> t5,
                NativeArray<T6> t6,
                NativeArray<T7> t7,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7>
            : IChunkQuery<T0, T1, T2, T3, T4, T5, T6, T7>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2,
                Span<T3> t3,
                Span<T4> t4,
                Span<T5> t5,
                Span<T6> t6,
                Span<T7> t7
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();
                var nArray3 = jobChunkHandle.GetComponentArray<T3>();
                var nArray4 = jobChunkHandle.GetComponentArray<T4>();
                var nArray5 = jobChunkHandle.GetComponentArray<T5>();
                var nArray6 = jobChunkHandle.GetComponentArray<T6>();
                var nArray7 = jobChunkHandle.GetComponentArray<T7>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    nArray3,
                    nArray4,
                    nArray5,
                    nArray6,
                    nArray7,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);
                jHandle = nArray3.Dispose(jHandle);
                jHandle = nArray4.Dispose(jHandle);
                jHandle = nArray5.Dispose(jHandle);
                jHandle = nArray6.Dispose(jHandle);
                jHandle = nArray7.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        /// <typeparam name="T7"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2, T3, T4, T5, T6, T7>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 8, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7>,
                T0, T1, T2, T3, T4, T5, T6, T7
            >(new JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                NativeArray<T3> t3,
                NativeArray<T4> t4,
                NativeArray<T5> t5,
                NativeArray<T6> t6,
                NativeArray<T7> t7,
                NativeArray<T8> t8,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8>
            : IChunkQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2,
                Span<T3> t3,
                Span<T4> t4,
                Span<T5> t5,
                Span<T6> t6,
                Span<T7> t7,
                Span<T8> t8
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();
                var nArray3 = jobChunkHandle.GetComponentArray<T3>();
                var nArray4 = jobChunkHandle.GetComponentArray<T4>();
                var nArray5 = jobChunkHandle.GetComponentArray<T5>();
                var nArray6 = jobChunkHandle.GetComponentArray<T6>();
                var nArray7 = jobChunkHandle.GetComponentArray<T7>();
                var nArray8 = jobChunkHandle.GetComponentArray<T8>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    nArray3,
                    nArray4,
                    nArray5,
                    nArray6,
                    nArray7,
                    nArray8,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);
                jHandle = nArray3.Dispose(jHandle);
                jHandle = nArray4.Dispose(jHandle);
                jHandle = nArray5.Dispose(jHandle);
                jHandle = nArray6.Dispose(jHandle);
                jHandle = nArray7.Dispose(jHandle);
                jHandle = nArray8.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        /// <typeparam name="T7"></typeparam>
        /// <typeparam name="T8"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 9, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8>,
                T0, T1, T2, T3, T4, T5, T6, T7, T8
            >(new JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                NativeArray<T3> t3,
                NativeArray<T4> t4,
                NativeArray<T5> t5,
                NativeArray<T6> t6,
                NativeArray<T7> t7,
                NativeArray<T8> t8,
                NativeArray<T9> t9,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
            : IChunkQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2,
                Span<T3> t3,
                Span<T4> t4,
                Span<T5> t5,
                Span<T6> t6,
                Span<T7> t7,
                Span<T8> t8,
                Span<T9> t9
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();
                var nArray3 = jobChunkHandle.GetComponentArray<T3>();
                var nArray4 = jobChunkHandle.GetComponentArray<T4>();
                var nArray5 = jobChunkHandle.GetComponentArray<T5>();
                var nArray6 = jobChunkHandle.GetComponentArray<T6>();
                var nArray7 = jobChunkHandle.GetComponentArray<T7>();
                var nArray8 = jobChunkHandle.GetComponentArray<T8>();
                var nArray9 = jobChunkHandle.GetComponentArray<T9>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    nArray3,
                    nArray4,
                    nArray5,
                    nArray6,
                    nArray7,
                    nArray8,
                    nArray9,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);
                jHandle = nArray3.Dispose(jHandle);
                jHandle = nArray4.Dispose(jHandle);
                jHandle = nArray5.Dispose(jHandle);
                jHandle = nArray6.Dispose(jHandle);
                jHandle = nArray7.Dispose(jHandle);
                jHandle = nArray8.Dispose(jHandle);
                jHandle = nArray9.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        /// <typeparam name="T7"></typeparam>
        /// <typeparam name="T8"></typeparam>
        /// <typeparam name="T9"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 10, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>,
                T0, T1, T2, T3, T4, T5, T6, T7, T8, T9
            >(new JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                NativeArray<T3> t3,
                NativeArray<T4> t4,
                NativeArray<T5> t5,
                NativeArray<T6> t6,
                NativeArray<T7> t7,
                NativeArray<T8> t8,
                NativeArray<T9> t9,
                NativeArray<T10> t10,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
            : IChunkQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2,
                Span<T3> t3,
                Span<T4> t4,
                Span<T5> t5,
                Span<T6> t6,
                Span<T7> t7,
                Span<T8> t8,
                Span<T9> t9,
                Span<T10> t10
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();
                var nArray3 = jobChunkHandle.GetComponentArray<T3>();
                var nArray4 = jobChunkHandle.GetComponentArray<T4>();
                var nArray5 = jobChunkHandle.GetComponentArray<T5>();
                var nArray6 = jobChunkHandle.GetComponentArray<T6>();
                var nArray7 = jobChunkHandle.GetComponentArray<T7>();
                var nArray8 = jobChunkHandle.GetComponentArray<T8>();
                var nArray9 = jobChunkHandle.GetComponentArray<T9>();
                var nArray10 = jobChunkHandle.GetComponentArray<T10>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    nArray3,
                    nArray4,
                    nArray5,
                    nArray6,
                    nArray7,
                    nArray8,
                    nArray9,
                    nArray10,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);
                jHandle = nArray3.Dispose(jHandle);
                jHandle = nArray4.Dispose(jHandle);
                jHandle = nArray5.Dispose(jHandle);
                jHandle = nArray6.Dispose(jHandle);
                jHandle = nArray7.Dispose(jHandle);
                jHandle = nArray8.Dispose(jHandle);
                jHandle = nArray9.Dispose(jHandle);
                jHandle = nArray10.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        /// <typeparam name="T7"></typeparam>
        /// <typeparam name="T8"></typeparam>
        /// <typeparam name="T9"></typeparam>
        /// <typeparam name="T10"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 11, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>,
                T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10
            >(new JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                NativeArray<T3> t3,
                NativeArray<T4> t4,
                NativeArray<T5> t5,
                NativeArray<T6> t6,
                NativeArray<T7> t7,
                NativeArray<T8> t8,
                NativeArray<T9> t9,
                NativeArray<T10> t10,
                NativeArray<T11> t11,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
            : IChunkQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2,
                Span<T3> t3,
                Span<T4> t4,
                Span<T5> t5,
                Span<T6> t6,
                Span<T7> t7,
                Span<T8> t8,
                Span<T9> t9,
                Span<T10> t10,
                Span<T11> t11
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();
                var nArray3 = jobChunkHandle.GetComponentArray<T3>();
                var nArray4 = jobChunkHandle.GetComponentArray<T4>();
                var nArray5 = jobChunkHandle.GetComponentArray<T5>();
                var nArray6 = jobChunkHandle.GetComponentArray<T6>();
                var nArray7 = jobChunkHandle.GetComponentArray<T7>();
                var nArray8 = jobChunkHandle.GetComponentArray<T8>();
                var nArray9 = jobChunkHandle.GetComponentArray<T9>();
                var nArray10 = jobChunkHandle.GetComponentArray<T10>();
                var nArray11 = jobChunkHandle.GetComponentArray<T11>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    nArray3,
                    nArray4,
                    nArray5,
                    nArray6,
                    nArray7,
                    nArray8,
                    nArray9,
                    nArray10,
                    nArray11,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);
                jHandle = nArray3.Dispose(jHandle);
                jHandle = nArray4.Dispose(jHandle);
                jHandle = nArray5.Dispose(jHandle);
                jHandle = nArray6.Dispose(jHandle);
                jHandle = nArray7.Dispose(jHandle);
                jHandle = nArray8.Dispose(jHandle);
                jHandle = nArray9.Dispose(jHandle);
                jHandle = nArray10.Dispose(jHandle);
                jHandle = nArray11.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        /// <typeparam name="T7"></typeparam>
        /// <typeparam name="T8"></typeparam>
        /// <typeparam name="T9"></typeparam>
        /// <typeparam name="T10"></typeparam>
        /// <typeparam name="T11"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 12, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>,
                T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11
            >(new JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
            where T12 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                NativeArray<T3> t3,
                NativeArray<T4> t4,
                NativeArray<T5> t5,
                NativeArray<T6> t6,
                NativeArray<T7> t7,
                NativeArray<T8> t8,
                NativeArray<T9> t9,
                NativeArray<T10> t10,
                NativeArray<T11> t11,
                NativeArray<T12> t12,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
            : IChunkQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
            where T12 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2,
                Span<T3> t3,
                Span<T4> t4,
                Span<T5> t5,
                Span<T6> t6,
                Span<T7> t7,
                Span<T8> t8,
                Span<T9> t9,
                Span<T10> t10,
                Span<T11> t11,
                Span<T12> t12
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();
                var nArray3 = jobChunkHandle.GetComponentArray<T3>();
                var nArray4 = jobChunkHandle.GetComponentArray<T4>();
                var nArray5 = jobChunkHandle.GetComponentArray<T5>();
                var nArray6 = jobChunkHandle.GetComponentArray<T6>();
                var nArray7 = jobChunkHandle.GetComponentArray<T7>();
                var nArray8 = jobChunkHandle.GetComponentArray<T8>();
                var nArray9 = jobChunkHandle.GetComponentArray<T9>();
                var nArray10 = jobChunkHandle.GetComponentArray<T10>();
                var nArray11 = jobChunkHandle.GetComponentArray<T11>();
                var nArray12 = jobChunkHandle.GetComponentArray<T12>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    nArray3,
                    nArray4,
                    nArray5,
                    nArray6,
                    nArray7,
                    nArray8,
                    nArray9,
                    nArray10,
                    nArray11,
                    nArray12,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);
                jHandle = nArray3.Dispose(jHandle);
                jHandle = nArray4.Dispose(jHandle);
                jHandle = nArray5.Dispose(jHandle);
                jHandle = nArray6.Dispose(jHandle);
                jHandle = nArray7.Dispose(jHandle);
                jHandle = nArray8.Dispose(jHandle);
                jHandle = nArray9.Dispose(jHandle);
                jHandle = nArray10.Dispose(jHandle);
                jHandle = nArray11.Dispose(jHandle);
                jHandle = nArray12.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        /// <typeparam name="T7"></typeparam>
        /// <typeparam name="T8"></typeparam>
        /// <typeparam name="T9"></typeparam>
        /// <typeparam name="T10"></typeparam>
        /// <typeparam name="T11"></typeparam>
        /// <typeparam name="T12"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
            where T12 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 13, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>,
                T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12
            >(new JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
            where T12 : struct, IComponent
            where T13 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                NativeArray<T3> t3,
                NativeArray<T4> t4,
                NativeArray<T5> t5,
                NativeArray<T6> t6,
                NativeArray<T7> t7,
                NativeArray<T8> t8,
                NativeArray<T9> t9,
                NativeArray<T10> t10,
                NativeArray<T11> t11,
                NativeArray<T12> t12,
                NativeArray<T13> t13,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
            : IChunkQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
            where T12 : struct, IComponent
            where T13 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2,
                Span<T3> t3,
                Span<T4> t4,
                Span<T5> t5,
                Span<T6> t6,
                Span<T7> t7,
                Span<T8> t8,
                Span<T9> t9,
                Span<T10> t10,
                Span<T11> t11,
                Span<T12> t12,
                Span<T13> t13
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();
                var nArray3 = jobChunkHandle.GetComponentArray<T3>();
                var nArray4 = jobChunkHandle.GetComponentArray<T4>();
                var nArray5 = jobChunkHandle.GetComponentArray<T5>();
                var nArray6 = jobChunkHandle.GetComponentArray<T6>();
                var nArray7 = jobChunkHandle.GetComponentArray<T7>();
                var nArray8 = jobChunkHandle.GetComponentArray<T8>();
                var nArray9 = jobChunkHandle.GetComponentArray<T9>();
                var nArray10 = jobChunkHandle.GetComponentArray<T10>();
                var nArray11 = jobChunkHandle.GetComponentArray<T11>();
                var nArray12 = jobChunkHandle.GetComponentArray<T12>();
                var nArray13 = jobChunkHandle.GetComponentArray<T13>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    nArray3,
                    nArray4,
                    nArray5,
                    nArray6,
                    nArray7,
                    nArray8,
                    nArray9,
                    nArray10,
                    nArray11,
                    nArray12,
                    nArray13,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);
                jHandle = nArray3.Dispose(jHandle);
                jHandle = nArray4.Dispose(jHandle);
                jHandle = nArray5.Dispose(jHandle);
                jHandle = nArray6.Dispose(jHandle);
                jHandle = nArray7.Dispose(jHandle);
                jHandle = nArray8.Dispose(jHandle);
                jHandle = nArray9.Dispose(jHandle);
                jHandle = nArray10.Dispose(jHandle);
                jHandle = nArray11.Dispose(jHandle);
                jHandle = nArray12.Dispose(jHandle);
                jHandle = nArray13.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        /// <typeparam name="T7"></typeparam>
        /// <typeparam name="T8"></typeparam>
        /// <typeparam name="T9"></typeparam>
        /// <typeparam name="T10"></typeparam>
        /// <typeparam name="T11"></typeparam>
        /// <typeparam name="T12"></typeparam>
        /// <typeparam name="T13"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
            where T12 : struct, IComponent
            where T13 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 14, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>,
                T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13
            >(new JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
            where T12 : struct, IComponent
            where T13 : struct, IComponent
            where T14 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                NativeArray<T3> t3,
                NativeArray<T4> t4,
                NativeArray<T5> t5,
                NativeArray<T6> t6,
                NativeArray<T7> t7,
                NativeArray<T8> t8,
                NativeArray<T9> t9,
                NativeArray<T10> t10,
                NativeArray<T11> t11,
                NativeArray<T12> t12,
                NativeArray<T13> t13,
                NativeArray<T14> t14,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
            : IChunkQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
            where T12 : struct, IComponent
            where T13 : struct, IComponent
            where T14 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2,
                Span<T3> t3,
                Span<T4> t4,
                Span<T5> t5,
                Span<T6> t6,
                Span<T7> t7,
                Span<T8> t8,
                Span<T9> t9,
                Span<T10> t10,
                Span<T11> t11,
                Span<T12> t12,
                Span<T13> t13,
                Span<T14> t14
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();
                var nArray3 = jobChunkHandle.GetComponentArray<T3>();
                var nArray4 = jobChunkHandle.GetComponentArray<T4>();
                var nArray5 = jobChunkHandle.GetComponentArray<T5>();
                var nArray6 = jobChunkHandle.GetComponentArray<T6>();
                var nArray7 = jobChunkHandle.GetComponentArray<T7>();
                var nArray8 = jobChunkHandle.GetComponentArray<T8>();
                var nArray9 = jobChunkHandle.GetComponentArray<T9>();
                var nArray10 = jobChunkHandle.GetComponentArray<T10>();
                var nArray11 = jobChunkHandle.GetComponentArray<T11>();
                var nArray12 = jobChunkHandle.GetComponentArray<T12>();
                var nArray13 = jobChunkHandle.GetComponentArray<T13>();
                var nArray14 = jobChunkHandle.GetComponentArray<T14>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    nArray3,
                    nArray4,
                    nArray5,
                    nArray6,
                    nArray7,
                    nArray8,
                    nArray9,
                    nArray10,
                    nArray11,
                    nArray12,
                    nArray13,
                    nArray14,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);
                jHandle = nArray3.Dispose(jHandle);
                jHandle = nArray4.Dispose(jHandle);
                jHandle = nArray5.Dispose(jHandle);
                jHandle = nArray6.Dispose(jHandle);
                jHandle = nArray7.Dispose(jHandle);
                jHandle = nArray8.Dispose(jHandle);
                jHandle = nArray9.Dispose(jHandle);
                jHandle = nArray10.Dispose(jHandle);
                jHandle = nArray11.Dispose(jHandle);
                jHandle = nArray12.Dispose(jHandle);
                jHandle = nArray13.Dispose(jHandle);
                jHandle = nArray14.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        /// <typeparam name="T7"></typeparam>
        /// <typeparam name="T8"></typeparam>
        /// <typeparam name="T9"></typeparam>
        /// <typeparam name="T10"></typeparam>
        /// <typeparam name="T11"></typeparam>
        /// <typeparam name="T12"></typeparam>
        /// <typeparam name="T13"></typeparam>
        /// <typeparam name="T14"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
            where T12 : struct, IComponent
            where T13 : struct, IComponent
            where T14 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 15, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>,
                T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14
            >(new JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
        public interface IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
            where T12 : struct, IComponent
            where T13 : struct, IComponent
            where T14 : struct, IComponent
            where T15 : struct, IComponent
        {
            JobHandle Schedule(
                JobChunkHandle chunk,
                NativeArray<T0> t0,
                NativeArray<T1> t1,
                NativeArray<T2> t2,
                NativeArray<T3> t3,
                NativeArray<T4> t4,
                NativeArray<T5> t5,
                NativeArray<T6> t6,
                NativeArray<T7> t7,
                NativeArray<T8> t8,
                NativeArray<T9> t9,
                NativeArray<T10> t10,
                NativeArray<T11> t11,
                NativeArray<T12> t12,
                NativeArray<T13> t13,
                NativeArray<T14> t14,
                NativeArray<T15> t15,
                JobHandle dependsOn
            );
        }

        private struct JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
            : IChunkQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
            where T12 : struct, IComponent
            where T13 : struct, IComponent
            where T14 : struct, IComponent
            where T15 : struct, IComponent
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
        {
            private readonly TScheduler _scheduler;
            private readonly UnityMyriadSafetySystemAdapter _safety;
            private readonly JobHandle _dependsOn;

            private NativeReference<JobHandle> _handle;

#pragma warning disable IDE0044
            private NativeList<GCHandle> _pins;
#pragma warning restore IDE0044

            public JobQuery(TScheduler scheduler, UnityMyriadSafetySystemAdapter safety, JobHandle dependsOn, NativeReference<JobHandle> handle, NativeList<GCHandle> pins)
            {
                _scheduler = scheduler;
                _safety = safety;
                _dependsOn = dependsOn;

                _handle = handle;
                _pins = pins;
            }

            public void Execute(
                ChunkHandle chunk,
                Span<T0> t0,
                Span<T1> t1,
                Span<T2> t2,
                Span<T3> t3,
                Span<T4> t4,
                Span<T5> t5,
                Span<T6> t6,
                Span<T7> t7,
                Span<T8> t8,
                Span<T9> t9,
                Span<T10> t10,
                Span<T11> t11,
                Span<T12> t12,
                Span<T13> t13,
                Span<T14> t14,
                Span<T15> t15
            )
            {
                // Early out if there's no work to do
                var entityCount = chunk.EntityCount;
                if (entityCount == 0)
                    return;

                // Wrap chunk handle
                var jobChunkHandle = new JobChunkHandle(chunk, _pins);

                // Get components arrays
                var nArray0 = jobChunkHandle.GetComponentArray<T0>();
                var nArray1 = jobChunkHandle.GetComponentArray<T1>();
                var nArray2 = jobChunkHandle.GetComponentArray<T2>();
                var nArray3 = jobChunkHandle.GetComponentArray<T3>();
                var nArray4 = jobChunkHandle.GetComponentArray<T4>();
                var nArray5 = jobChunkHandle.GetComponentArray<T5>();
                var nArray6 = jobChunkHandle.GetComponentArray<T6>();
                var nArray7 = jobChunkHandle.GetComponentArray<T7>();
                var nArray8 = jobChunkHandle.GetComponentArray<T8>();
                var nArray9 = jobChunkHandle.GetComponentArray<T9>();
                var nArray10 = jobChunkHandle.GetComponentArray<T10>();
                var nArray11 = jobChunkHandle.GetComponentArray<T11>();
                var nArray12 = jobChunkHandle.GetComponentArray<T12>();
                var nArray13 = jobChunkHandle.GetComponentArray<T13>();
                var nArray14 = jobChunkHandle.GetComponentArray<T14>();
                var nArray15 = jobChunkHandle.GetComponentArray<T15>();

                // Call user code to schedule a job
                var jHandle = _scheduler.Schedule(
                    jobChunkHandle,
                    nArray0,
                    nArray1,
                    nArray2,
                    nArray3,
                    nArray4,
                    nArray5,
                    nArray6,
                    nArray7,
                    nArray8,
                    nArray9,
                    nArray10,
                    nArray11,
                    nArray12,
                    nArray13,
                    nArray14,
                    nArray15,
                    JobHandle.CombineDependencies(
                        _dependsOn,
                        _safety.GetAttachedJob(chunk.Archetype.ArchetypeId)
                    )
                );

                // Dispose the auto arrays
                jHandle = nArray0.Dispose(jHandle);
                jHandle = nArray1.Dispose(jHandle);
                jHandle = nArray2.Dispose(jHandle);
                jHandle = nArray3.Dispose(jHandle);
                jHandle = nArray4.Dispose(jHandle);
                jHandle = nArray5.Dispose(jHandle);
                jHandle = nArray6.Dispose(jHandle);
                jHandle = nArray7.Dispose(jHandle);
                jHandle = nArray8.Dispose(jHandle);
                jHandle = nArray9.Dispose(jHandle);
                jHandle = nArray10.Dispose(jHandle);
                jHandle = nArray11.Dispose(jHandle);
                jHandle = nArray12.Dispose(jHandle);
                jHandle = nArray13.Dispose(jHandle);
                jHandle = nArray14.Dispose(jHandle);
                jHandle = nArray15.Dispose(jHandle);

                // Chain this handle with all the others generated for other chunks
                _handle.Value = JobHandle.CombineDependencies(_handle.Value, jHandle);
            }
        }

        /// <summary>
        /// Schedule jobs to run over component data. Different jobs can be scheduled per chunk, this is controlled
        /// through the <see cref="TScheduler"/> struct.
        /// 
        /// Note that the Unity safety system does <b>NOT</b> apply to the data here, if two different queries are
        /// scheduled they may both access the same data in parallel and cause serious issues. it's up to the caller
        /// to ensure this does not happen!
        /// </summary>
        /// <typeparam name="TScheduler">Schedules jobs for chunks</typeparam>
        /// <typeparam name="T0"></typeparam>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        /// <typeparam name="T7"></typeparam>
        /// <typeparam name="T8"></typeparam>
        /// <typeparam name="T9"></typeparam>
        /// <typeparam name="T10"></typeparam>
        /// <typeparam name="T11"></typeparam>
        /// <typeparam name="T12"></typeparam>
        /// <typeparam name="T13"></typeparam>
        /// <typeparam name="T14"></typeparam>
        /// <typeparam name="T15"></typeparam>
        /// <param name="world"></param>
        /// <param name="sched"></param>
        /// <param name="query"></param>
        /// <param name="dependsOn"></param>
        /// <returns>Combined job handle of all chunk jobs</returns>
        public static QueryJobHandle Schedule<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this World world, TScheduler sched, [CanBeNull] ref QueryDescription query, JobHandle dependsOn = default)
            where TScheduler : IJobQueryScheduler<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
            where T0 : struct, IComponent
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
            where T7 : struct, IComponent
            where T8 : struct, IComponent
            where T9 : struct, IComponent
            where T10 : struct, IComponent
            where T11 : struct, IComponent
            where T12 : struct, IComponent
            where T13 : struct, IComponent
            where T14 : struct, IComponent
            where T15 : struct, IComponent
        {
            query ??= world.GetCachedQuery<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>();

            var chunkCount = query.CountChunks();
            if (chunkCount == 0)
                return default;

            // Get the safety system
            var safety = (UnityMyriadSafetySystemAdapter)world.LockManager;

            // Create collections to accumulate things we'll need to clean up afterwards
            var pins = new NativeList<GCHandle>(chunkCount * 16, Allocator.TempJob);
            var handle = new NativeReference<JobHandle>(default, Allocator.TempJob);

            // Execute standard Myriad.ECS query which will schedule a job per chunk
            world.ExecuteChunk<
                JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>,
                T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15
            >(new JobQuery<TScheduler, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(sched, safety, dependsOn, handle, pins));

            // Ensure all jobs are started before we wait on them
            JobHandle.ScheduleBatchedJobs();

            // Take the handle
            var jobHandle = handle.Value;
            handle.Dispose();

            return new QueryJobHandle(jobHandle, pins);
        }

        // proof x
    }
}


