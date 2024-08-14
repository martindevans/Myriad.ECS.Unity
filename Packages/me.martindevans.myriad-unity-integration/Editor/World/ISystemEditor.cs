using System;
using Myriad.ECS.Systems;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MyriadSystemEditorAttribute
        : Attribute
    {
        public Type Type { get; }

        public MyriadSystemEditorAttribute(Type type)
        {
            Type = type;
        }
    }

    public interface IMyriadSystemEditor
    {
        void Draw<T>(ISystem<T> system);
    }
}
