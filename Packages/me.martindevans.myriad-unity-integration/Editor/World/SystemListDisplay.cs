using Packages.me.martindevans.myriad_unity_integration.Runtime;
using System.Collections.Generic;
using System;
using Placeholder.Editor.UI.Editor;
using Placeholder.Editor.UI.Editor.Components;
using UnityEditor;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    public class SystemListDisplay<TSim, TData>
       : IComponent
       where TSim : BaseSimulationHost<TData>
    {
        private TSim _host;
        private readonly SystemGroupDrawer<TData> _groupDrawer = new();

        public void OnEnable(SerializedObject target)
        {
            _host = (TSim)target.targetObject;
        }

        public void OnDisable()
        {
            _host = null;
            _groupDrawer.Clear();
        }

        public void Draw()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (_host && _host.Systems != null)
                _groupDrawer.DrawSystemGroup(_host.Systems, TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(12), parentDisabled:false);
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