using System;
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
        /// <summary>
        /// Map from (Archetype ID, Component ID) -> JobHandle which is accessing this component in this archetype
        /// </summary>
        /// <remarks>All handles here are automatically joined into the overall archetype handle too</remarks>
        private readonly Dictionary<(long, ComponentID), JobHandle> _archetypeComponentHandles = new();

        /// <summary>
        /// Block on the job handle for this archetype
        /// </summary>
        /// <param name="archetype"></param>
        public void Block(Archetype archetype)
        {
            foreach (var component in archetype.Components)
                Block(archetype, component);
        }

        /// <summary>
        /// Wait for multithreaded work which is accessing a specific component in a specific archetype to finish
        /// </summary>
        /// <param name="archetype"></param>
        /// <param name="id"></param>
        public void Block(Archetype archetype, ComponentID id)
        {
            if (_archetypeComponentHandles.Remove((archetype.ArchetypeId, id), out var handle))
                handle.Complete();
        }

        /// <summary>
        /// Attach a job handle to the given archetype/component pair
        /// </summary>
        /// <param name="archetypeId"></param>
        /// <param name="components"></param>
        /// <param name="handle"></param>
        public void AttachJob(long archetypeId, Span<ComponentID> components, JobHandle handle)
        {
            // Store handle for all components
            foreach (var component in components)
            {
                // Combine with existing handle
                if (_archetypeComponentHandles.TryGetValue((archetypeId, component), out var acHandle))
                    handle = JobHandle.CombineDependencies(handle, acHandle);

                // Store it
                _archetypeComponentHandles[(archetypeId, component)] = handle;
            }
        }

        /// <summary>
        /// Get a handle for accessing specific components in a specific archetype
        /// </summary>
        /// <param name="archetypeId"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        public JobHandle GetAttachedJob(long archetypeId, Span<ComponentID> components)
        {
            var handle = default(JobHandle);

            foreach (var component in components)
                if (_archetypeComponentHandles.TryGetValue((archetypeId, component), out var value))
                    handle = JobHandle.CombineDependencies(handle, value);

            return handle;
        }
    }
}
