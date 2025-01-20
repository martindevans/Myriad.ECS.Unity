using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Myriad.ECS;
using Myriad.ECS.Components;
using Myriad.ECS.IDs;
using Packages.me.martindevans.myriad_unity_integration.Editor.Extensions;
using Packages.me.martindevans.myriad_unity_integration.Editor.UIComponents;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Placeholder.Editor.UI.Editor;
using Placeholder.Editor.UI.Editor.Components;
using Placeholder.Editor.UI.Editor.Components.Section;
using Placeholder.Editor.UI.Editor.Components.Sections;
using Placeholder.Editor.UI.Editor.Helpers;
using Placeholder.Editor.UI.Editor.Style;
using UnityEditor;
using UnityEngine;
using IComponent = Placeholder.Editor.UI.Editor.Components.IComponent;

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
                    new PlaymodeSwitchSection(
                        new ComponentList(
                            new DisplayId(),
                            new FieldValueLabel<MyriadEntity>("Exists", m => m.Entity?.Exists().ToString() ?? "no_binding"),
                            new FieldValueLabel<MyriadEntity>("Phantom", m => m.Entity?.IsPhantom().ToString() ?? "no_binding")
                        ),
                        new ComponentList(
                            new InfoBoxComponent("When this behaviour is attached to a Myriad Entity it can be used as a 'Binding' between the scene and the ECS", MessageType.Info)
                        )
                    )
                ),
                new DefaultInspectorSection { Expanded = true },
                new ComponentListDisplay()
            )
        {
        }
    }

    public class DisplayId
        : IComponent
    {
        private MyriadEntity _entity;

        public void OnEnable(SerializedObject target)
        {
            _entity = (MyriadEntity)target.targetObject;
        }

        public void OnDisable()
        {
        }

        public void Draw()
        {
            if (!_entity.Entity.HasValue)
            {
                EditorGUILayout.LabelField("ID", "Unknown/Unbound");
            }
            else
            {
                var id = _entity.Entity.Value.UniqueID().ToString();

                var display = id;
                if (_entity.HasMyriadComponent<DebugDisplayName>())
                    display = $"{id} ({_entity.GetMyriadComponent<DebugDisplayName>().Name})";

                EditorGUILayout.LabelField("ID", display);
            }
        }

        public IEnumerable<SerializedProperty> GetChildProperties()
        {
            yield break;
        }

        public bool IsVisible => true;
        public bool RequiresConstantRepaint => true;
        public BasePlaceholderEditor Editor { get; set; }
    }

    public class ComponentListDisplay
        : IComponent
    {
        private MyriadEntity _entity;
        private EntityDrawer _drawer;

        public void OnEnable(SerializedObject target)
        {
            _entity = (MyriadEntity)target.targetObject;
        }

        public void OnDisable()
        {
        }

        public void Draw()
        {
            if (_drawer == null)
                if (_entity.Entity.HasValue)
                    _drawer = new EntityDrawer(_entity.Entity.Value);

            _drawer?.Draw();
        }

        public IEnumerable<SerializedProperty> GetChildProperties()
        {
            yield break;
        }

        public bool IsVisible => true;
        public bool RequiresConstantRepaint => true;
        public BasePlaceholderEditor Editor { get; set; }
    }

    public class EntityDrawer
    {
        private readonly Dictionary<ComponentID, bool> _expandedComponents = new();

        private readonly IReadOnlyDictionary<Type, Type> _editorTypes;
        private readonly Dictionary<ComponentID, IMyriadComponentEditor> _editorInstances = new();

        public Entity Entity { get; }

        public EntityDrawer(Entity entity)
        {
            Entity = entity;

            _editorTypes = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                            from type in assembly.GetTypes()
                            where typeof(IMyriadComponentEditor).IsAssignableFrom(type)
                            let editor = type
                            let attr = editor.GetCustomAttribute<MyriadComponentEditorAttribute>()
                            where attr != null
                            let tgt = attr.Type
                            select (editor, tgt)).ToDictionary(x => x.tgt, x => x.editor);
        }

        public void Draw()
        {
            if (Entity.Exists())
            {
                var components = Entity.ComponentTypes;
                foreach (var component in components)
                {
                    DrawComponent(component);
                }
            }
        }

        private void DrawComponent(ComponentID component)
        {
            var name = component.Type.GetFormattedName();
            if (component.IsPhantomComponent)
                name += " (PHANTOM)";
            if (component.IsDisposableComponent)
                name += " (DISPOSE)";

            var instance = GetEditorInstance(component);
            var emptyable = instance as IMyriadEmptyComponentEditor;

            if (instance == null || emptyable is { IsEmpty: true })
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
                        using (new EditorGUILayout.VerticalScope(Styles.LeftPadding))
                            instance.Draw(Entity);
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
                editor = DefaultComponentEditor.Create(id.Type);
            else
                editor = (IMyriadComponentEditor)Activator.CreateInstance(editorType);

            _editorInstances.Add(id, editor);
            return editor;
        }
    }
}
