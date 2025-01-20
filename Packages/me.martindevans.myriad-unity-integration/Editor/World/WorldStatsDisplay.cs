using System.Collections.Generic;
using System.Linq;
using Myriad.ECS.Components;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Placeholder.Editor.UI.Editor;
using Placeholder.Editor.UI.Editor.Components;
using Placeholder.Editor.UI.Editor.Helpers;
using Placeholder.Editor.UI.Editor.Style;
using UnityEditor;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    public class WorldStatsDisplay<TSim, TData>
        : IComponent
        where TSim : BaseSimulationHost<TData>
    {
        private TSim _host;
        private bool _expanded = true;

        public void OnEnable(SerializedObject target)
        {
            _host = (TSim)target.targetObject;
        }

        public void OnDisable()
        {
            _host = null;
        }

        public void Draw()
        {
            var world = _host.World;

            using (new EditorGUILayout.VerticalScope(_expanded ? Styles.ContentOutline : GUIStyle.none))
            {
                _expanded = Header.Fold(new GUIContent($"Statistics"), _expanded);
                if (_expanded)
                {
                    using (new EditorGUILayout.VerticalScope(Styles.LeftPadding))
                    {
                        EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);
                        using (new EditorGUI.IndentLevelScope())
                        {
                            var entities = _host.World.Count();
                            var phantoms = _host.World.Count<Phantom>();

                            EditorGUILayout.LabelField("Total", entities.ToString());
                            EditorGUILayout.LabelField("Live", (entities - phantoms).ToString());
                            EditorGUILayout.LabelField("Phantom", phantoms.ToString());
                        }

                        EditorGUILayout.LabelField("Archetypes", EditorStyles.boldLabel);
                        using (new EditorGUI.IndentLevelScope())
                        {
                            var archetypes = world.Archetypes.Count;
                            var empty = world.Archetypes.Count(a => a.EntityCount == 0);

                            EditorGUILayout.LabelField("Total", archetypes.ToString());
                            EditorGUILayout.LabelField("Occupied", (archetypes - empty).ToString());
                            EditorGUILayout.LabelField("Empty", empty.ToString());
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
        public BasePlaceholderEditor Editor { get; set; }
    }
}