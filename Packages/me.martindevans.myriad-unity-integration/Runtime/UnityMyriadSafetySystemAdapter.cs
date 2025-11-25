using System.Collections.Generic;
using Myriad.ECS.Locks;
using Myriad.ECS.Worlds.Archetypes;
using Unity.Jobs;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    public class UnityMyriadSafetySystemAdapter
        : IWorldArchetypeSafetyManager
    {
        private readonly Dictionary<long, JobHandle> _handles = new();

        public void Block(Archetype archetype)
        {
            if (_handles.Remove(archetype.ArchetypeId, out var handle))
                handle.Complete();
        }

        /// <summary>
        /// Attach a job handle to the given archetype
        /// </summary>
        /// <param name="archetypeId"></param>
        /// <param name="handle"></param>
        public void AttachJob(long archetypeId, JobHandle handle)
        {
            // Combine with existing handle (if any)
            if (_handles.TryGetValue(archetypeId, out var archHandle))
                handle = JobHandle.CombineDependencies(handle, archHandle);

            _handles[archetypeId] = handle;
        }

        /// <summary>
        /// Get a combined handle for all jobs attached to the archetype
        /// </summary>
        /// <param name="archetypeId"></param>
        /// <returns></returns>
        public JobHandle GetAttachedJob(long archetypeId)
        {
            return _handles.GetValueOrDefault(archetypeId);
        }
    }
}
