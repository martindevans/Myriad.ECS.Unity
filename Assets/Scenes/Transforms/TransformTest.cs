using System.Collections.Generic;
using Myriad.ECS.Command;
using Myriad.ECS.Components;
using Myriad.ECS.Systems;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Components;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Extensions;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Systems;
using UnityEngine;

namespace Assets.Scenes.Transforms
{
    public class TransformTest
        : MonoBehaviour
    {
        public BaseWorldHost Sim;
        public Transform Root;

        public int Count;

        public void OnEnable()
        {
            // Create Myriad entities, mirror Unity hierarchy
            var cmd = new CommandBuffer(Sim.World);
            var map = new Dictionary<Transform, CommandBuffer.BufferedEntity>();

            var todo = new Queue<Transform>();
            todo.Enqueue(Root);
            while (todo.TryDequeue(out var item))
            {
                Count++;

                var be = cmd.Create();
                map[item] = be;

                be
                   .SetupGameObjectBinding(item, DestructMode.Both)
                   .Set(new MyriadTransformSystem<GameTime>.LocalTransformMatrix())
                   .Set(new MyriadTransformSystem<GameTime>.WorldTransformMatrix());

                if (item.parent)
                {
                    var p = map[item.parent];
                    be.Set(new TransformParent(), p);
                }

                for (var i = 0; i < item.childCount; i++)
                    todo.Enqueue(item.GetChild(i));
            }

            cmd.Playback().Dispose();
        }

        public void Update()
        {
            // Copy from Unity local transforms into Myriad local transforms
            foreach (var (_, m, l) in Sim.World.Query<MyriadEntity, MyriadTransformSystem<GameTime>.LocalTransformMatrix>())
            {
                var t = m.Ref.transform;
                var localMatrix = Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
                l.Ref.Transform = new MyriadTransformSystem<GameTime>.TransformMatrix { Matrix = localMatrix };
            }
        }

        public void OnDrawGizmos()
        {
            // Draw cubes according to Myriad world transforms
            foreach (var (_, w) in Sim.World.Query<MyriadTransformSystem<GameTime>.WorldTransformMatrix>())
            {
                Gizmos.matrix = w.Ref.Transform.Matrix;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 1.05f);
            }
        }
    }
}
