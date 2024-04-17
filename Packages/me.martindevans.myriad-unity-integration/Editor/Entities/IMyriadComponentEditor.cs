using System;
using Packages.me.martindevans.myriad_unity_integration.Runtime;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.Entities
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MyriadComponentEditorAttribute
        : Attribute
    {
        public Type Type { get; }

        public MyriadComponentEditorAttribute(Type type)
        {
            Type = type;
        }
    }

    public interface IMyriadComponentEditor
    {
        void Draw(MyriadEntity entity);
    }
}
