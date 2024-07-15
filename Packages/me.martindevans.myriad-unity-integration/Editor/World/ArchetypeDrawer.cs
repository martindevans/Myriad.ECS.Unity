using System;
using System.Collections.Generic;
using Myriad.ECS.Worlds.Archetypes;
using Placeholder.Editor.UI.Editor.Helpers;
using Placeholder.Editor.UI.Editor.Style;
using UnityEditor;
using UnityEngine;
using Packages.me.martindevans.myriad_unity_integration.Editor.Entities;
using Packages.me.martindevans.myriad_unity_integration.Editor.Extensions;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    public class ArchetypeDrawer
    {
        private const int DRAW_COUNT = 10;

        private readonly Archetype _archetype;

        private bool _expanded;

        private bool _viewingEntities;

        private List<EntityDrawer> _entitiesDrawers = new List<EntityDrawer>();
        private int _startIndex = -1;

        public ArchetypeDrawer(Archetype archetype)
        {
            _archetype = archetype;
        }

        public void Draw()
        {
            using (new EditorGUILayout.VerticalScope(_expanded ? Styles.ContentOutline : GUIStyle.none))
            {
                var title = new GUIContent($"{unchecked((uint)_archetype.GetHashCode())} ({_archetype.EntityCount} entities)")
                {
                    image = _archetype.IsPhantom ? EditorGUIUtility.IconContent("BuildSettings.Lumin On@2x").image : null,
                };

                _expanded = Header.Fold(title, _expanded);
                if (_expanded)
                {
                    using (new EditorGUILayout.VerticalScope(Styles.LeftPadding))
                    {
                        DrawEntities(_archetype);

                        using (new EditorGUILayout.VerticalScope(Styles.ContentOutline))
                        {
                            foreach (var component in _archetype.Components)
                            {
                                var txt = component.Type.GetFormattedName();
                                if (component.IsPhantomComponent)
                                    txt += " (PHANTOM)";

                                EditorGUILayout.LabelField(txt);
                            }
                        }
                    }
                }
            }
        }

        private void DrawEntities(Archetype archetype)
        {
            _viewingEntities = Header.Fold(new GUIContent($"Entities ({archetype.EntityCount})"), _viewingEntities);
            if (!_viewingEntities)
                return;

            if (_startIndex < 0)
                MoveEntityIndex(0);

            using (new EditorGUILayout.VerticalScope(Styles.ContentOutline))
            {
                using (new EditorGUILayout.HorizontalScope(Styles.ContentOutline))
                {
                    if (GUILayout.Button("<- Prev"))
                        MoveEntityIndex(_startIndex - 10);

                    if (GUILayout.Button("Refresh"))
                        MoveEntityIndex(_startIndex);

                    if (GUILayout.Button("Next ->"))
                        MoveEntityIndex(_startIndex + 10);
                }

                GUILayout.Label($"{_startIndex} to {_startIndex + _entitiesDrawers.Count}");

                GUILayout.Space(4);

                using (new EditorGUILayout.VerticalScope(Styles.LeftPadding))
                {
                    foreach (var entitiesDrawer in _entitiesDrawers)
                    {
                        using (new EditorGUILayout.VerticalScope(Styles.ContentOutline))
                        {
                            Header.Simple(new GUIContent($"{entitiesDrawer.Entity.UniqueID()}"));
                            using (new EditorGUILayout.VerticalScope(Styles.LeftPadding))
                                entitiesDrawer.Draw();
                        }
                    }
                }
            }
        }

        private void MoveEntityIndex(int startIndex)
        {
            _startIndex = startIndex;
            _startIndex = Math.Max(_startIndex, 0);
            _startIndex = Math.Min(_startIndex, _archetype.EntityCount - 1);

            _entitiesDrawers.Clear();

            var idx = 0;
            foreach (var entity in _archetype.Entities)
            {
                if (idx >= _startIndex)
                    _entitiesDrawers.Add(new EntityDrawer(_archetype.World, entity));
                idx++;

                if (_entitiesDrawers.Count > DRAW_COUNT)
                    break;
            }
        }
    }
}