using Myriad.ECS;
using Myriad.ECS.Command;
using Myriad.ECS.Worlds;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    /// <summary>
    /// A component which is also a MonoBehaviour
    /// </summary>
    public interface IBehaviourComponent
        : IComponent
    {
        /// <summary>
        /// Called automatically when this GameObject is bound to a Myriad entity. Requires a <see cref="MyriadEntity"/> component
        /// attached to the gameObject. Note that this will only be called once, when the entity is initially bound, components
        /// which are added later will not be bound!
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cmd"></param>
        public void Bind(Entity entity, CommandBuffer cmd);
    }
}
