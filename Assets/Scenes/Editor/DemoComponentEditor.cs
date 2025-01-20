using Myriad.ECS;
using Packages.me.martindevans.myriad_unity_integration.Editor.Entities;
using UnityEditor;

namespace Assets.Scenes.Editor
{
    [MyriadComponentEditor(typeof(DemoComponent))]
    public class DemoComponentEditor
        : IMyriadComponentEditor
    {
        public void Draw(Entity entity)
        {
            ref var demo = ref entity.GetComponentRef<DemoComponent>();
            demo.Value = EditorGUILayout.DelayedIntField("Value", demo.Value);
        }
    }
}
