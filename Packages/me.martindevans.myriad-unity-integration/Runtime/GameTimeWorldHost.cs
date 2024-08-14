using Myriad.ECS.Worlds;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    public class GameTimeWorldHost
        : WorldHost<GameTime>
    {
        private readonly GameTime _time = new();

        private World _world;
        public override World World
        {
            get
            {
                if (_world == null)
                    _world = GetBuilder().Build();
                return _world;
            }
        }

        protected virtual WorldBuilder GetBuilder()
        {
            return new WorldBuilder();
        }

        protected override GameTime GetData()
        {
            _time.Time = Time.timeAsDouble;
            _time.DeltaTime = Time.deltaTime;
            _time.Frame++;

            return _time;
        }
    }
}
