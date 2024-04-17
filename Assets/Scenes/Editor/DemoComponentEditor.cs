using Packages.me.martindevans.myriad_unity_integration.Editor.Entities;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using UnityEditor;

namespace Assets.Scenes.Editor
{
    [MyriadComponentEditor(typeof(DemoComponent))]
    public class DemoComponentEditor
        : IMyriadComponentEditor
    {
        public void Draw(MyriadEntity entity)
        {
            ref var demo = ref entity.GetMyriadComponent<DemoComponent>();
            demo.Value = EditorGUILayout.DelayedIntField("Value", demo.Value);
        }
    }
}
