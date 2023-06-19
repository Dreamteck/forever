namespace Dreamteck.Forever.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(RandomPathGenerator))]
    public class RandomPathGeneratorEditor : HighLevelPathGeneratorEditor
    {
        bool orientation = false;
        bool offset = false;
        bool colors = false;
        bool sizes = false;
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            Panel("Orientation", ref orientation, OrientationGUI);
            Panel("Colors", ref colors, ColorGUI);
            Panel("Sizes", ref sizes, SizeGUI);
            Panel("Offset", ref offset, OffsetGUI);
            RandomPathGenerator gen = (RandomPathGenerator)target;
            if (gen.randomizer == null)
            {
                EditorGUILayout.HelpBox("This generator needs a randomizer reference in order to work", MessageType.Error);
            }
        }

        protected virtual void OrientationGUI()
        {
            serializedObject.Update();
            SerializedProperty usePitch = serializedObject.FindProperty("usePitch");
            SerializedProperty useYaw = serializedObject.FindProperty("useYaw");
            SerializedProperty useRoll = serializedObject.FindProperty("useRoll");
            SerializedProperty restrictPitch = serializedObject.FindProperty("restrictPitch");
            SerializedProperty minOrientation = serializedObject.FindProperty("minOrientation");
            SerializedProperty maxOrientation = serializedObject.FindProperty("maxOrientation");
            SerializedProperty minRandomStep = serializedObject.FindProperty("minRandomStep");
            SerializedProperty maxRandomStep = serializedObject.FindProperty("maxRandomStep");
            SerializedProperty minTurnRate = serializedObject.FindProperty("minTurnRate");
            SerializedProperty maxTurnRate = serializedObject.FindProperty("maxTurnRate");
            SerializedProperty useStartPitchTarget = serializedObject.FindProperty("useStartPitchTarget");
            SerializedProperty startTargetOrientation = serializedObject.FindProperty("startTargetOrientation");
            SerializedProperty restrictYaw = serializedObject.FindProperty("restrictYaw");
            SerializedProperty useStartYawTarget = serializedObject.FindProperty("useStartYawTarget");
            SerializedProperty restrictRoll = serializedObject.FindProperty("restrictRoll");
            SerializedProperty useStartRollTarget = serializedObject.FindProperty("useStartRollTarget");


            Vector3 minOrientationVector = minOrientation.vector3Value;
            Vector3 maxOrientationVector = maxOrientation.vector3Value;
            Vector3 minRandomStepVector = minRandomStep.vector3Value;
            Vector3 maxRandomStepVector = maxRandomStep.vector3Value;
            Vector3 minTurnRateVector = minTurnRate.vector3Value;
            Vector3 maxTurnRateVector = maxTurnRate.vector3Value;
            Vector3 startTargetOrientationVector = startTargetOrientation.vector3Value;

            EditorGUI.BeginChangeCheck();

            ComponentUI("Pitch", usePitch, restrictPitch, ref minOrientationVector.x, ref maxOrientationVector.x, ref minRandomStepVector.x, ref maxRandomStepVector.x, ref minTurnRateVector.x, ref maxTurnRateVector.x, useStartPitchTarget, ref startTargetOrientationVector.x);
            ComponentUI("Yaw", useYaw, restrictYaw, ref minOrientationVector.y, ref maxOrientationVector.y, ref minRandomStepVector.y, ref maxRandomStepVector.y, ref minTurnRateVector.y, ref maxTurnRateVector.y, useStartYawTarget, ref startTargetOrientationVector.y);
            ComponentUI("Roll", useRoll, restrictRoll, ref minOrientationVector.z, ref maxOrientationVector.z, ref minRandomStepVector.z, ref maxRandomStepVector.z, ref minTurnRateVector.z, ref maxTurnRateVector.z, useStartRollTarget, ref startTargetOrientationVector.z);

            if (EditorGUI.EndChangeCheck())
            {
                minOrientation.vector3Value = minOrientationVector;
                maxOrientation.vector3Value = maxOrientationVector;
                minRandomStep.vector3Value = minRandomStepVector;
                maxRandomStep.vector3Value = maxRandomStepVector;
                minTurnRate.vector3Value = minTurnRateVector;
                maxTurnRate.vector3Value = maxTurnRateVector;
                startTargetOrientation.vector3Value = startTargetOrientationVector;
                serializedObject.ApplyModifiedProperties();
            }
            
        }

        private void ComponentUI(string name, SerializedProperty toggle, SerializedProperty useRestriction, ref float restrictMin, ref float restrictMax, ref float minRandomStep, ref float maxRandomStep, ref float minTurnRate, ref float maxTurnRate, SerializedProperty useStartTarget, ref float startTarget)
        {
            EditorGUILayout.BeginHorizontal();
            toggle.boolValue = EditorGUILayout.Toggle(toggle.boolValue, GUILayout.Width(20));
            GUILayout.Label(name, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            if (toggle.boolValue)
            {
                EditorGUILayout.PropertyField(useRestriction, new GUIContent("Restrict"));
                if (useRestriction.boolValue)
                {
                    restrictMin = EditorGUILayout.FloatField("Restrict Min.", restrictMin);
                    restrictMax = EditorGUILayout.FloatField("Restrict Max.", restrictMax);
                }
                minRandomStep = EditorGUILayout.FloatField("Min. Target Step", minRandomStep);
                if (minRandomStep < 0f) minRandomStep = 0f;
                maxRandomStep = EditorGUILayout.FloatField("Max. Target Step", maxRandomStep);
                if (maxRandomStep < 0f) maxRandomStep = 0f;
                minTurnRate = EditorGUILayout.FloatField("Min. Turn Rate", minTurnRate);
                maxTurnRate = EditorGUILayout.FloatField("Max. Turn Rate", maxTurnRate);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(useStartTarget, new GUIContent("Level Start Target"));
                if (useStartTarget.boolValue)
                {
                    startTarget = EditorGUILayout.FloatField("", startTarget);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
        }

        protected virtual void ColorGUI()
        {
            serializedObject.Update();
            SerializedProperty useColors = serializedObject.FindProperty("useColors");
            SerializedProperty minColor = serializedObject.FindProperty("minColor");
            SerializedProperty maxColor = serializedObject.FindProperty("maxColor");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(useColors);
            EditorGUILayout.EndHorizontal();
            if (useColors.boolValue)
            {
                EditorGUILayout.PropertyField(minColor, new GUIContent("Min."));
                EditorGUILayout.PropertyField(maxColor, new GUIContent("Max."));
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        protected virtual void SizeGUI()
        {
            SerializedProperty useSizes = serializedObject.FindProperty("useSizes");
            SerializedProperty minSize = serializedObject.FindProperty("minSize");
            SerializedProperty maxSize = serializedObject.FindProperty("maxSize");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(useSizes);
            EditorGUILayout.EndHorizontal();
            if (useSizes.boolValue)
            {
                EditorGUILayout.PropertyField(minSize, new GUIContent("Min."));
                EditorGUILayout.PropertyField(maxSize, new GUIContent("Max."));
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        protected virtual void OffsetGUI()
        {
            SerializedProperty minSegmentOffset = serializedObject.FindProperty("minSegmentOffset");
            SerializedProperty maxSegmentOffset = serializedObject.FindProperty("maxSegmentOffset");
            SerializedProperty segmentOffsetSpace = serializedObject.FindProperty("segmentOffsetSpace");
            SerializedProperty newLevelMinOffset = serializedObject.FindProperty("newLevelMinOffset");
            SerializedProperty newLevelMaxOffset = serializedObject.FindProperty("newLevelMaxOffset");
            SerializedProperty levelOffsetSpace = serializedObject.FindProperty("levelOffsetSpace");

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(minSegmentOffset, new GUIContent("Min. Segment Offset"));
            EditorGUILayout.PropertyField(maxSegmentOffset, new GUIContent("Max. Segment Offset"));
            EditorGUILayout.PropertyField(segmentOffsetSpace, new GUIContent("Segment Offset Space"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(newLevelMinOffset, new GUIContent("Min. New Level Offset"));
            EditorGUILayout.PropertyField(newLevelMaxOffset, new GUIContent("Max. New Level Offset"));
            EditorGUILayout.PropertyField(levelOffsetSpace, new GUIContent("Level Offset Space"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
