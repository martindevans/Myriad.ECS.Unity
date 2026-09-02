using Myriad.ECS.Queries;
using System;
using System.Runtime.InteropServices;
using Myriad.ECS;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Queries
{
    /// <summary>
    /// Provides access to chunk data in a job safe way
    /// </summary>
    public ref struct JobChunkHandle
    {
        private readonly ChunkHandle _handle;
        private NativeList<GCHandle> _pins;

        /// <summary>
        /// Get the number of entities in this chunk
        /// </summary>
        public int EntityCount => _handle.EntityCount;

        /// <summary>
        /// Unique ID of this chunk
        /// </summary>
        public long ChunkId => _handle.ChunkId;

        internal JobChunkHandle(
            ChunkHandle handle,
            NativeList<GCHandle> pins
        )
        {
            _handle = handle;
            _pins = pins;
        }

        /// <summary>
        /// Get a native array with a view of entity data that can be passed into a job.
        /// <b>Arrays retrieved through this method must be disposed!</b>
        /// </summary>
        /// <returns></returns>
        public NativeArray<EntityId> GetEntityArray()
        {
            // Pin array for component
            var array = _handle.Danger().GetEntityIdArray();
            var pin = GCHandle.Alloc(array, GCHandleType.Pinned);
            _pins.Add(pin);

            unsafe
            {
                // Wrap as native array
                var nArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(
                    (void*)pin.AddrOfPinnedObject(), _handle.EntityCount, Allocator.None
                );

                // Attach a safety handle.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(
                    ref nArray,
                    AtomicSafetyHandle.Create()
                );
#endif

                return nArray;
            }
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
        /// <b>Arrays retrieved through this method must be disposed!</b>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public NativeArray<T> GetComponentArray<T>()
            where T : struct, IComponent
        {
            return GetComponentArray<T>(blocking: true);
        }

        internal NativeArray<T> GetComponentArray<T>(bool blocking)
            where T : struct, IComponent
        {
            // Pin array for component
            var array = _handle.Danger().GetComponentArray<T>(blocking: blocking);
            var pin = GCHandle.Alloc(array, GCHandleType.Pinned);
            _pins.Add(pin);

            unsafe
            {
                // Wrap as native array
                var nArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                    (void*)pin.AddrOfPinnedObject(), _handle.EntityCount, Allocator.None
                );

                // Attach a safety handle.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(
                    ref nArray,
                    AtomicSafetyHandle.Create()
                );
#endif

                return nArray;
            }
        }

        /// <summary>
        /// Get direct access to components
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Span<T> GetComponentSpan<T>()
            where T : IComponent
        {
            return _handle.GetComponentSpan<T>();
        }
    }
}
