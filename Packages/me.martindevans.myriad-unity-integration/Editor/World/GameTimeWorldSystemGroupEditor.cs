using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Systems;
using UnityEditor;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    [CustomEditor(typeof(WorldSystemGroup<GameTime>), true)]
    public class GameTimeWorldSystemGroupEditor
        : BaseWorldSystemGroupEditor<GameTime>
    {
    }
}