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

        public double TimeSpeed = 1.0;

        protected virtual WorldBuilder GetBuilder()
        {
            return new WorldBuilder();
        }

        protected override GameTime GetData()
        {
            _time.TimeSpeed = TimeSpeed;
            _time.Time += Time.deltaTime * TimeSpeed;
            _time.DeltaTime = Time.deltaTime * TimeSpeed;
            _time.Frame++;

            return _time;
        }
    }
}
