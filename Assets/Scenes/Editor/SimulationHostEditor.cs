using Packages.me.martindevans.myriad_unity_integration.Editor.World;
using UnityEditor;

namespace Assets.Scenes.Editor
{
    [CustomEditor(typeof(SimulationHost))]
    public class SimulationHostEditor
        : BaseSimulationHostEditor<SimulationHost, int>
    {
    }
}
