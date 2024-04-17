using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Myriad.ECS.IDs;
using Packages.me.martindevans.myriad_unity_integration.Editor.UIComponents;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Placeholder.Editor.UI.Editor;
using Placeholder.Editor.UI.Editor.Components;
using Placeholder.Editor.UI.Editor.Components.Sections;
using Placeholder.Editor.UI.Editor.Helpers;
using Placeholder.Editor.UI.Editor.Style;
using UnityEditor;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.Entities
{
    [CustomEditor(typeof(MyriadEntity))]
    public class MyriadEntityEditor
        : BasePlaceholderEditor
    {
        public MyriadEntityEditor()
            : base(
                new BasicSection(
                    new GUIContent("Myriad Entity"),
                    new FieldValueLabel<MyriadEntity>("ID", m => m.Entity.UniqueID().ToString()),
                    new FieldValueLabel<MyriadEntity>("Exists", m => m.Entity.Exists(m.World).ToString()),
                    new FieldValueLabel<MyriadEntity>("Phantom", m => m.Entity.IsPhantom(m.World).ToString())
                ),
                new ComponentListDisplay()
            )
        {
        }
    }

    public class ComponentListDisplay
        : IComponent
    {
        private MyriadEntity _entity;

        private readonly Dictionary<ComponentID, bool> _expandedComponents = new();

        private IReadOnlyDictionary<Type, Type> _editorTypes = new Dictionary<Type, Type>();
        private readonly Dictionary<ComponentID, IMyriadComponentEditor> _editorInstances = new();

        public void OnEnable(SerializedObject target)
        {
            _editorTypes = (from editor in Assembly.GetExecutingAssembly().GetTypes()
                            where typeof(IMyriadComponentEditor).IsAssignableFrom(editor)
                            let attr = editor.GetCustomAttribute<MyriadComponentEditorAttribute>()
                            where attr != null
                            let tgt = attr.Type
                            select (editor, tgt)).ToDictionary(x => x.tgt, x => x.editor);

            _entity = (MyriadEntity)target.targetObject;
        }

        public void OnDisable()
        {
        }

        public void Draw()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (_entity != null)
            {
                var components = _entity.Entity.GetComponents(_entity.World);
                foreach (var component in components)
                {
                    DrawComponent(component);
                }
            }
        }

        private void DrawComponent(ComponentID component)
        {
            var name = component.Type.Name;
            if (component.IsPhantomComponent)
                name += " (PHANTOM)";

            var instance = GetEditorInstance(component);

            if (instance == null)
            {
                Header.Simple(new GUIContent(name));
            }
            else
            {
                var expanded = _expandedComponents.GetValueOrDefault(component, true);
                using (new EditorGUILayout.VerticalScope(expanded ? Styles.ContentOutline : GUIStyle.none))
                {
                    expanded = Header.Fold(new GUIContent(name), expanded);
                    if (expanded)
                    {
                        using (new EditorGUILayout.VerticalScope(Styles.LeftPadding))
                        {
                            instance.Draw(_entity);
                        }
                    }
                }
                _expandedComponents[component] = expanded;
            }
        }

        [CanBeNull]
        private IMyriadComponentEditor GetEditorInstance(ComponentID id)
        {
            if (_editorInstances.TryGetValue(id, out var editor))
                return editor;

            if (!_editorTypes.TryGetValue(id.Type, out var editorType))
                return null;

            editor = (IMyriadComponentEditor)Activator.CreateInstance(editorType);
            _editorInstances.Add(id, editor);
            return editor;
        }

        public IEnumerable<SerializedProperty> GetChildProperties()
        {
            yield break;
        }

        public bool IsVisible => true;
        public bool RequiresConstantRepaint => true;
    }
}
