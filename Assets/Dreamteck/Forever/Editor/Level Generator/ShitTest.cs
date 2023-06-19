using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Dreamteck.Forever.Editor
{
    public class ShitTest : EditorWindow
    {
        SerializedProperty prop;
        SerializedObject obj;

        public void Init(SerializedProperty shitProperty, SerializedObject ob)
        {
            prop = shitProperty;
            obj = ob;
        }

        private void OnGUI()
        {
            SerializedProperty sequence = prop.FindPropertyRelative("sequences.Array.data[0]");
            SerializedProperty sequenceName = sequence.FindPropertyRelative("name");
            EditorGUILayout.PropertyField(sequenceName);
            if (GUI.changed)
            {
                obj.ApplyModifiedProperties();
            }
        }
    }
}

