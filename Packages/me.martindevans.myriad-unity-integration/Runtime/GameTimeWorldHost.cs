using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime
{
    public abstract class GameTimeWorldHost
        : WorldHost<GameTime>
    {
        private readonly GameTime _time = new();

        protected override GameTime GetData()
        {
            _time.Time = Time.timeAsDouble;
            _time.DeltaTime = Time.deltaTime;
            _time.Frame++;

            return _time;
        }
    }
}
