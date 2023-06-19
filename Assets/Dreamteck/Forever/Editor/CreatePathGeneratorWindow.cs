namespace Dreamteck.Forever
{
    using UnityEngine;
    using UnityEditor;
    using System;

    public class CreatePathGeneratorWindow : EditorWindow
    {
        Type[] generatorTypes = new Type[0];
        Vector2 scroll = Vector2.zero;

        [MenuItem("Assets/Create/Forever/Path Generator")]
        public static void CreateWindow()
        {
            GetWindow<CreatePathGeneratorWindow>(true);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("New Path Generator");
            generatorTypes = FindDerivedClasses.GetAllDerivedClasses(typeof(LevelPathGenerator)).ToArray();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < generatorTypes.Length; i++)
            {
                string btnTxt = generatorTypes[i].ToString();
                if (btnTxt.StartsWith("Dreamteck.Forever.")) btnTxt = btnTxt.Substring("Dreamteck.Forever.".Length);
                if (GUILayout.Button(btnTxt))
                {
                    Selection.activeObject = ScriptableObjectUtility.CreateAsset(generatorTypes[i].ToString(), btnTxt);
                    Close();
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
