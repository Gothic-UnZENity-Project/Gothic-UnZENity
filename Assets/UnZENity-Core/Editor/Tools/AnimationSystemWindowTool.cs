using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core._Npc2;
using GUZ.Core.Animations;
using UnityEditor;
using UnityEngine;

namespace GUZ.Core.Editor.Tools
{
    public class AnimationSystemWindowTool : EditorWindow
    {
        private Dictionary<string, AnimationSystem> _animationSystems = new();
        private Vector2 _scrollPosition;

        private int _selectedAnimationSystemIndex;
        private AnimationSystem _targetAnimationSystem;

        // Add menu item to create the window
        [MenuItem("UnZENity/Animation Debug Window")]
        public static void ShowWindow()
        {
            GetWindow<AnimationSystemWindowTool>("Animation System Debug");
        }

        private void OnGUI()
        {
            DrawNpcSelection();

            if (_targetAnimationSystem == null)
            {
                EditorGUILayout.HelpBox("No AnimationSystem selected in the scene!", MessageType.Warning);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawAnimationInfo();
            DrawBoneStates();
            DrawTimelineView();
            DrawPlaybackControls();

            EditorGUILayout.EndScrollView();

            // Repaint the window every frame to update the animation state
            Repaint();
        }

        private void DrawNpcSelection()
        {
            EditorGUILayout.LabelField("1. Select NPC", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Re-Collect AnimationSystems
            if (GUILayout.Button("(Re)collect AnimationSystems"))
            {
                var emptyElement = new[] { new { name = "Empty", animComp = (AnimationSystem)null } };

                // Add additional empty element to the Dictionary
                _animationSystems = emptyElement
                    .Concat(FindObjectsOfType<AnimationSystem>()
                        .Select(animComp => new { animComp.GetComponentInParent<NpcLoader2>().name, animComp }))
                    .ToDictionary(i => i.name, i => i.animComp);

            }

            // NPC dropdown
            _selectedAnimationSystemIndex = EditorGUILayout.Popup("NPC", _selectedAnimationSystemIndex, _animationSystems.Keys.ToArray());

            if (_selectedAnimationSystemIndex >= _animationSystems.Count)
            {
                _targetAnimationSystem = null;
            }
            else
            {
                _targetAnimationSystem = _animationSystems.Values.ElementAt(_selectedAnimationSystemIndex);
            }
        }

        private void DrawAnimationInfo()
        {
            EditorGUILayout.LabelField("Currently Playing Animations", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Access the currently playing animations from selected AnimationSystem
            foreach (var trackInstance in _targetAnimationSystem.DebugTrackInstances)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Animation: {trackInstance.Track.Animation.Name}");
                EditorGUILayout.LabelField($"Time: {trackInstance.CurrentTime:F2} / {trackInstance.Track.Duration:F2} - {trackInstance.State}");
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawBoneStates()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Bone States", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // "" | "S_WALK" | "T_DIALOGGESTURE_00" | ...
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("-");
            foreach (var trackInstance in _targetAnimationSystem.DebugTrackInstances)
            {
                EditorGUILayout.LabelField(trackInstance.Track.Animation.Name);
                EditorGUILayout.LabelField("-");
            }
            EditorGUILayout.EndHorizontal();

            // "L_ARM" | BlendIn(0.32f) /---  / | Play(1.00f) /-----/ | ...
            foreach (var boneName in _targetAnimationSystem.DebugBoneNames)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(boneName);
                foreach (var trackInstance in _targetAnimationSystem.DebugTrackInstances)
                {
                    var boneIndex = Array.IndexOf(trackInstance.Track.BoneNames, boneName);
                    EditorGUILayout.LabelField($"{trackInstance.BoneStates[boneIndex]}({trackInstance.BoneBlendWeights[boneIndex]:F2})");
                    EditorGUILayout.LabelField("-");
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTimelineView()
        {
            // Calculate the visible time range
            float timelineWidth = position.width - 100; // Leave space for labels
            float timelineHeight = 20f;
            Rect timelineRect = GUILayoutUtility.GetRect(timelineWidth, timelineHeight);

            // Draw timeline background
            EditorGUI.DrawRect(timelineRect, new Color(0.2f, 0.2f, 0.2f));

            // Draw time markers
            float totalDuration = 5f; // Get this from your animation system
            for (float time = 0; time <= totalDuration; time += 0.5f)
            {
                float x = timelineRect.x + (time / totalDuration) * timelineRect.width;
                Rect markerRect = new Rect(x, timelineRect.y, 1, timelineRect.height);
                EditorGUI.DrawRect(markerRect, Color.gray);

                // Draw time labels
                Rect labelRect = new Rect(x - 15, timelineRect.y - 15, 30, 15);
                GUI.Label(labelRect, time.ToString("F1"));
            }
        }

        private void DrawPlaybackControls()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Play", GUILayout.Width(50)))
            {
                // Implement play functionality
            }

            if (GUILayout.Button("Pause", GUILayout.Width(50)))
            {
                // Implement pause functionality
            }

            if (GUILayout.Button("Stop", GUILayout.Width(50)))
            {
                // Implement stop functionality
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
