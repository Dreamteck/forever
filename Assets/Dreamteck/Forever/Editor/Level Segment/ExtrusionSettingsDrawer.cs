namespace Dreamteck.Forever.Editor
{
    using UnityEngine;
    using UnityEditor;


    [CustomPropertyDrawer(typeof(ExtrusionSettings))]
    public class ExtrusionSettingsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUIUtility.labelWidth = 105;
            SerializedProperty indexing = property.FindPropertyRelative("indexing");
            SerializedProperty boundsInclusion = property.FindPropertyRelative("boundsInclusion");
            SerializedProperty applyRotation = property.FindPropertyRelative("applyRotation");
            SerializedProperty keepUpright = property.FindPropertyRelative("keepUpright");
            SerializedProperty upVector = property.FindPropertyRelative("upVector");
            SerializedProperty applyScale = property.FindPropertyRelative("applyScale");
            SerializedProperty bendSprite = property.FindPropertyRelative("bendSprite");
            SerializedProperty bendMesh = property.FindPropertyRelative("bendMesh");
            SerializedProperty applyMeshColors = property.FindPropertyRelative("applyMeshColors");
            SerializedProperty meshColliderHandling = property.FindPropertyRelative("meshColliderHandling");
#if DREAMTECK_SPLINES
            SerializedProperty bendSpline = property.FindPropertyRelative("bendSpline");
#endif


            EditorGUILayout.PropertyField(indexing);
            EditorGUILayout.Space();

            boundsInclusion.intValue = (EditorGUILayout.MaskField("Include in Bounds", boundsInclusion.intValue, boundsInclusion.enumNames));


            EditorGUILayout.LabelField("Transform", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(applyRotation);
            if (applyRotation.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(keepUpright);
                if (keepUpright.boolValue)
                {
                    EditorGUILayout.PropertyField(upVector);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.PropertyField(applyScale);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Extrusion", EditorStyles.boldLabel);

            if (!bendSprite.boolValue)
            {
                EditorGUILayout.PropertyField(bendMesh);
            }
            if (bendMesh.boolValue)
            {
                EditorGUILayout.PropertyField(applyMeshColors);
            }
            if (!bendMesh.boolValue)
            {
                EditorGUILayout.PropertyField(bendSprite);
            }

#if DREAMTECK_SPLINES
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Splines", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(bendSpline);
#endif

            EditorGUILayout.PropertyField(meshColliderHandling, new GUIContent("Mesh Collider"));
        }
    }
}
