using System;
using System.Collections.Generic;
using System.Linq;
using Myriad.ECS.Queries;
using Packages.me.martindevans.myriad_unity_integration.Editor.Extensions;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Placeholder.Editor.UI.Editor;
using Placeholder.Editor.UI.Editor.Components;
using UnityEditor;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.World
{
    public class EntityQueryComponent<TSim, TData>
        : IComponent
        where TSim : BaseSimulationHost<TData>
    {
        private TSim _host;

        private QueryBuilder _builder = new();

        private TypeColumn _included;
        private TypeColumn _excluded;

        private IReadOnlyList<Type> _componentTypes;

        public void OnEnable(SerializedObject target)
        {
            _host = (TSim)target.targetObject;
            _builder = new QueryBuilder();

            _componentTypes = typeof(Myriad.ECS.IComponent).GetTypesAssignableTo().ToArray();
            _included = new("Include", _componentTypes, (b, id) =>
            {
                b.Include(id);
                RecreateLists();
            });
            _excluded = new("Exclude", _componentTypes, (b, id) =>
            {
                b.Exclude(id);
                RecreateLists();
            });

            _included.Builder = _builder;
            _excluded.Builder = _builder;
        }

        public void OnDisable()
        {
        }

        private void RecreateLists()
        {
            _included.RebuildMenu();
            _excluded.RebuildMenu();
        }

        public void Draw()
        {
            if (GUILayout.Button("Clear"))
            {
                _builder = new();
                _included.Builder = _builder;
                _excluded.Builder = _builder;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _included.DrawColumn();
                _excluded.DrawColumn();
            }

            //if (EditorGUILayout.DropdownButton(new GUIContent("types"), FocusType.Passive))
            //{
            //    GenericMenu menu = new GenericMenu();
            //    menu.AddItem(new GUIContent("Item 1"), false, handleItemClicked, "Item 1");
            //    menu.AddItem(new GUIContent("Item 2"), false, handleItemClicked, "Item 2");
            //    menu.AddItem(new GUIContent("Item 3"), false, handleItemClicked, "Item 3");
            //    menu.ShowAsContext();
            //}

            //EditorGUILayout.Popup(new GUIContent("types"), 0, new [] { "a", "b", "c" });

            ////using (new EditorGUILayout.VerticalScope(Styles.LeftPadding))
            //{
            //    EditorGUILayout.LabelField("Hello");
            //}

            //void handleItemClicked(object p)
            //{
            //    Debug.Log(p);
            //}
        }

        private void DrawColumn(string title, List<Type> types, GenericMenu menu)
        {
            if (EditorGUILayout.DropdownButton(new GUIContent(title), FocusType.Passive))
                menu.ShowAsContext();

            foreach (var type in types)
                EditorGUILayout.LabelField(type.GetFormattedName());
        }

        public IEnumerable<SerializedProperty> GetChildProperties()
        {
            yield break;
        }

        public bool IsVisible => true;
        public bool RequiresConstantRepaint => true;
        public BasePlaceholderEditor Editor { get; set; }

        private class TypeColumn
        {
            private QueryBuilder _builder;
            public QueryBuilder Builder
            {
                get => _builder;
                set
                {
                    _builder = value;
                    _addedTypes.Clear();
                    RebuildMenu();
                }
            }

            private readonly string _title;
            private readonly IReadOnlyList<Type> _allTypes;
            private readonly Action<QueryBuilder, Type> _add;

            private readonly HashSet<Type> _addedTypes = new();
            private GenericMenu _menu = new();
            
            public TypeColumn(string title, IReadOnlyList<Type> allTypes, Action<QueryBuilder, Type> add)
            {
                _title = title;
                _allTypes = allTypes;
                _add = add;
            }

            public void RebuildMenu()
            {
                _menu = new GenericMenu();
                foreach (var type in _allTypes)
                {
                    if (_builder.IsIncluded(type))
                        continue;
                    if (_builder.IsExcluded(type))
                        continue;
                    if (_builder.IsExactlyOneOf(type))
                        continue;
                    if (_builder.IsAtLeastOneOf(type))
                        continue;

                    _menu.AddItem(new GUIContent(type.GetFormattedName()), false, () =>
                    {
                        _add(Builder, type);
                        _addedTypes.Add(type);
                    });
                }
            }

            public void DrawColumn()
            {
                if (EditorGUILayout.DropdownButton(new GUIContent(_title), FocusType.Passive))
                    _menu.ShowAsContext();

                foreach (var type in _addedTypes)
                    EditorGUILayout.LabelField(type.GetFormattedName());
            }
        }
    }
}