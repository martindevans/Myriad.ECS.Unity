using Myriad.ECS.Command;
using Myriad.ECS.Components;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Extensions
{
    public static class CommandBufferExtensions
    {
        public static CommandBuffer.BufferedEntity SetupGameObjectBinding(this CommandBuffer.BufferedEntity entity, GameObject go)
        {
            return entity
                .Set(go.GetOrAddComponent<MyriadEntity>())
                .Set(new DebugDisplayName(go.name), CommandBuffer.DuplicateSet.Discard);
        }
    }
}