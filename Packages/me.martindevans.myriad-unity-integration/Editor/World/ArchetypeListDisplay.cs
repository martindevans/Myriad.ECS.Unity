using System.Collections.Generic;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using UnityEditor;
using Myriad.ECS.Worlds.Archetypes;
using Placeholder.Editor.UI.Editor.Helpers;
using Placeholder.Editor.UI.Editor.Style;
using UnityEngine;
using IComponent = Placeholder.Editor.UI.Editor.Components.IComponent;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    public class ArchetypeListDisplay<TSim, TData>
        : IComponent
        where TSim : BaseSimulationHost<TData>
    {
        private readonly Dictionary<Archetype, ArchetypeDrawer> _drawers = new();

        private bool _expanded;
        private TSim _host;

        public void OnEnable(SerializedObject target)
        {
            _host = (TSim)target.targetObject;

            _drawers.Clear();
        }

        public void OnDisable()
        {
            _drawers.Clear();
        }

        public void Draw()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (_host != null && _host.Systems != null)
                DrawArchetypes(_host.World);
        }

        private void DrawArchetypes(Myriad.ECS.Worlds.World world)
        {
            using (new EditorGUILayout.VerticalScope(_expanded ? Styles.ContentOutline : GUIStyle.none))
            {
                _expanded = Header.Fold(new GUIContent($"Archetypes ({world.Archetypes.Count})"), _expanded);
                if (_expanded)
                {
                    using (new EditorGUILayout.VerticalScope(Styles.LeftPadding))
                    {
                        foreach (var archetype in world.Archetypes)
                        {
                            if (!_drawers.TryGetValue(archetype, out var drawer))
                            {
                                drawer = new ArchetypeDrawer(archetype);
                                _drawers[archetype] = drawer;
                            }

                            drawer.Draw();
                        }
                    }
                }
            }
        }

        public IEnumerable<SerializedProperty> GetChildProperties()
        {
            yield break;
        }

        public bool IsVisible => true;
        public bool RequiresConstantRepaint => true;
    }
}