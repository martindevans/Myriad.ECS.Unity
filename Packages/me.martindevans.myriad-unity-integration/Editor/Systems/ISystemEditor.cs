using System;
using Myriad.ECS.Systems;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.Systems
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SystemEditorAttribute
        : Attribute
    {
        public Type Type { get; }

        public SystemEditorAttribute(Type type)
        {
            Type = type;
        }
    }

    public interface ISystemEditor
    {
        void Draw<T>(ISystem<T> system);
    }
}
