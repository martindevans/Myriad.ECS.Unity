using System.Collections.Generic;
using JetBrains.Annotations;
using Myriad.ECS;
using Myriad.ECS.Components;
using Myriad.ECS.IDs;
using Packages.me.martindevans.myriad_unity_integration.Editor.UIComponents;
using Packages.me.martindevans.myriad_unity_integration.Runtime.Components;
using Placeholder.Editor.UI.Editor;
using Placeholder.Editor.UI.Editor.Components;
using Placeholder.Editor.UI.Editor.Components.Section;
using Placeholder.Editor.UI.Editor.Components.Sections;
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
                var id = _entity.Entity.Value;

                var display = _entity.HasMyriadComponent<DebugDisplayName>()
                    ? $"{id} ({_entity.GetMyriadComponent<DebugDisplayName>().Name})"
                    : $"{id.UniqueID()} ({id.ToString()})";

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
        private readonly Dictionary<ComponentID, IMyriadComponentEditor> _editorInstances = new();

        public Entity Entity { get; }

        public EntityDrawer(Entity entity)
        {
            Entity = entity;
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
            var inst = GetEditorInstance(component)!;
            inst.Draw(Entity);
        }

        [CanBeNull]
        private IMyriadComponentEditor GetEditorInstance(ComponentID id)
        {
            if (_editorInstances.TryGetValue(id, out var editor))
                return editor;

            editor = MyriadComponentEditorHelper.CreateEditorInstanceWithHeaderWrapper(id);
            _editorInstances.Add(id, editor);

            return editor;
        }
    }
}
