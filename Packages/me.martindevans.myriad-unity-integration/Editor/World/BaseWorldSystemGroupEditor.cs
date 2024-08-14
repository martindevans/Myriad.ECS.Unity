using System;
using System.Collections.Generic;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Placeholder.Editor.UI.Editor;
using Placeholder.Editor.UI.Editor.Components;
using Placeholder.Editor.UI.Editor.Components.Section;
using Placeholder.Editor.UI.Editor.Components.Sections;
using UnityEditor;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    public class BaseWorldSystemGroupEditor<TData>
        : BasePlaceholderEditor
    {
        protected BaseWorldSystemGroupEditor()
            : base(
                new DefaultInspectorSection { Expanded = true },
                new PlaymodeSection(new WorldSystemGroupListDisplay<TData>())
            )
        {
        }
    }

    public class WorldSystemGroupListDisplay<TData>
        : IComponent
    {
        private WorldSystemGroup<TData> _provider;
        private readonly SystemGroupDrawer<TData> _groupDrawer = new();

        public void OnEnable(SerializedObject target)
        {
            _provider = (WorldSystemGroup<TData>)target.targetObject;
        }

        public void OnDisable()
        {
            _provider = null;
            _groupDrawer.Clear();
        }

        public void Draw()
        {
            var g = _provider?.Group;
            if (g != null)
                _groupDrawer.DrawSystemGroup(g, g.TotalExecutionTime, TimeSpan.FromMilliseconds(12));
        }

        public IEnumerable<SerializedProperty> GetChildProperties()
        {
            yield break;
        }

        public bool IsVisible => true;
        public bool RequiresConstantRepaint => true;
        public BasePlaceholderEditor Editor { get; set; }
    }
}