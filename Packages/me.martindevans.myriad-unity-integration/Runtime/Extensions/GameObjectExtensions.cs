using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Extensions
{
    internal static class GameObjectExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject go)
            where T : MonoBehaviour
        {
            if (go.TryGetComponent<T>(out var t))
                return t;

            return go.AddComponent<T>();
        }
    }
}