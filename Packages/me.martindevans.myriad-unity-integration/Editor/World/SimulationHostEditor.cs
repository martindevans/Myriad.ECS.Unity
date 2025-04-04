using System.Collections.Generic;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Placeholder.Editor.UI.Editor;
using Placeholder.Editor.UI.Editor.Components;
using Placeholder.Editor.UI.Editor.Components.Section;
using Placeholder.Editor.UI.Editor.Components.Sections;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    public abstract class BaseWorldHostEditor<TSim>
        : BasePlaceholderEditor
        where TSim : BaseWorldHost
    {
        protected BaseWorldHostEditor(params IComponent[] components)
            : base(MakeArr(components))
        {
        }

        private static IComponent[] MakeArr(params IComponent[] components)
        {
            var list = new List<IComponent>();

            list.Add(new DefaultInspectorSection { Expanded = true });
            list.Add(new PlaymodeSection(new WorldStatsDisplay<TSim>()));
            list.AddRange(components);
            list.Add(new PlaymodeSection(new ArchetypeListDisplay<TSim>()));

            //wip:
            //new PlaymodeSection(new FoldoutSection(new GUIContent("Entity Query"), new EntityQueryComponent<TSim, TData>()))

            return list.ToArray();
        }
    }

    public abstract class BaseSimulationHostEditor<TSim, TData>
        : BaseWorldHostEditor<TSim>
        where TSim : BaseSimulationHost<TData>
    {
        protected BaseSimulationHostEditor()
            : base(
                new PlaymodeSection(new SystemListDisplay<TSim, TData>())
            )
        {
        }
    }
}
