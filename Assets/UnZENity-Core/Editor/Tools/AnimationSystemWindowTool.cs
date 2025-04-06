using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core._Npc2;
using GUZ.Core.Animations;
using GUZ.Core.Globals;
using UnityEditor;
using UnityEngine;
using AnimationState = GUZ.Core.Animations.AnimationState;

namespace GUZ.Core.Editor.Tools
{
    public class AnimationSystemWindowTool : EditorWindow
    {
        private Dictionary<string, AnimationSystem> _animationSystems = new();
        private Vector2 _scrollPosition;

        private int _selectedAnimationSystemIndex;
        private AnimationSystem _targetAnimationSystem;
        private float _timeScale = 1f;
        private bool _isTimeScaleFoldedOut;

        private GUILayoutOption _buttonSmall = GUILayout.Width(50);
        private GUILayoutOption _buttonWide = GUILayout.Width(100);



        [MenuItem("UnZENity/Animation System Debugger", priority = 200)]
        public static void ShowWindow()
        {
            var titleContent = new GUIContent("Animation System", Constants.TextureUnZENityLogoTransparent);

            var window = GetWindow<AnimationSystemWindowTool>();
            window.titleContent = titleContent;
        }

        private void OnGUI()
        {
            DrawTimeScale();
            DrawNpcSelection();

            if (_targetAnimationSystem == null)
            {
                EditorGUILayout.HelpBox("No AnimationSystem selected in the scene!", MessageType.Warning);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawBreakpointInfo();
            DrawAnimationInfo();
            DrawBoneStates();

            EditorGUILayout.EndScrollView();

            // Repaint the window every frame to update the animation state
            Repaint();
        }

        private void DrawNpcSelection()
        {
            EditorGUILayout.LabelField("Select NPC", EditorStyles.boldLabel);

            // NPC dropdown
            _selectedAnimationSystemIndex =
                EditorGUILayout.Popup("NPC", _selectedAnimationSystemIndex, _animationSystems.Keys.ToArray(), GUILayout.Width(400));

            if (_selectedAnimationSystemIndex >= _animationSystems.Count)
            {
                _targetAnimationSystem = null;
            }
            else
            {
                var oldSelectedAnimationSystem = _targetAnimationSystem;
                _targetAnimationSystem = _animationSystems.Values.ElementAt(_selectedAnimationSystemIndex);

                if (oldSelectedAnimationSystem != _targetAnimationSystem && oldSelectedAnimationSystem != null)
                {
                    // Reset! Otherwise, we will never find again from which NPC the game is pausing.
                    oldSelectedAnimationSystem.DebugPauseAtPlayAnimation = false;
                    oldSelectedAnimationSystem.DebugPauseAtStopAnimation = false;
                }
            }

            var origBack = GUI.backgroundColor;
            // Green == already collected once at least
            GUI.backgroundColor = _animationSystems.Any() ? Color.green : Color.grey;

            // Re-Collect AnimationSystems
            if (GUILayout.Button("(Re)collect Animation Systems", GUILayout.Width(400)))
            {
                var emptyElement = new[] { new { name = "<<Choose NPC>>", animComp = (AnimationSystem)null } };

                // Add additional empty element to the Dictionary
                _animationSystems = emptyElement
                    .Concat(FindObjectsOfType<AnimationSystem>()
                        .Select(animComp => new { animComp.GetComponentInParent<NpcLoader2>().name, animComp }))
                    .ToDictionary(i => i.name, i => i.animComp);
            }
            GUI.backgroundColor = origBack;
        }

        private void DrawTimeScale()
        {
            // TimeScale controls in a foldout
            _isTimeScaleFoldedOut = EditorGUILayout.Foldout(_isTimeScaleFoldedOut, "Time Scale Controls", true);
            if (_isTimeScaleFoldedOut)
            {
                // Center the buttons by using flexible space before and after
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace(); // This pushes content to the center

                if (GUILayout.Button("|<", _buttonSmall))
                {
                    _timeScale = 0f;
                }
                if (GUILayout.Button("<<", _buttonSmall))
                {
                    _timeScale -= 0.1f;
                }
                if (GUILayout.Button("<", _buttonSmall))
                {
                    _timeScale -= 0.01f;
                }

                var origBack = GUI.backgroundColor;

                // Green == We have the normal 1f timeScale active (Helps finding issues if it's not reset to 1 after use)
                GUI.backgroundColor = !Mathf.Approximately(Time.timeScale, 1f) ? Color.grey : Color.green;
                if (GUILayout.Button("1", _buttonSmall))
                {
                    _timeScale = 1f;
                }
                GUI.backgroundColor = origBack;

                if (GUILayout.Button(">", _buttonSmall))
                {
                    _timeScale += 0.01f;
                }
                if (GUILayout.Button(">>", _buttonSmall))
                {
                    _timeScale += 0.1f;
                }
                if (GUILayout.Button(">|", _buttonSmall))
                {
                    _timeScale = 2f;
                }

                GUILayout.FlexibleSpace(); // This pushes content to the center
                EditorGUILayout.EndHorizontal();

                // Show slider inside the foldout, also centered
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Time Scale:", GUILayout.Width(70));
                _timeScale = EditorGUILayout.Slider(_timeScale, 0, 2, GUILayout.Width(200));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                // Show current timescale value with a label
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField($"Current Value: {_timeScale:F2}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                DrawDivider();
            }

            // Apply the timescale value (outside the foldout so it's always applied)
            Time.timeScale = _timeScale;
        }

        private void DrawBreakpointInfo()
        {
            var origBackgroundColor = GUI.backgroundColor;

            DrawDivider();
            EditorGUILayout.LabelField("Breakpoints", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUI.backgroundColor = _targetAnimationSystem.DebugPauseAtPlayAnimation ? Color.green : origBackgroundColor;
            if (GUILayout.Button("PlayAnimation", _buttonWide))
            {
                _targetAnimationSystem.DebugPauseAtPlayAnimation = !_targetAnimationSystem.DebugPauseAtPlayAnimation;
            }

            GUI.backgroundColor = _targetAnimationSystem.DebugPauseAtStopAnimation ? Color.green : origBackgroundColor;
            if (GUILayout.Button("StopAnimation", _buttonWide))
            {
                _targetAnimationSystem.DebugPauseAtStopAnimation = !_targetAnimationSystem.DebugPauseAtStopAnimation;
            }
            GUILayout.EndHorizontal();

            GUI.backgroundColor = origBackgroundColor;
        }

        private void DrawAnimationInfo()
        {
            DrawDivider();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Layer - Animation", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Time x/y - State", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            // Access the currently playing animations from selected AnimationSystem
            foreach (var trackInstance in _targetAnimationSystem.DebugTrackInstances)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{trackInstance.Track.Layer:D2} - {trackInstance.Track.Name}");
                EditorGUILayout.LabelField(
                    $"{trackInstance.CurrentTime:F2} / {trackInstance.Track.Duration:F2} - {trackInstance.State}");
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawBoneStates()
        {
            DrawDivider();

            var originalBackgroundColor = GUI.backgroundColor;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Bone States", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // "" | "S_WALK" | "T_DIALOGGESTURE_00" | ...
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("-");
            foreach (var trackInstance in _targetAnimationSystem.DebugTrackInstances)
            {
                EditorGUILayout.LabelField(trackInstance.Track.Name);
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

                    if (boneIndex == -1)
                    {
                        EditorGUILayout.LabelField("-");
                        continue;
                    }

                    switch (trackInstance.BoneStates[boneIndex])
                    {
                        case AnimationState.None:
                        case AnimationState.BlendIn:
                        case AnimationState.Play:
                            GUI.backgroundColor = Color.Lerp(Color.red, Color.green, trackInstance.BoneBlendWeights[boneIndex]);
                            break;
                        case AnimationState.BlendOut:
                        case AnimationState.Stop:
                            GUI.backgroundColor = Color.Lerp(Color.grey, Color.green, trackInstance.BoneBlendWeights[boneIndex]);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // Reserve a rectangle for the progress bar
                    var progressRect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                    EditorGUI.ProgressBar(progressRect, trackInstance.BoneBlendWeights[boneIndex],
                        $"{trackInstance.BoneStates[boneIndex]}({trackInstance.BoneBlendWeights[boneIndex]:F2})");
                }
                EditorGUILayout.EndHorizontal();
                GUI.backgroundColor = originalBackgroundColor;
            }
        }

        private void DrawDivider()
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, Color.grey);
            EditorGUILayout.Space(5);
        }
    }
}
