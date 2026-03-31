using Myriad.ECS.Systems;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Systems.Utility;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Systems
{
    public class UnityMyriadSystemGroup
        : WorldSystemGroup<GameTime>
    {
        protected override ISystemGroup<GameTime> CreateGroup(BaseSimulationHost<GameTime> world)
        {
            var utilityCmd = new CommandBufferSystem<GameTime>(world.World);

            return new SystemGroup<GameTime>(
                "Unity/Myriad Integration",
                new MyriadEntityBindingSystem<GameTime>(world.World),
                new SystemGroup<GameTime>(
                    "Utilities",
                    new GameTimeCountdownAutoDeleteSystem(world.World, utilityCmd.Buffer),
                    utilityCmd
                )
            );
        }
    }
}
