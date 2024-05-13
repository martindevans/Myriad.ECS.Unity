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
        public void Bind(World world, Entity entity, CommandBuffer cmd);
    }
}
