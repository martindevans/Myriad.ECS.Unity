using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Packages.me.martindevans.myriad_unity_integration.Editor.World;

namespace Packages.me.martindevans.myriad_unity_integration.Editor
{
    internal static class SystemEditorHelper
    {
        private static readonly Dictionary<Type, Type> _editorTypes;

        static SystemEditorHelper()
        {
            _editorTypes = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                            from type in assembly.GetTypes()
                            where typeof(IMyriadSystemEditor).IsAssignableFrom(type)
                            let editor = type
                            let attr = editor.GetCustomAttribute<MyriadSystemEditorAttribute>()
                            where attr != null
                            let tgt = attr.Type
                            select (editor, tgt)).ToDictionary(x => x.tgt, x => x.editor);
        }

        public static bool TryGet(Type type, out Type o)
        {
            return _editorTypes.TryGetValue(type, out o);
        }
    }
}