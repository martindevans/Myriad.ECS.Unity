using Myriad.ECS;

namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Components
{
    /// <summary>
    /// Entities tagged with this will be destroyed when their bound gameobject is destroyed
    /// </summary>
    internal struct AutoDestructEntityFromGameObject
        : IComponent
    {
    }
}