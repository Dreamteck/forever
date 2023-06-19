namespace Dreamteck.Forever.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(LevelPathGenerator), true)]
    public class PathGeneratorEditor : Editor
    {
        bool path = false;
        protected delegate void UIHandler();

        public override void OnInspectorGUI()
        {
            LevelPathGenerator generator = (LevelPathGenerator)target;
            base.OnInspectorGUI();
            OnInspector();
        }

        protected virtual void OnInspector()
        {
            LevelPathGenerator generator = (LevelPathGenerator)target;
            Panel("Path", ref path, PathGUI);
            if (GUI.changed) EditorUtility.SetDirty(generator);
        }

        protected virtual void PathGUI()
        {
            serializedObject.Update();
            SerializedProperty pathType = serializedObject.FindProperty("pathType");
            SerializedProperty controlPointsPerSegment = serializedObject.FindProperty("controlPointsPerSegment");
            SerializedProperty sampleRate = serializedObject.FindProperty("sampleRate");
            SerializedProperty customNormalInterpolation = serializedObject.FindProperty("customNormalInterpolation");
            SerializedProperty normalInterpolation = serializedObject.FindProperty("normalInterpolation");
            SerializedProperty customValueInterpolation = serializedObject.FindProperty("customValueInterpolation");
            SerializedProperty valueInterpolation = serializedObject.FindProperty("valueInterpolation");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(pathType, new GUIContent("Type"));
            EditorGUILayout.PropertyField(controlPointsPerSegment, new GUIContent("Points Per Segment"));
            if (controlPointsPerSegment.intValue < 2)
            {
                controlPointsPerSegment.intValue = 2;
            }
            EditorGUILayout.PropertyField(sampleRate, new GUIContent("Sample Rate"));
            if (sampleRate.intValue < 1)
            {
                sampleRate.intValue = 1;
            }
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(customNormalInterpolation, new GUIContent("Normal Interpolation"));
            if (customNormalInterpolation.boolValue)
            {
                EditorGUILayout.PropertyField(normalInterpolation, new GUIContent());
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(customValueInterpolation, new GUIContent("Value Interpolation"));
            if (customValueInterpolation.boolValue)
            {
                EditorGUILayout.PropertyField(valueInterpolation, new GUIContent());
            }
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        protected void Panel(string name, ref bool toggle, UIHandler handler)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.indentLevel++;
            toggle = EditorGUILayout.Foldout(toggle, name);
            EditorGUI.indentLevel--;
            if (toggle)
            {
                EditorGUILayout.Space();
                handler();
            }
            EditorGUILayout.EndVertical();
        }

        protected void OnEnable()
        {
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += DrawScene;
#else
            SceneView.onSceneGUIDelegate += DrawScene;
#endif
        }

        protected void OnDisable()
        {
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= DrawScene;
#else
            SceneView.onSceneGUIDelegate -= DrawScene;
#endif
        }

        public virtual void DrawScene(SceneView current)
        {

        }
    }
}
