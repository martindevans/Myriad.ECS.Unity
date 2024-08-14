using Packages.me.martindevans.myriad_unity_integration.Runtime;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    [UnityEditor.CustomEditor(typeof(GameTimeWorldHost))]
    public class GameTimeWorldHostEditor
        : BaseSimulationHostEditor<GameTimeWorldHost, GameTime>
    {
    }
}
