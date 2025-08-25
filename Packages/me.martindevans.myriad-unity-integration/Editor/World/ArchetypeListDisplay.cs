using System;
using System.Collections.Generic;
using System.Linq;
using Myriad.ECS.IDs;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using UnityEditor;
using Myriad.ECS.Worlds.Archetypes;
using Placeholder.Editor.UI.Editor;
using Placeholder.Editor.UI.Editor.Helpers;
using Placeholder.Editor.UI.Editor.Style;
using UnityEngine;
using IComponent = Placeholder.Editor.UI.Editor.Components.IComponent;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    public class ArchetypeListDisplay<TSim>
        : IComponent
        where TSim : BaseWorldHost
    {
        private readonly Dictionary<Archetype, ArchetypeDrawer> _drawers = new();

        private bool _expanded;
        private TSim _host;

        private bool _showEmpty;
        private string _filter;

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
            if (_host)
                DrawArchetypes(_host.World);
        }

        private void DrawArchetypes(Myriad.ECS.Worlds.World world)
        {
            var emptyCount = world.Archetypes.Count(a => a.EntityCount == 0);

            using (new EditorGUILayout.VerticalScope(_expanded ? Styles.ContentOutline : GUIStyle.none))
            {
                _expanded = Header.Fold(new GUIContent($"Archetypes ({world.Archetypes.Count} total, {emptyCount} empty)"), _expanded);
                if (_expanded)
                {
                    using (new EditorGUILayout.VerticalScope(Styles.LeftPadding))
                    {
                        using (new EditorGUILayout.VerticalScope(Styles.ContentOutline))
                        {
                            _showEmpty = EditorGUILayout.Toggle("Show Empty", _showEmpty);
                            _filter = EditorGUILayout.TextField("Component Type Filter", _filter);
                        }

                        foreach (var archetype in world.Archetypes)
                        {
                            if (!_showEmpty && archetype.EntityCount == 0)
                                continue;

                            var match = string.IsNullOrWhiteSpace(_filter);
                            foreach (var component in archetype.Components)
                            {
                                if (match)
                                    break;
                                match |= MatchesFilter(component, _filter);
                            }

                            if (match)
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
        }

        private static bool MatchesFilter(ComponentID component, string filter)
        {
            return (component.Type.FullName ?? component.Type.Name).Contains(filter, StringComparison.InvariantCultureIgnoreCase);
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