using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Systems;

namespace Assets.Scenes.Transforms
{
    public class TransformSystem
        : WorldSystemGroup<GameTime>
    {
        protected override ISystemGroup<GameTime> CreateGroup(World world)
        {
            return new SystemGroup<GameTime>("Transform",
                new MyriadTransformSystem<GameTime>(world)
            );
        }
    }
}
