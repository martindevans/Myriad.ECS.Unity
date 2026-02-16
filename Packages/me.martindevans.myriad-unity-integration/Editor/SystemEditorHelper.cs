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
            // Try to get an editor for the type directly
            if (_editorTypes.TryGetValue(type, out o))
                return true;

            // If the editor is generic, try to get an editor for the generic type
            if (type.IsGenericType)
            {
                var openGeneric = type.GetGenericTypeDefinition();
                if (_editorTypes.TryGetValue(openGeneric, out o))
                    return true;
            }

            return false;
        }
    }
}