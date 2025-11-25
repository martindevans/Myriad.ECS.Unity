using Myriad.ECS.Systems;
using Myriad.ECS.Worlds;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    /// <summary>
    /// Automatically pumps a simulation every frame
    /// </summary>
    public class GameTimeSimulationHost
        : BaseSimulationHost<GameTime>
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

        public override double CurrentTime => _time.Time;
        public override ulong CurrentFrame => _time.Frame;

        protected virtual WorldBuilder GetBuilder()
        {
            return new WorldBuilder()
               .WithSafetySystem(new UnityMyriadSafetySystemAdapter());
        }

        protected override GameTime GetData()
        {
            _time.Tick(Time.deltaTime * TimeSpeed);
            return _time;
        }
    }
}
