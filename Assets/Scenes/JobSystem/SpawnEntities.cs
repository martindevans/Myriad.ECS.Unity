using System;
using System.Collections;
using Myriad.ECS.Command;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using UnityEngine;

namespace Assets.Scenes.JobSystem
{
    public class SpawnEntities
        : MonoBehaviour
    {
        public int Count = 10000;
        public BaseWorldHost World;

        private void OnEnable()
        {
            StartCoroutine(CoSpawn());
        }

        private IEnumerator CoSpawn()
        {
            var cmd = new CommandBuffer(World.World);
            var rng = new System.Random();

            for (var i = 0; i < Count; i += 8)
            {
                yield return null;

                for (var j = 0; j < 8; j++)
                {
                    var eb = cmd.Create()
                                .Set(new DemoComponent())
                                .Set(new GenericDemoComponent<ulong>());

                    if (rng.NextDouble() < 0.2)
                        eb.Set(new GenericDemoComponent<int>());
                    if (rng.NextDouble() < 0.2)
                        eb.Set(new GenericDemoComponent<float>());
                    if (rng.NextDouble() < 0.2)
                        eb.Set(new GenericDemoComponent<long>());
                    if (rng.NextDouble() < 0.2)
                        eb.Set(new GenericDemoComponent<decimal>());
                    if (rng.NextDouble() < 0.2)
                        eb.Set(new GenericDemoComponent<double>());
                }

                cmd.Playback().Dispose();
            }
        }
    }
}
