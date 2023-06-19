namespace Dreamteck.Forever.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System;

    [CustomEditor(typeof(ForeverLevel), true)]
    [CanEditMultipleObjects]
    public class ForeverLevelEditor : UnityEditor.Editor
    {
        private SerializedProperty _remoteSceneName;
        private SerializedProperty _loadingPriority;
        private SerializedProperty _remoteSequence;
        private string[] sceneNames = new string[0];


        private void OnEnable()
        {
            _remoteSceneName = serializedObject.FindProperty("_remoteSceneName");
            _remoteSequence = serializedObject.FindProperty("_remoteSequence");
            _loadingPriority = serializedObject.FindProperty("_loadingPriority");
#if UNITY_2019_3_OR_NEWER
            if (EditorWindow.HasOpenInstances<SequenceEditWindow>())
            {
                OpenSequenceEditor();
            }
#endif
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            if (_remoteSequence.boolValue)
            {
                LevelSelectionDropdown(_remoteSceneName);
                EditorGUILayout.PropertyField(_loadingPriority);
            } else if(targets.Length == 1)
            {
                if (GUILayout.Button("Edit Sequence", GUILayout.Height(50)))
                {
                    OpenSequenceEditor();
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void OpenSequenceEditor()
        {
            ForeverLevel level = (ForeverLevel)target;
            SequenceEditWindow window = EditorWindow.GetWindow<SequenceEditWindow>(true);
            window.Init(level.sequenceCollection, level, OnApplySequences, level.name + " - Sequence Editor");
            window.onClose += OnEditorClosed;
        }

        private void OnEditorClosed()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }
        }

        private void OnApplySequences(SegmentSequence[] sequences)
        {
            ForeverLevel level = (ForeverLevel)target;
            level.sequenceCollection.sequences = sequences;
        }

        private void LevelSelectionDropdown(SerializedProperty property)
        {
            EditorGUIUtility.labelWidth = 70;
            if (sceneNames.Length != EditorBuildSettings.scenes.Length + 1) sceneNames = new string[EditorBuildSettings.scenes.Length + 1];
            int sceneIndex = 0;
            sceneNames[0] = "NONE";
            for (int i = 1; i < sceneNames.Length; i++)
            {
                sceneNames[i] = System.IO.Path.GetFileNameWithoutExtension(EditorBuildSettings.scenes[i - 1].path);
                if (property.stringValue == sceneNames[i]) sceneIndex = i;
            }
            EditorGUIUtility.labelWidth = 80;
            sceneIndex = EditorGUILayout.Popup("Scene", sceneIndex, sceneNames);
            if (sceneIndex == 0) property.stringValue = "";
            else property.stringValue = sceneNames[sceneIndex];
            if (sceneIndex > 0)
            {
                if (GUILayout.Button("Go To Scene"))
                {
                    for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
                    {
                        if (System.IO.Path.GetFileNameWithoutExtension(EditorBuildSettings.scenes[i].path) == property.stringValue)
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(EditorBuildSettings.scenes[i].path, UnityEditor.SceneManagement.OpenSceneMode.Single);
                            break;
                        }
                    }
                }
            }
            EditorGUIUtility.labelWidth = 0;
        }
    }
}