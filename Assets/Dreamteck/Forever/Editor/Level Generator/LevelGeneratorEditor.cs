namespace Dreamteck.Forever.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;
    using System.Collections.Generic;
    using UnityEngine.UI;
    using UnityEditorInternal;

    [CustomEditor(typeof(LevelGenerator))]
    public class LevelGeneratorEditor : Editor
    {
        private GUIStyle _boxStyle = null;
        private bool _levelFoldout = false;
        private MonoScript _script;
        private ReorderableList _levelList;
        private SerializedProperty _levelCollection;
        private string _saveLevelPath = "";


        private void OnEnable()
        {
            _levelFoldout = EditorPrefs.GetBool("Dreamteck.Forever.LevelEditor.levelFoldout", _levelFoldout);
            _script = MonoScript.FromMonoBehaviour((LevelGenerator)target);
            _levelCollection = serializedObject.FindProperty("_levelCollection");
            _levelList = new ReorderableList(serializedObject, _levelCollection);
            _levelList.drawHeaderCallback += DrawHeader;
            _levelList.drawElementCallback += DrawElement;
            _levelList.onAddCallback += AddItem;
            _levelList.onRemoveCallback += RemoveItem;
            _saveLevelPath = Application.dataPath;
        }

        private void OnDisable()
        {
             EditorPrefs.SetBool("Dreamteck.Forever.LevelEditor.levelFoldout", _levelFoldout);
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            GUI.color = new Color(1, 1, 1, 0.5f);
            EditorGUILayout.ObjectField("Script", _script, typeof(MonoScript), false);
            GUI.color = Color.white;
            serializedObject.Update();
            SerializedProperty loopSegmentLogic = serializedObject.FindProperty("_loopSegmentLogic");
            SerializedProperty maxSegments = serializedObject.FindProperty("_maxSegments");
            SerializedProperty pathGenerator = serializedObject.FindProperty("_sharedPathGenerator");
            SerializedProperty usePathGeneratorInstance = serializedObject.FindProperty("usePathGeneratorInstance");
            SerializedProperty generateSegmentsAhead = serializedObject.FindProperty("_generateSegmentsAhead");
            SerializedProperty activateSegmentsAhead = serializedObject.FindProperty("_activateSegmentsAhead");
            SerializedProperty levelIteration = serializedObject.FindProperty("_levelIteration");
            SerializedProperty levelRandomizer = serializedObject.FindProperty("_levelRandomizer");
            SerializedProperty generationRandomizer = serializedObject.FindProperty("_generationRandomizer");
            SerializedProperty startLevel = serializedObject.FindProperty("_startLevel");
            SerializedProperty buildOnAwake = serializedObject.FindProperty("_buildOnAwake");
            SerializedProperty loadTimeout = serializedObject.FindProperty("_loadTimeout");
            SerializedProperty multithreaded = serializedObject.FindProperty("_multithreaded");
            SerializedProperty useUnloadUnusedAssets = serializedObject.FindProperty("_useUnloadUnusedAssets");

            LevelGenerator gen = (LevelGenerator)target;
            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(gen, "Edit Level Generator");
            EditorGUILayout.PropertyField(buildOnAwake);
            EditorGUILayout.PropertyField(loadTimeout);
            EditorGUILayout.PropertyField(multithreaded);
            EditorGUILayout.PropertyField(useUnloadUnusedAssets);

            EditorGUILayout.PropertyField(loopSegmentLogic, new GUIContent("Loop Segments", "If using a path generator which makes a closed loop, setting this to true ensures proper segment logic handling when going from the last segment to the first"));
            if(!loopSegmentLogic.boolValue)
            {
                EditorGUILayout.PropertyField(maxSegments, new GUIContent("Max. Segments", "Forever will remove old segments when the maximum number is reached."));
                if (maxSegments.intValue < 1) maxSegments.intValue = 1;
            }

            EditorGUILayout.PropertyField(generateSegmentsAhead);
            if (generateSegmentsAhead.intValue < 1) generateSegmentsAhead.intValue = 1;
            if (generateSegmentsAhead.intValue > maxSegments.intValue) generateSegmentsAhead.intValue = maxSegments.intValue;
            EditorGUILayout.PropertyField(activateSegmentsAhead);
            if (activateSegmentsAhead.intValue < 0) activateSegmentsAhead.intValue = 0;
            if (activateSegmentsAhead.intValue > generateSegmentsAhead.intValue) activateSegmentsAhead.intValue = generateSegmentsAhead.intValue;

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(pathGenerator);
            if (pathGenerator.objectReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(usePathGeneratorInstance);
                EditorGUI.indentLevel--;
            } else
            {
                EditorGUILayout.HelpBox("A Path Generator needs to be assigned to the Level Generator.", MessageType.Error);
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.GetStyle("box"));
                _boxStyle.normal.background = DreamteckEditorGUI.blankImage;
                _boxStyle.margin = new RectOffset(0, 0, 0, 2);
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(generationRandomizer);

            EditorGUILayout.Space();
            _levelFoldout = EditorGUILayout.Foldout(_levelFoldout, "Levels");
            if (_levelFoldout)
            {
                EditorGUILayout.PropertyField(levelIteration);
                if(levelIteration.intValue == (int)LevelGenerator.LevelIteration.Random)
                {
                    EditorGUILayout.PropertyField(levelRandomizer);
                }
                EditorGUILayout.PropertyField(startLevel);

                if (startLevel.intValue >= _levelCollection.arraySize)
                {
                    startLevel.intValue = _levelCollection.arraySize - 1;
                }
                if (startLevel.intValue < 0)
                {
                    startLevel.intValue = 0;
                }
                _levelList.DoLayoutList();
                ListLegacyLevels();
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }


            if (gen.levelCollection.Length == 0)
            {
                EditorGUILayout.HelpBox("No defined levels. Define at least one level.", MessageType.Error);
            }
        }

        private void AddItem(ReorderableList list)
        {
            _levelCollection.InsertArrayElementAtIndex(_levelCollection.arraySize);
            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveItem(ReorderableList list)
        {
            _levelCollection.DeleteArrayElementAtIndex(list.index--);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader(Rect rect)
        {
            GUI.Label(rect, "Level Collection");
        }

        private void DrawElement(Rect rect, int index, bool active, bool focused)
        {
            SerializedProperty item = _levelCollection.GetArrayElementAtIndex(index);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, item);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        void Delete(int index)
        {
            SerializedProperty levelsProperty = serializedObject.FindProperty("levels");
            levelsProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        void ListLegacyLevels()
        {
            LevelGenerator generator = (LevelGenerator)target;
            SerializedProperty levelsProperty = serializedObject.FindProperty("levels");
            SerializedProperty levelCollectionProperty = serializedObject.FindProperty("_levelCollection");
            if (levelsProperty.arraySize > 0)
            {
                EditorGUILayout.HelpBox("Legacy Levels Found. Convert them to the new scriptable object format.", MessageType.Warning);
            }
            for (int i = 0; i < levelsProperty.arraySize; i++)
            {
                SerializedProperty legacyLevel = levelsProperty.GetArrayElementAtIndex(i);
                SerializedProperty levelTitle = legacyLevel.FindPropertyRelative("title");
                EditorGUILayout.BeginVertical(_boxStyle);
                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button("Convert To New Format"))
                {
                    _saveLevelPath = EditorUtility.OpenFolderPanel("Localtion", _saveLevelPath, "");
                    if (_saveLevelPath.StartsWith(Application.dataPath))
                    {
                        if (System.IO.Directory.Exists(_saveLevelPath))
                        {
                            _saveLevelPath = _saveLevelPath.Substring(Application.dataPath.Length);
                            ForeverLevel newLevel = ScriptableObjectUtility.CreateAsset<ForeverLevel>(_saveLevelPath + "/" + levelTitle.stringValue, false);
                            if (newLevel)
                            {
                                newLevel.name = levelTitle.stringValue;
                                newLevel.CopyFromLegacy(generator.levels[i]);
                                levelCollectionProperty.arraySize++;
                                SerializedProperty levelProperty = levelCollectionProperty.GetArrayElementAtIndex(levelCollectionProperty.arraySize - 1);
                                levelProperty.objectReferenceValue = newLevel;
                                serializedObject.ApplyModifiedProperties();
                                Delete(i);
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("Error", "Error creating new level object", "OK");
                            }
                            AssetDatabase.SaveAssets();
                        }
                    } else
                    {
                        EditorUtility.DisplayDialog("Invalid Path", "The selected directory is not a part of this project.", "I'll Try Again");
                    }

                }
                EditorGUILayout.LabelField(i + "  " + levelTitle.stringValue);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
        }
    }
}
