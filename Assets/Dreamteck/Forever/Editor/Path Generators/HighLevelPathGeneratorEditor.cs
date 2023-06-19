namespace Dreamteck.Forever.Editor
{
    using UnityEngine;
    using UnityEditor;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(HighLevelPathGenerator))]
    public class HighLevelPathGeneratorEditor : PathGeneratorEditor
    {
        protected override void PathGUI()
        {
            base.PathGUI();
            HighLevelPathGenerator generator = (HighLevelPathGenerator)target;
            EditorGUILayout.BeginHorizontal();
            if (generator.useCustomNormalDirection) EditorGUIUtility.labelWidth = 110f;
            generator.useCustomNormalDirection = EditorGUILayout.Toggle("Normals Override", generator.useCustomNormalDirection);
            if (generator.useCustomNormalDirection) generator.customNormalDirection = EditorGUILayout.Vector3Field("", generator.customNormalDirection);
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.EndHorizontal();
            if (GUI.changed) EditorUtility.SetDirty(generator);
        }
    }
}
