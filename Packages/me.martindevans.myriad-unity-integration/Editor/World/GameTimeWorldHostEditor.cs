using Myriad.ECS.Systems;
using Packages.me.martindevans.myriad_unity_integration.Runtime;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    [UnityEditor.CustomEditor(typeof(GameTimeSimulationHost))]
    public class GameTimeWorldHostEditor
        : BaseSimulationHostEditor<GameTimeSimulationHost, GameTime>
    {
    }
}
