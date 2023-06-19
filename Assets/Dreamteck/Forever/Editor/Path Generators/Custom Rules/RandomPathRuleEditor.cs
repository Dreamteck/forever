namespace Dreamteck.Forever.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(RandomPathRule), true)]
    public class RandomPathRuleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            RandomPathRule rule = (RandomPathRule)target;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Set Target:");
            EditorGUIUtility.labelWidth = 30f;
            EditorGUILayout.BeginHorizontal();
            rule.setYaw = EditorGUILayout.Toggle("Yaw", rule.setYaw);
            rule.setPitch = EditorGUILayout.Toggle("Pitch", rule.setPitch);
            rule.setRoll = EditorGUILayout.Toggle("Roll", rule.setRoll);
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (rule.setYaw)
            {
                GUILayout.Box("", GUILayout.Width(60), GUILayout.Height(60));
                Rect rect = GUILayoutUtility.GetLastRect();
                Quaternion rot = Quaternion.AngleAxis(rule.targetYaw, Vector3.forward);
                DrawArrow(rect, rot);
            }

            if (rule.setPitch)
            {
                GUILayout.Box("", GUILayout.Width(60), GUILayout.Height(60));
                Rect rect = GUILayoutUtility.GetLastRect();
                Quaternion rot = Quaternion.AngleAxis(rule.targetPitch + 90, Vector3.forward);
                DrawArrow(rect, rot);
            }

            if (rule.setRoll)
            {
                GUILayout.Box("", GUILayout.Width(60), GUILayout.Height(60));
                Rect rect = GUILayoutUtility.GetLastRect();
                Quaternion rot = Quaternion.AngleAxis(rule.targetRoll, Vector3.forward);
                DrawArrow(rect, rot);
            }
            EditorGUILayout.EndHorizontal();

            if (rule.setYaw) rule.targetYaw = EditorGUILayout.Slider("Target Yaw", rule.targetYaw, -180f, 180f);
            if (rule.setPitch) rule.targetPitch = EditorGUILayout.Slider("Target Pitch", rule.targetPitch, -180, 180f);
            if (rule.setRoll) rule.targetRoll = EditorGUILayout.Slider("Target Roll", rule.targetRoll, -180f, 180f);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Set Step:");
            EditorGUIUtility.labelWidth = 30f;
            EditorGUILayout.BeginHorizontal();
            rule.setYawStep = EditorGUILayout.Toggle("Yaw", rule.setYawStep);
            rule.setPitchStep = EditorGUILayout.Toggle("Pitch", rule.setPitchStep);
            rule.setRollStep = EditorGUILayout.Toggle("Roll", rule.setRollStep);
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 0;
            EditorGUILayout.Space();
            if (rule.setYawStep) rule.yawStep = EditorGUILayout.FloatField("Yaw Step", rule.yawStep);
            if (rule.setPitchStep) rule.pitchStep = EditorGUILayout.FloatField("Pitch Step", rule.pitchStep);
            if (rule.setRollStep) rule.rollStep = EditorGUILayout.FloatField("Roll Step", rule.rollStep);

            if (EditorGUI.EndChangeCheck()) Undo.RecordObject(target, "Property Change");
        }

        void DrawArrow(Rect rect, Quaternion orientation)
        {
            Handles.BeginGUI();
            Vector2 arrowDirection = orientation * -Vector2.up;
            Vector2 arrowRightArm = orientation * (Vector2.up + Vector2.right);
            Vector2 arrowLeftArm = orientation * (Vector2.up + Vector2.left);
            Vector2 arrowOrigin = new Vector2(rect.x + rect.width /2f, rect.y + rect.height / 2f);
            Vector2 arrowEnd = arrowOrigin + arrowDirection * 30;
            Handles.color = Color.black;
            Handles.DrawLine(arrowOrigin, arrowEnd);
            Handles.DrawLine(arrowEnd, arrowEnd + arrowRightArm * 5);
            Handles.DrawLine(arrowEnd, arrowEnd + arrowLeftArm * 5);
            Handles.EndGUI();
            Handles.color = Color.white;
        }
    }
}
