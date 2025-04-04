using Myriad.ECS.Command;
using Myriad.ECS.Worlds;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    /// <summary>
    /// Contains a world
    /// </summary>
    public abstract class BaseWorldHost
        : MonoBehaviour
    {
        private CommandBuffer _cmd;
        /// <summary>
        /// A shared command buffer which will be executed before updates every frame.
        /// </summary>
        public CommandBuffer CommandBuffer
        {
            get
            {
                if (_cmd == null)
                    _cmd = new CommandBuffer(World);
                return _cmd;
            }
        }

        /// <summary>
        /// Za Warudo
        /// </summary>
        public abstract World World { get; }

        /// <summary>
        /// Indicates if the world should be disposed when OnDestroy happens
        /// </summary>
        protected virtual bool DisposeWorld => true;

        protected virtual void Update()
        {
            CommandBuffer.Playback().Dispose();
        }

        /// <summary>
        /// Disposes the world
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (DisposeWorld)
                World.Dispose();
        }
    }
}
