using Myriad.ECS;
using Myriad.ECS.Command;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    /// <summary>
    /// A component which is also a MonoBehaviour
    /// </summary>
    public interface IBehaviourComponent
        : IComponent
    {
        public void Bind(Entity entity, CommandBuffer cmd);
    }
}
