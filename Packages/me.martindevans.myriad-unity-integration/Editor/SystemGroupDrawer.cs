using System;
using Myriad.ECS.Systems;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Packages.me.martindevans.myriad_unity_integration.Editor.Extensions;
using Packages.me.martindevans.myriad_unity_integration.Runtime;
using Placeholder.Editor.UI.Editor.Helpers;
using Placeholder.Editor.UI.Editor.Style;
using JetBrains.Annotations;
using Packages.me.martindevans.myriad_unity_integration.Editor.World;
using System.Collections.Generic;

namespace Packages.me.martindevans.myriad_unity_integration.Editor
{
    public class SystemGroupDrawer<TData>
    {
        private readonly Dictionary<string, bool> _expandedGroups = new();
        private readonly Dictionary<ISystem<TData>, bool> _expandedSystems = new();

        private const float smoothing = 0.05f;
        private readonly Dictionary<string, float> _smoothedProgressIndicator = new();

        private readonly Dictionary<(string name, Type type), IMyriadSystemEditor> _editorInstances = new();

        public void DrawSystemGroup(ISystemGroup<TData> group, TimeSpan parentTime, TimeSpan highTimeThreshold)
        {
            if (group == null)
                return;

            var childGroups = Math.Max(1, group.Systems.Count(a => a.System is ISystemGroup<TData>));
            var childTimeThreshold = highTimeThreshold / childGroups;

            var expanded = _expandedGroups.GetValueOrDefault(group.Name, true);
            var micros = group.TotalExecutionTime.Ticks / (double)TimeSpan.TicksPerMillisecond * 1000;

            var opts = GetHeaderOptions(group.Name, group.TotalExecutionTime, parentTime, 0, (float)highTimeThreshold.TotalMilliseconds / 2f, (float)highTimeThreshold.TotalMilliseconds);

            using (new EditorGUILayout.VerticalScope(expanded ? Styles.ContentOutline : GUIStyle.none))
            {
                expanded = Header.Fold(new GUIContent($"{group.Name} : {micros:F0}us"), expanded, opts);
                if (expanded)
                {
                    using (new EditorGUILayout.VerticalScope(Styles.LeftPadding))
                    {
                        switch (group)
                        {
                            case PhasedParallelSystemGroup<TData> phased:
                                EditorGUILayout.LabelField($"Parallel System Phases: {phased.Phases}");
                                break;
                            case OrderedParallelSystemGroup<TData> ordered:
                                EditorGUILayout.LabelField($"Parallel System Dependency Depth: {ordered.MaxDependencyChain}");
                                break;
                        }

                        foreach (var item in group.Systems)
                        {
                            if (item.System is ISystemGroup<TData> sg)
                            {
                                DrawSystemGroup(sg, group.TotalExecutionTime, childTimeThreshold);
                            }
                            else
                            {
                                DrawSystem(item, group.TotalExecutionTime);
                            }
                        }
                    }
                }
            }

            _expandedGroups[group.Name] = expanded;
        }

        private void DrawSystem(SystemGroupItem<TData> item, TimeSpan groupTime)
        {
            var name = item.Type.GetFormattedName();

            var expanded = _expandedSystems.GetValueOrDefault(item.System, false);
            var enabled = item.Enabled;

            var microsPre = item.BeforeUpdateTime.Ticks / (double)TimeSpan.TicksPerMillisecond * 1000;
            var micros = item.UpdateTime.Ticks / (double)TimeSpan.TicksPerMillisecond * 1000;
            var microsPost = item.AfterUpdateTime.Ticks / (double)TimeSpan.TicksPerMillisecond * 1000;

            var timeThisFrame = item.BeforeUpdateTime + item.UpdateTime + item.AfterUpdateTime;
            var opts = GetHeaderOptions(name, timeThisFrame, groupTime);

            using (new EditorGUILayout.VerticalScope(expanded ? Styles.ContentOutline : GUIStyle.none))
            {
                Header.Toggle(new GUIContent($"{name} ({microsPre:F0}us, {micros:F0}us, {microsPost:F0}us)"), ref enabled, ref expanded, ToggleType.Tick, opts);
                if (expanded)
                {
                    using (new EditorGUILayout.VerticalScope(Styles.LeftPadding))
                    {
                        if (item.HasBeforeUpdate)
                            EditorGUILayout.LabelField($"Before Update: {microsPre:F0}us");
                        EditorGUILayout.LabelField($"Update:        {micros:F0}us");
                        if (item.HasAfterUpdate)
                            EditorGUILayout.LabelField($"After Update:  {microsPost:F0}us");

                        if (item.System is ISystemQueryEntityCount sqec)
                            EditorGUILayout.LabelField($"Queried Entities:  {sqec.QueryEntityCount}");

                        var editor = GetEditorInstance(name, item.Type);
                        editor?.Draw(item.System);
                    }
                }
            }

            _expandedSystems[item.System] = expanded;
            item.Enabled = enabled;
        }

        private Header.HeaderOptions GetHeaderOptions(string name, TimeSpan itemTime, TimeSpan parentTime, float lowTime = 0, float midTime = 1, float highTime = 2)
        {
            // Calculate progress as time of this item versus time of parent. Smooth it out
            // with the progress value from last frame.
            var timeThisFrame = (float)itemTime.TotalMilliseconds;
            var progressThisFrame = timeThisFrame / (float)(parentTime.TotalMilliseconds + 0.001);
            var prevProgress = _smoothedProgressIndicator.GetValueOrDefault(name, progressThisFrame);
            var progress = Mathf.Clamp01(Mathf.Lerp(prevProgress, progressThisFrame, smoothing));
            _smoothedProgressIndicator[name] = progress;

            // < low time == green
            // low -> mid == green to yellow
            // mid -> high == yellow to red
            // > high == red
            var c = Color.green;
            if (timeThisFrame > lowTime && timeThisFrame < midTime)
                c = Color.Lerp(Color.green, Color.yellow, (timeThisFrame - lowTime) / (midTime - lowTime));
            if (timeThisFrame > midTime)
                c = Color.Lerp(Color.yellow, Color.red, (timeThisFrame - midTime) / (highTime - midTime));

            return new Header.HeaderOptions
            {
                Progress = progress,
                ProgressColor = c,
            };
        }

        [CanBeNull]
        private IMyriadSystemEditor GetEditorInstance(string name, Type type)
        {
            if (_editorInstances.TryGetValue((name, type), out var editor))
                return editor;

            if (!SystemEditorHelper.TryGet(type, out var editorType))
                return null;

            editor = (IMyriadSystemEditor)Activator.CreateInstance(editorType);
            _editorInstances.Add((name, type), editor);
            return editor;
        }

        public void Clear()
        {
            _editorInstances.Clear();
            _smoothedProgressIndicator.Clear();
            _expandedGroups.Clear();
            _expandedSystems.Clear();
        }
    }
}