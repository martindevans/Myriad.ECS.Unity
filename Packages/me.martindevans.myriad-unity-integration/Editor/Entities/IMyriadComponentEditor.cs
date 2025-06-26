using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Myriad.ECS;
using Myriad.ECS.IDs;
using Packages.me.martindevans.myriad_unity_integration.Editor.Extensions;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Components;
using Placeholder.Editor.UI.Editor.Helpers;
using Placeholder.Editor.UI.Editor.Style;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.Entities
{
    public static class MyriadComponentEditorHelper
    {
        private static readonly IReadOnlyDictionary<Type, Type> _editorTypes;

        static MyriadComponentEditorHelper()
        {
            _editorTypes = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                            from type in assembly.GetTypes()
                            where typeof(IMyriadComponentEditor).IsAssignableFrom(type)
                            let editor = type
                            let attr = editor.GetCustomAttribute<MyriadComponentEditorAttribute>()
                            where attr != null
                            let tgt = attr.Type
                            select (editor, tgt)).ToDictionary(x => x.tgt, x => x.editor);
        }

        [CanBeNull]
        public static IMyriadComponentEditor CreateEditorInstance(ComponentID id)
        {
            IMyriadComponentEditor editor;
            if (!_editorTypes.TryGetValue(id.Type, out var editorType))
                editor = DefaultComponentEditor.Create(id.Type);
            else
                editor = (IMyriadComponentEditor)Activator.CreateInstance(editorType);

            return editor;
        }

        [CanBeNull]
        public static IMyriadComponentEditor CreateEditorInstanceWithHeaderWrapper(ComponentID id)
        {
            var editor = CreateEditorInstance(id);
            return new ComponentEditorHeaderWrapper(editor, id);
        }
    }

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
        void Draw(MyriadEntity entity)
        {
            Draw(entity.Entity!.Value);
        }

        void Draw(Entity entity);
    }

    internal interface IMyriadEmptyComponentEditor
        : IMyriadComponentEditor
    {
        public bool IsEmpty { get; }
    }

    public abstract class BaseMyriadEmptyComponentEditor
        : IMyriadComponentEditor
    {
        public void Draw(Entity entity)
        {
        }
    }

    public static class DefaultComponentEditor
    {
        public static IMyriadComponentEditor Create(Type type)
        {
            var t = typeof(DefaultComponentEditor<>).MakeGenericType(type);
            return (IMyriadComponentEditor)Activator.CreateInstance(t, null);
        }
    }

    public class DefaultComponentEditor<TComponent>
        : IMyriadEmptyComponentEditor
        where TComponent : IComponent
    {
        private static readonly FieldInfo[] _fields;
        private static readonly PropertyInfo[] _properties;
        public bool IsEmpty => _fields.Length == 0 && _properties.Length == 0;

        static DefaultComponentEditor()
        {
            var tc = typeof(TComponent);
            _fields = tc.GetFields(BindingFlags.Instance | BindingFlags.Public);
            _properties = tc.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            if (typeof(MonoBehaviour).IsAssignableFrom(tc))
            {
                _fields = (from field in _fields
                           where field.DeclaringType != typeof(MonoBehaviour) && field.DeclaringType != typeof(Behaviour) && field.DeclaringType != typeof(Component) && field.DeclaringType != typeof(Object)
                           select field).ToArray();

                _properties = (from property in _properties
                               where property.DeclaringType != typeof(MonoBehaviour) && property.DeclaringType != typeof(Behaviour) && property.DeclaringType != typeof(Component) && property.DeclaringType != typeof(Object)
                               select property).ToArray();
            }
        }

        public void Draw(Entity entity)
        {
            var c = entity.GetComponentRef<TComponent>();
            var cBox = (object)c;

            foreach (var fieldInfo in _fields)
            {
                var v = fieldInfo.GetValue(cBox);
                EditorGUILayout.LabelField(fieldInfo.Name, v?.ToString() ?? "null");
            }

            foreach (var propInfo in _properties)
            {
                var v = propInfo.GetValue(cBox);
                EditorGUILayout.LabelField(propInfo.Name, v?.ToString() ?? "null");
            }
        }
    }

    public class ComponentEditorHeaderWrapper
        : IMyriadComponentEditor
    {
        private readonly IMyriadComponentEditor _inner;

        private readonly string _name;
        private readonly bool _empty;

        private bool _expanded = true;

        public ComponentEditorHeaderWrapper(IMyriadComponentEditor inner, ComponentID id)
        {
            _inner = inner;

            _name = id.Type.GetFormattedName();
            if (id.IsPhantomComponent)
                _name += " (PHANTOM)";
            if (id.IsDisposableComponent)
                _name += " (DISPOSE)";

            _empty = inner is IMyriadEmptyComponentEditor { IsEmpty: true };
        }

        public void Draw(Entity entity)
        {
            if (_empty)
            {
                Header.Simple(new GUIContent(_name));
            }
            else
            {
                using (new EditorGUILayout.VerticalScope(_expanded ? Styles.ContentOutline : GUIStyle.none))
                {
                    _expanded = Header.Fold(new GUIContent(_name), _expanded);
                    if (_expanded)
                        using (new EditorGUILayout.VerticalScope(Styles.LeftPadding))
                            _inner.Draw(entity);
                }
            }
        }
    }
}
