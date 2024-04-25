using Myriad.ECS;
using Myriad.ECS.Command;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    public interface IAutobindComponent
        : IComponent
    {
        public void Bind(CommandBuffer.BufferedEntity entity);
    }
}