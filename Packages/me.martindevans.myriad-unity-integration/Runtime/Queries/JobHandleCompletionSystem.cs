using System;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using System.Collections.Generic;
using static Myriad.ECS.Worlds.WorldJobExtensions;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Queries
{
    /// <summary>
    /// A collection of handles which will automatically be completed later
    /// </summary>
    public interface IQueryJobHandleCompletionGate
    {
        void AddHandle(QueryJobHandle handle);
    }

    public abstract class BaseQueryJobHandleCompletionGate
        : IQueryJobHandleCompletionGate, IDisposable
    {
        private readonly List<QueryJobHandle> _handles = new();

        public void AddHandle(QueryJobHandle handle)
        {
            _handles.Add(handle);
        }

        protected void Complete()
        {
            foreach (var handle in _handles)
                handle.Dispose();
            _handles.Clear();
        }

        public void Dispose()
        {
            Complete();
        }
    }

    /// <summary>
    /// Waits on all <see cref="WorldJobExtensions.QueryJobHandle"/> that have been added to it
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueryJobHandleCompletionGateBeforeUpdate<T>
        : BaseQueryJobHandleCompletionGate, ISystemBefore<T>
    {
        public void BeforeUpdate(T data)
        {
            Complete();
        }

        public void Update(T data)
        {
        }
    }

    /// <summary>
    /// Waits on all <see cref="WorldJobExtensions.QueryJobHandle"/> that have been added to it
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueryJobHandleCompletionGateUpdate<T>
        : BaseQueryJobHandleCompletionGate, ISystem<T>
    {
        public void Update(T data)
        {
            Complete();
        }
    }

    /// <summary>
    /// Waits on all <see cref="WorldJobExtensions.QueryJobHandle"/> that have been added to it
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class QueryJobHandleCompletionGateAfterUpdate<T>
        : BaseQueryJobHandleCompletionGate, ISystemAfter<T>
    {
        public void Update(T data)
        {
        }

        public void AfterUpdate(T data)
        {
            Complete();
        }
    }
}
