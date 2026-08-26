using JetBrains.Annotations;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Queries
{
    /// <summary>
    /// A handle for a Unity job based Myriad query. <b>MUST</b> be waited on at least once for correctness!
    /// </summary>
    [MustDisposeResource]
    public struct QueryJobHandle
        : IDisposable
    {
        private JobHandle _jobHandle;
        private NativeList<GCHandle> _pins;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly SafetyObject _safety;
#endif


        public bool IsCompleted => _jobHandle.IsCompleted;

        public JobHandle Handle => _jobHandle;

        internal QueryJobHandle(JobHandle handle, NativeList<GCHandle> pins)
        {
            if (!pins.IsCreated)
                throw new ArgumentException("`pins` NativeList must be created", nameof(pins));

            _jobHandle = handle;
            _pins = pins;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            _safety = new SafetyObject();
#endif
        }

        public void Complete()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Completing a default handle is valid - so null safety is allowed!
            if (_safety != null)
                _safety.Dispose();
#endif

            _jobHandle.Complete();

            if (_pins.IsCreated)
            {
                for (var i = 0; i < _pins.Length; i++)
                {
                    var pin = _pins[i];
                    if (pin.IsAllocated)
                        _pins[i].Free();
                }

                _pins.Dispose();
            }
        }

        public void Dispose()
        {
            Complete();
        }

        /// <summary>
        /// Combine another job handle into this handle
        /// </summary>
        /// <param name="handle"></param>
        public void Chain(JobHandle handle)
        {
            _jobHandle = JobHandle.CombineDependencies(_jobHandle, handle);
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        /// <summary>
        /// Safety stuff is held in this object, so the handle can be passed around and they all share a reference to the same safety
        /// </summary>
        private class SafetyObject
            : IDisposable
        {
            private AtomicSafetyHandle _safety;
            private DisposeSentinel _sentinel;
            private bool _disposed;

            public SafetyObject()
            {
                DisposeSentinel.Create(out _safety, out _sentinel, 32, Allocator.Persistent);
            }

            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;

                DisposeSentinel.Dispose(ref _safety, ref _sentinel);
                _sentinel = null;
                _safety = default;
            }
        }
#endif
    }
}
