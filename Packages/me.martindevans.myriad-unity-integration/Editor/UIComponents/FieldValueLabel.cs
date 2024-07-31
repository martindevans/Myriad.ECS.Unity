#nullable enable

using System;
using System.Collections.Generic;
using Placeholder.Editor.UI.Editor;
using Placeholder.Editor.UI.Editor.Components;
using UnityEditor;
using UnityEngine;

namespace Packages.me.martindevans.myriad_unity_integration.Editor.UIComponents
{
    public class FieldValueLabel<T>
        : IComponent
    {
        private readonly Func<T, string> _getValue;
        private readonly string _label;
        private T? _object;

        public bool IsVisible => true;
        public bool RequiresConstantRepaint => false;
        public BasePlaceholderEditor? Editor { get; set; }

        public FieldValueLabel(string label, Func<T, string> getValue)
        {
            _getValue = getValue;
            _label = label;
        }

        public void OnEnable(SerializedObject target)
        {
            _object = (T)(object)target.targetObject;
        }

        public void OnDisable()
        {
        }

        public void Draw()
        {
            if (_object == null)
            {
                var saved = GUI.color;
                GUI.color = Color.magenta;
                EditorGUILayout.LabelField("No target object");
                GUI.color = saved;
            }
            else
            {
                var v = _getValue(_object);
                EditorGUILayout.LabelField(_label, v);
            }
        }

        public IEnumerable<SerializedProperty> GetChildProperties()
        {
            yield break;
        }
    }
}