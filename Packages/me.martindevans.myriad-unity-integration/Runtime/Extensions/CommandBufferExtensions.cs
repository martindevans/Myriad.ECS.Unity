using Myriad.ECS.Command;
using Myriad.ECS.Components;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Components;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Extensions
{
    public static class CommandBufferExtensions
    {
        public static CommandBuffer.BufferedEntity SetupGameObjectBinding(this CommandBuffer.BufferedEntity entity, GameObject go, bool? autoDestructGameObject = default, bool autoDestructEntity = false)
        {
            var me = go.GetOrAddComponent<MyriadEntity>();
            if (autoDestructGameObject.HasValue)
                me.AutoDestructGameObject = autoDestructGameObject.Value;

            var eb = entity
                .Set(me)
                .Set(new DebugDisplayName(go.name), CommandBuffer.DuplicateSet.Discard);

            if (autoDestructEntity)
                eb.Set(new AutoDestructEntityFromGameObject());

            return eb;
        }
    }
}