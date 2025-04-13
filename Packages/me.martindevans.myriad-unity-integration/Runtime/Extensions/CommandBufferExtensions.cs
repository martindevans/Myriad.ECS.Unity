using Myriad.ECS.Command;
using Myriad.ECS.Components;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Components;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Extensions
{
    public static class CommandBufferExtensions
    {
        public static CommandBuffer.BufferedEntity SetupGameObjectBinding(this CommandBuffer.BufferedEntity entity, Transform tr, DestructMode destruct)
        {
            return SetupGameObjectBinding(entity, tr.gameObject, destruct);
        }

        public static CommandBuffer.BufferedEntity SetupGameObjectBinding(this CommandBuffer.BufferedEntity entity, GameObject go, DestructMode destruct)
        {
            var me = go.GetOrAddComponent<MyriadEntity>();
            me.DestructMode = destruct;

            var eb = entity
                .Set(me)
                .Set(new DebugDisplayName(go.name), CommandBuffer.DuplicateSet.Discard);

            return eb;
        }
    }
}