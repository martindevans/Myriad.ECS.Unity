using Placeholder.Editor.UI.Editor.Components;
using System.Collections.Generic;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using UnityEditor;
using System;
using Myriad.ECS.Systems;
using Myriad.ECS.Worlds.Archetypes;
using Placeholder.Editor.UI.Editor.Helpers;
using Placeholder.Editor.UI.Editor.Style;
using UnityEngine;
using Myriad.ECS.Worlds;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    public class ArchetypeListDisplay<TSim, TData>
        : IComponent
        where TSim : BaseSimulationHost<TData>
    {
        private Dictionary<Archetype, bool> _expandedArchetypes = new();

        private bool _expanded;
        private TSim _host;

        public void OnEnable(SerializedObject target)
        {
            _host = (TSim)target.targetObject;
            _expandedArchetypes.Clear();
        }

        public void OnDisable()
        {
        }

        public void Draw()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (_host != null && _host.Systems != null)
                DrawArchetypes(_host.World);;
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
                            DrawArchetype(archetype);
                        }
                    }
                }
            }
        }

        private void DrawArchetype(Archetype archetype)
        {
            var expanded = _expandedArchetypes.GetValueOrDefault(archetype, false);

            using (new EditorGUILayout.VerticalScope(expanded ? Styles.ContentOutline : GUIStyle.none))
            {
                var phantom = archetype.IsPhantom ? "(PHANTOM)" : "";
                expanded = Header.Fold(new GUIContent($"{archetype.GetHashCode()} ({archetype.EntityCount} entities) {phantom}"), expanded);
                if (expanded)
                {
                    using (new EditorGUILayout.VerticalScope(Styles.LeftPadding))
                    {
                        foreach (var component in archetype.Components)
                        {
                            var txt = component.Type.Name;
                            if (component.IsPhantomComponent)
                                txt += " (PHANTOM)";

                            EditorGUILayout.LabelField(txt);
                        }
                    }
                }
            }

            _expandedArchetypes[archetype] = expanded;
        }

        public IEnumerable<SerializedProperty> GetChildProperties()
        {
            yield break;
        }

        public bool IsVisible => true;
        public bool RequiresConstantRepaint => true;
    }
}