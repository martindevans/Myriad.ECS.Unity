using Myriad.ECS;
using Myriad.ECS.Worlds;
using Packages.me.martindevans.myriad_unity_integration.Editor.Entities;
using UnityEditor;

namespace Assets.Scenes.Editor
{
    [MyriadComponentEditor(typeof(DemoComponent))]
    public class DemoComponentEditor
        : IMyriadComponentEditor
    {
        public bool IsEmpty => false;

        public void Draw(World world, Entity entity)
        {
            ref var demo = ref entity.GetComponentRef<DemoComponent>(world);
            demo.Value = EditorGUILayout.DelayedIntField("Value", demo.Value);
        }
    }
}
