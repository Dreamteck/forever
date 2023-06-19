namespace Dreamteck.Forever
{
    using UnityEngine;
    using UnityEditor;
    using System;

    public class CreateCustomSequenceWindow : EditorWindow
    {
        Type[] sequenceTypes = new Type[0];
        Vector2 scroll = Vector2.zero;

        [MenuItem("Assets/Create/Forever/Custom Sequence")]
        public static void CreateWindow()
        {
            GetWindow<CreateCustomSequenceWindow>(true);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("New Custom Sequence");
            sequenceTypes = FindDerivedClasses.GetAllDerivedClasses(typeof(CustomSequence)).ToArray();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < sequenceTypes.Length; i++)
            {
                string btnTxt = sequenceTypes[i].ToString();
                if (btnTxt.StartsWith("Dreamteck.Forever.")) btnTxt = btnTxt.Substring("Dreamteck.Forever.".Length);
                if (GUILayout.Button(btnTxt))
                {
                    Selection.activeObject = ScriptableObjectUtility.CreateAsset(sequenceTypes[i].ToString(), btnTxt);
                    Close();
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
