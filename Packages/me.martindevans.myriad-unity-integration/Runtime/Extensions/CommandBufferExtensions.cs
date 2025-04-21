using System.Collections.Generic;
using Myriad.ECS.Command;
using Myriad.ECS.Components;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Components;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Extensions
{
    public static class CommandBufferExtensions
    {
        private static readonly List<IBehaviourComponent> _temp = new();

        public static CommandBuffer.BufferedEntity SetupGameObjectBinding(this CommandBuffer.BufferedEntity entity, Transform tr, DestructMode destruct)
        {
            return SetupGameObjectBinding(entity, tr.gameObject, destruct);
        }

        public static CommandBuffer.BufferedEntity SetupGameObjectBinding(this CommandBuffer.BufferedEntity entity, GameObject go, DestructMode destruct)
        {
            var me = go.GetOrAddComponent<MyriadEntity>();
            me._destructMode = destruct;

            _temp.Clear();
            go.GetComponentsInChildren(true, _temp);
            foreach (var item in _temp)
                item.EarlyBind(entity);
            _temp.Clear();

            return entity
                .Set(me, entity)
                .Set(new DebugDisplayName(go.name), CommandBuffer.DuplicateSet.Discard);
        }
    }
}