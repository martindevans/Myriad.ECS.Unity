using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Placeholder.Editor.UI.Editor;
using Placeholder.Editor.UI.Editor.Components.Section;
using Placeholder.Editor.UI.Editor.Components.Sections;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    public abstract class BaseSimulationHostEditor<TSim, TData>
        : BasePlaceholderEditor
        where TSim : BaseSimulationHost<TData>
    {
        protected BaseSimulationHostEditor()
            : base(
                new DefaultInspectorSection { Expanded = true },
                new PlaymodeSection(new WorldStatsDisplay<TSim, TData>()),
                new PlaymodeSection(new SystemListDisplay<TSim, TData>()),
                new PlaymodeSection(new ArchetypeListDisplay<TSim, TData>())

                //wip:
                //new PlaymodeSection(new FoldoutSection(new GUIContent("Entity Query"), new EntityQueryComponent<TSim, TData>()))
            )
        {
        }
    }
}
