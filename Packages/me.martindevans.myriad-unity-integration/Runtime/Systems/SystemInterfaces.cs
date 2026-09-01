namespace Packages.me.martindevans.myriad_unity_integration.Runtime.Systems
{
    /// <summary>
    /// Systems implementing this interface will automatically display the entity count in the Unity inspector
    /// </summary>
    public interface ISystemQueryEntityCount
    {
        int QueryEntityCount { get; }
    }

    /// <summary>
    /// Systems implementing this interface will automatically display the chunk count in the Unity inspector
    /// </summary>
    public interface ISystemQueryChunkCount
    {
        int QueryChunkCount { get; }
    }

    /// <summary>
    /// Systems implementing this interface will automatically display the archetype count in the Unity inspector
    /// </summary>
    public interface ISystemQueryArchetypeCount
    {
        int QueryArchetypeCount { get; }
    }

    /// <summary>
    /// Systems implementing this interface will automatically display the job count in the Unity inspector
    /// </summary>
    public interface ISystemQueryScheduledJobCount
    {
        int QueryJobCount { get; }
    }
}
