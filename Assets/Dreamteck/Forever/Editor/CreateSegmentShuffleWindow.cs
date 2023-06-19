namespace Dreamteck.Forever
{
    using UnityEngine;
    using UnityEditor;
    using System;

    public class CreateSegmentShuffleWindow : EditorWindow
    {
        Type[] randomizerTypes = new Type[0];
        Vector2 scroll = Vector2.zero;

        [MenuItem("Assets/Create/Forever/Segment Shuffle")]
        public static void CreateWindow()
        {
            GetWindow<CreateSegmentShuffleWindow>(true);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("New Segment Shuffle");
            randomizerTypes = FindDerivedClasses.GetAllDerivedClasses(typeof(SegmentShuffle)).ToArray();
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < randomizerTypes.Length; i++)
            {
                string btnTxt = randomizerTypes[i].ToString();
                if (btnTxt.StartsWith("Dreamteck.Forever.")) btnTxt = btnTxt.Substring("Dreamteck.Forever.".Length);
                if (GUILayout.Button(btnTxt))
                {
                    Selection.activeObject = ScriptableObjectUtility.CreateAsset(randomizerTypes[i].ToString(), btnTxt);
                    Close();
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
