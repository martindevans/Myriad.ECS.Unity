using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Systems
{
    public class UnityMyriadSystemGroup
        : WorldSystemGroup<GameTime>
    {
        protected override ISystemGroup<GameTime> CreateGroup(BaseSimulationHost<GameTime> world)
        {
            return new SystemGroup<GameTime>(
                "Unity/Myriad Integration",
                new MyriadEntityBindingSystem<GameTime>(world.World)
            );
        }
    }
}
