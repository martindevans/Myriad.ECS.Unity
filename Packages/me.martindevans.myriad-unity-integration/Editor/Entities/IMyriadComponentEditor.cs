using System;
using System.Linq;
using System.Reflection;
using Myriad.ECS;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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
}
