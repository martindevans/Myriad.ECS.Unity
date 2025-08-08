using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Systems;

namespace Assets.Scenes
{
    public class DemoSystemProvider
        : WorldSystemGroup<GameTime>
    {
        protected override ISystemGroup<GameTime> CreateGroup(BaseSimulationHost<GameTime> world)
        {
            return new OrderedParallelSystemGroup<GameTime>(
                "parallel group",
                new WasteTimeSystem(),
                new EmptySystem(),
                new EmptySystem(),
                new EmptySystem()
            );
        }
    }
}
