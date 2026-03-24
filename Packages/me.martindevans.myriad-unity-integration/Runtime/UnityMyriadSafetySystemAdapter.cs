using System.Collections.Generic;
using Myriad.ECS.IDs;
using Myriad.ECS.Locks;
using Myriad.ECS.Worlds.Archetypes;
using Unity.Jobs;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    /// <summary>
    /// Stores a handle for each archetype. Allowing blocking on work using that archetype.
    /// </summary>
    public class UnityMyriadSafetySystemAdapter
        : IWorldArchetypeSafetyManager
    {
        private readonly Dictionary<long, JobHandle> _archetypeHandles = new();

        /// <summary>
        /// Block on the job handle for this archetype
        /// </summary>
        /// <param name="archetype"></param>
        public void Block(Archetype archetype)
        {
            if (_archetypeHandles.Remove(archetype.ArchetypeId, out var handle))
                handle.Complete();
        }

        /// <summary>
        /// Wait for multithreaded work which is accessing a specific component in a specific archetype to finish
        /// </summary>
        /// <param name="archetype"></param>
        /// <param name="id"></param>
        public void Block(Archetype archetype, ComponentID id)
        {
            // Defer to blocking on the entire archetype
            Block(archetype);
        }

        /// <summary>
        /// Attach a job handle to the given archetype
        /// </summary>
        /// <param name="archetypeId"></param>
        /// <param name="handle"></param>
        public void AttachJob(long archetypeId, JobHandle handle)
        {
            // Combine with existing handle (if any)
            if (_archetypeHandles.TryGetValue(archetypeId, out var archHandle))
                handle = JobHandle.CombineDependencies(handle, archHandle);

            _archetypeHandles[archetypeId] = handle;
        }

        /// <summary>
        /// Get a combined handle for all jobs attached to the archetype
        /// </summary>
        /// <param name="archetypeId"></param>
        /// <returns></returns>
        public JobHandle GetAttachedJob(long archetypeId)
        {
            return _archetypeHandles.GetValueOrDefault(archetypeId);
        }
    }
}
