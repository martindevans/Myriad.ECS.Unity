using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Placeholder.Editor.UI.Editor;
using Placeholder.Editor.UI.Editor.Components.Section;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    public abstract class BaseSimulationHostEditor<TSim, TData>
        : BasePlaceholderEditor
        where TSim : BaseSimulationHost<TData>
    {
        protected BaseSimulationHostEditor()
            : base(
                new DefaultInspectorSection { Expanded = true },
                new SystemListDisplay<TSim, TData>(),
                new ArchetypeListDisplay<TSim, TData>()
            )
        {
        }
    }
}
