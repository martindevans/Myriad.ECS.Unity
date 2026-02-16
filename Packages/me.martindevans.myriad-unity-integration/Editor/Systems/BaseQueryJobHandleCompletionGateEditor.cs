using Myriad.ECS.Systems;
using Packages.me.martindevans.myriad_unity_integration.Editor.World;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Queries;
using UnityEditor;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.Systems
{
    public class BaseQueryJobHandleCompletionGateEditor
        : IMyriadSystemEditor
    {
        public void Draw<T>(ISystem<T> sys)
        {
            var system = (sys as BaseQueryJobHandleCompletionGate)!;

            EditorGUILayout.LabelField($"Previous Handle Count: {system.PreviousCompleteHandleCount}");
        }
    }

    [MyriadSystemEditor(typeof(QueryJobHandleCompletionGateBeforeUpdate<>))]
    public class QueryJobHandleCompletionGateBeforeUpdateEditor
        : BaseQueryJobHandleCompletionGateEditor
    {
    }

    [MyriadSystemEditor(typeof(QueryJobHandleCompletionGateUpdate<>))]
    public class QueryJobHandleCompletionGateUpdateEditor
        : BaseQueryJobHandleCompletionGateEditor
    {
    }

    [MyriadSystemEditor(typeof(QueryJobHandleCompletionGateAfterUpdate<>))]
    public class QueryJobHandleCompletionGateAfterUpdateEditor
        : BaseQueryJobHandleCompletionGateEditor
    {
    }
}
