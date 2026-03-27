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
        private readonly Dictionary<(long, ComponentID), JobHandle> _archetypeComponentHandles = new();

        /// <summary>
        /// Block on the job handle for this archetype
        /// </summary>
        /// <param name="archetype"></param>
        void IWorldArchetypeSafetyManager.Block(Archetype archetype)
        {
            var handle = default(JobHandle);

            foreach (var component in archetype.Components)
                if (_archetypeComponentHandles.Remove((archetype.ArchetypeId, component), out var value))
                    handle = JobHandle.CombineDependencies(handle, value);

            handle.Complete();
        }

        /// <summary>
        /// Wait for multithreaded work which is accessing a set of components in a specific archetype to finish
        /// </summary>
        /// <param name="archetype"></param>
        /// <param name="ids"></param>
        void IWorldArchetypeSafetyManager.Block(Archetype archetype, ReadOnlySpan<ComponentID> ids)
        {
            GetAttachedJob(archetype.ArchetypeId, ids);
        }

        /// <summary>
        /// Attach a job handle to the given archetype/component pair
        /// </summary>
        /// <param name="archetypeId"></param>
        /// <param name="components"></param>
        /// <param name="handle"></param>
        public void AttachJob(long archetypeId, ReadOnlySpan<ComponentID> components, JobHandle handle)
        {
            foreach (var component in components)
            {
                if (_archetypeComponentHandles.TryGetValue((archetypeId, component), out var acHandle))
                {
                    acHandle = JobHandle.CombineDependencies(handle, acHandle);
                    _archetypeComponentHandles[(archetypeId, component)] = acHandle;
                }
                else
                {
                    _archetypeComponentHandles[(archetypeId, component)] = handle;
                }
            }
        }

        /// <summary>
        /// Get a handle for accessing specific components in a specific archetype
        /// </summary>
        /// <param name="archetypeId"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        public JobHandle GetAttachedJob(long archetypeId, ReadOnlySpan<ComponentID> components)
        {
            var handle = default(JobHandle);

            foreach (var component in components)
                if (_archetypeComponentHandles.TryGetValue((archetypeId, component), out var value))
                    handle = JobHandle.CombineDependencies(handle, value);

            return handle;
        }
    }
}
