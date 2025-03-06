using Myriad.ECS.Systems;
using Packages.me.martindevans.myriad_unity_integration.Editor.World;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using UnityEditor;

namespace Assets.Scenes.Editor
{
    [CustomEditor(typeof(SimulationHost))]
    public class SimulationHostEditor
        : BaseSimulationHostEditor<SimulationHost, GameTime>
    {
    }
}
