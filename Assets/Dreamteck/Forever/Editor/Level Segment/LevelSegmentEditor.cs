namespace Dreamteck.Forever.Editor
{
    using UnityEngine;
    using System.Collections;
    using UnityEditor;
    using System.Collections.Generic;
    using Splines;

#if DREAMTECK_SPLINES
    using Splines.Editor;
#endif

#if UNITY_2021_1_OR_NEWER
    using UnityEditor.SceneManagement;
#else
    using UnityEditor.Experimental.SceneManagement;
#endif


    using System.IO;


    [CustomEditor(typeof(LevelSegment), true)]
    [CanEditMultipleObjects]
    public class LevelSegmentEditor : Editor
    {
        public class PropertyEditWindow : EditorWindow
        {
            public List<int> selectedProperties = null;
            public LevelSegment segment = null;
            public LevelSegmentEditor segmentEditor = null;
            private SerializedObject serialized;

            public void Init(Vector2 pos, LevelSegment segment, LevelSegmentEditor segmentEditor)
            {
                this.segment = segment;
                this.segmentEditor = segmentEditor;
                serialized = new SerializedObject(segment);
                Rect newPos = position;
                newPos.x = pos.x - position.width;
                if (newPos.y < pos.y) newPos.y = pos.y;
                position = newPos;
            }

            private void OnGUI()
            {
                if (selectedProperties.Count == 0) return;
                if (selectedProperties.Count == 1) titleContent = new GUIContent(segment.objectProperties[selectedProperties[0]].transform.name + " - Extrusion Settings");
                else titleContent = new GUIContent("Multiple Objects - Extrusion Settings");
                serialized.Update();
                SerializedProperty objectProperties = serialized.FindProperty("objectProperties");

                GUILayout.BeginVertical();
                bool settingsComponentPresent = false;
                int overridePropertyIndex = 0;
                SerializedProperty[] settings = new SerializedProperty[selectedProperties.Count];
                SerializedProperty[] overrides = new SerializedProperty[selectedProperties.Count];
                for (int i = 0; i < settings.Length; i++)
                {
                    SerializedProperty property = objectProperties.GetArrayElementAtIndex(selectedProperties[i]);
                    settings[i] = property.FindPropertyRelative("_extrusionSettings");
                    SerializedProperty overrideSettingsComponent = property.FindPropertyRelative("overrideSettingsComponent");
                    overrides[i] = overrideSettingsComponent;
                    if (!overrideSettingsComponent.boolValue)
                    {
                        overridePropertyIndex = i;
                    }
                    if (segment.objectProperties[selectedProperties[i]].hasSettingsComponent) settingsComponentPresent = true;
                }

                if (settingsComponentPresent)
                {
                    EditorGUILayout.HelpBox("One or more of the objects have a Settings Component attached. The components' extrusion settings will be used.", MessageType.Info);
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(overrides[overridePropertyIndex], new GUIContent("Overide Settings Component"));
                    if (EditorGUI.EndChangeCheck())
                    {
                        for (int i = 0; i < overrides.Length; i++)
                        {
                            overrides[i].boolValue = overrides[overridePropertyIndex].boolValue;
                        }
                        serialized.ApplyModifiedProperties();
                        segment.UpdateReferences();
                    }
                }

                if(!settingsComponentPresent || overrides[overridePropertyIndex].boolValue) {
                    
                    EditorGUILayout.Space();
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(settings[0], true);

                    if (EditorGUI.EndChangeCheck())
                    {
                        for (int i = 1; i < settings.Length; i++)
                        {
                            settings[i].FindPropertyRelative("meshColliderHandling").enumValueIndex = settings[0].FindPropertyRelative("meshColliderHandling").enumValueIndex;
                            settings[i].FindPropertyRelative("boundsInclusion").intValue = settings[0].FindPropertyRelative("boundsInclusion").intValue;
                            settings[i].FindPropertyRelative("indexing").enumValueIndex = settings[0].FindPropertyRelative("indexing").enumValueIndex;
                            settings[i].FindPropertyRelative("applyRotation").boolValue = settings[0].FindPropertyRelative("applyRotation").boolValue;
                            settings[i].FindPropertyRelative("keepUpright").boolValue = settings[0].FindPropertyRelative("keepUpright").boolValue;
                            settings[i].FindPropertyRelative("upVector").vector3Value = settings[0].FindPropertyRelative("upVector").vector3Value;
                            settings[i].FindPropertyRelative("applyScale").boolValue = settings[0].FindPropertyRelative("applyScale").boolValue;
                            settings[i].FindPropertyRelative("bendMesh").boolValue = settings[0].FindPropertyRelative("bendMesh").boolValue;
                            settings[i].FindPropertyRelative("bendSprite").boolValue = settings[0].FindPropertyRelative("bendSprite").boolValue;
                            settings[i].FindPropertyRelative("bendPolygonCollider").boolValue = settings[0].FindPropertyRelative("bendPolygonCollider").boolValue;
                            settings[i].FindPropertyRelative("applyMeshColors").boolValue = settings[0].FindPropertyRelative("applyMeshColors").boolValue;
#if DREAMTECK_SPLINES
                            settings[i].FindPropertyRelative("bendSpline").boolValue = settings[0].FindPropertyRelative("bendSpline").boolValue;
#endif
                        }
                        serialized.ApplyModifiedProperties();
                        segment.UpdateReferences();
                    }

                    if(GUILayout.Button("Select In Scene"))
                    {
                        List<GameObject> goList = new List<GameObject>();
                        for (int i = 0; i < selectedProperties.Count; i++)
                        {
                            goList.Add(segment.objectProperties[selectedProperties[i]].transform.gameObject);
                        }
                        Selection.objects = goList.ToArray();
                        Close();
                    }
                        
                }
                
            }

            private void OnDestroy()
            {
                segmentEditor.selectedProperties.Clear();
            }
        }

        [InitializeOnLoad]
        internal static class PrefabStageCheck
        {
            internal static bool open = false;
            static PrefabStageCheck()
            {
                PrefabStage.prefabStageOpened -= OnStageOpen;
                PrefabStage.prefabStageOpened += OnStageOpen;
                PrefabStage.prefabStageClosing -= OnStageClose;
                PrefabStage.prefabStageClosing += OnStageClose;
            }

            static void OnStageOpen(PrefabStage stage)
            {
                open = true;
            }

            static void OnStageClose(PrefabStage stage)
            {
                open = false;
            }
        }

        internal class PropertyBinder
        {
            internal int index = 0;
            internal string name = "";

            internal PropertyBinder(int index, string name)
            {
                this.index = index;
                this.name = name;
            }
        }

        private bool showProperties = false;
        private bool showCustomPaths = false;
        UnityEditor.IMGUI.Controls.SearchField searchField = null;
        PropertyBinder[] properties = new PropertyBinder[0];
        private string propertyFilter = "";
        private PropertyEditWindow propertyWindow = null;
        List<int> selectedProperties = new List<int>();
        private string relativePath = "";
#if DREAMTECK_SPLINES
        private SplineComputer[] splines = new SplineComputer[0];
#endif

        private LevelSegment[] allSegments = new LevelSegment[0];
        private LevelSegment[] sceneSegments = new LevelSegment[0];
        private GUIStyle boxStyle = null;

        int selectedPath = -1;
        int renameCustomPath = -1;
        LevelSegmentCustomPathEditor pathEditor = null;
        int[] laneIndices = new int[0];
        string[] laneNames = new string[0];

        private bool debugFoldout = false;
        EditorGUIEvents input = new EditorGUIEvents();

        public static EditorWindow GetWindowByName(string pName)
        {
            Object[] objectList = Resources.FindObjectsOfTypeAll(typeof(EditorWindow));
            foreach (Object obj in objectList)
            {
                if (obj.GetType().ToString() == pName)
                    return ((EditorWindow)obj);
            }
            return null;
        }

        private void Awake()
        {
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += DuringSceneGUI;
#endif
            GetSegments();
            if (Application.isPlaying) return;
            for (int i = 0; i < allSegments.Length; i++) allSegments[i].UpdateReferences();
            Undo.undoRedoPerformed += OnUndoRedo;
            PrefabStage.prefabStageClosing -= OnSavingPrefab;
            PrefabStage.prefabStageClosing += OnSavingPrefab;
        }

#if !UNITY_2019_1_OR_NEWER
        protected void OnSceneGUI()
        {
            DuringSceneGUI(SceneView.currentDrawingSceneView);
        }
#endif

        private void OnDestroy()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
#if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= DuringSceneGUI;
#endif
        }

        void OnUndoRedo()
        {
            selectedPath = -1;
            pathEditor = null;
            LevelSegment segment = (LevelSegment)target;
        }

        void OnSavingPrefab(PrefabStage stage)
        {
            LevelSegment segment = stage.prefabContentsRoot.GetComponent<LevelSegment>();
            if (segment != null)
            {
                segment.EditorPack();
#if UNITY_2020_1_OR_NEWER
                PrefabUtility.SaveAsPrefabAsset(segment.gameObject, stage.assetPath);
#else
                PrefabUtility.SaveAsPrefabAsset(segment.gameObject, stage.prefabAssetPath);
#endif
            }
            PrefabStage.prefabStageClosing -= OnSavingPrefab;
        }

        void GetSegments()
        {
            if (allSegments.Length != targets.Length) allSegments = new LevelSegment[targets.Length];
            for (int i = 0; i < targets.Length; i++) allSegments[i] = (LevelSegment)targets[i];
            List<LevelSegment> sceneSegmentsList = new List<LevelSegment>();
            for (int i = 0; i < allSegments.Length; i++)
            {
                sceneSegmentsList.Add(allSegments[i]);
            }
            sceneSegments = sceneSegmentsList.ToArray();
            //Unpack the scene segments only
            if (!Application.isPlaying)
            {
                for (int i = 0; i < sceneSegments.Length; i++)
                {
                    if (!sceneSegments[i].unpacked) sceneSegments[i].EditorUnpack();
                }
            }

#if DREAMTECK_SPLINES
            List<SplineComputer> comps = new List<SplineComputer>();
            for (int i = 0; i < sceneSegments.Length; i++)
            {
                List<Transform> children = new List<Transform>();
                SceneUtility.GetChildrenRecursively(sceneSegments[i].transform, ref children);
                for (int j = 1; j < children.Count; j++)
                {
                    SplineComputer comp = children[j].GetComponent<SplineComputer>();
                    if (comp != null)
                    {
                        comps.Add(comp);
                        //SplineDrawer.RegisterComputer(comp);
                    }
                }
            }
            splines = comps.ToArray();
#endif
        }

        bool IsSceneObject(GameObject obj)
        {
            Object parentObject = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            PrefabAssetType type = PrefabUtility.GetPrefabAssetType(obj);
            if(type == PrefabAssetType.Regular && type == PrefabAssetType.Variant) return false;
            if (parentObject == null) return true;
            return !AssetDatabase.Contains(parentObject);
        }

        int GetPropertyIndex(LevelSegment.ObjectProperty[] properties, LevelSegment.ObjectProperty property)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                if (property == properties[i]) return i;
            }
            return 0;
        }

        void ObjectPropertiesUI(PropertyBinder[] binders, LevelSegment.ObjectProperty[] properties)
        {
            input.Update();
            for (int i = 0; i < binders.Length; i++)
            {
                LevelSegment.ObjectProperty property = properties[binders[i].index];
                if (selectedProperties.Contains(binders[i].index))
                {
                    GUI.backgroundColor = ForeverPrefs.highlightColor;
                    GUI.contentColor = ForeverPrefs.highlightContentColor;
                }
                else
                {
                    if (property.extrusionSettings.ignore) GUI.backgroundColor = Color.gray;
                    else GUI.backgroundColor = DreamteckEditorGUI.lightColor;
                    GUI.contentColor = new Color(1f, 1f, 1f, 0.8f);
                }
                GUILayout.BeginVertical(boxStyle);
                EditorGUILayout.LabelField(i + "   " + binders[i].name);
                GUILayout.EndVertical();
                Rect lastRect = GUILayoutUtility.GetLastRect();
                lastRect.width -= 30;
                if (lastRect.Contains(Event.current.mousePosition) && input.mouseLeft)
                {
                    if (Event.current.shift)
                    {
                        if (selectedProperties.Count == 0) selectedProperties.Add(binders[i].index);
                        else
                        {
                            if (i < selectedProperties[0])
                            {
                                for (int n = selectedProperties[0] - 1; n >= i; n--)
                                {
                                    if (!selectedProperties.Contains(binders[n].index)) selectedProperties.Add(binders[n].index);
                                }
                            }
                            else
                            {
                                for (int n = selectedProperties[0] + 1; n <= i; n++)
                                {
                                    if (!selectedProperties.Contains(binders[n].index)) selectedProperties.Add(binders[n].index);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (Event.current.control)
                        {
                            if (!selectedProperties.Contains(binders[i].index)) selectedProperties.Add(binders[i].index);
                        }
                        else
                        {
                            selectedProperties.Clear();
                            selectedProperties.Add(binders[i].index);
                        }

                    }
                    Repaint();
                    if (propertyWindow != null) propertyWindow.Repaint();
                    SceneView.RepaintAll();
                }
                lastRect.width += 30;
            }
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
#if DREAMTECK_SPLINES
            for (int i = 0; i < splines.Length; i++)
            {
                //if (splines[i] != null) SplineDrawer.UnregisterComputer(splines[i]);
            }
#endif
            if (propertyWindow != null) propertyWindow.Close();
        }

        private bool IsObjectPrefabInstance(GameObject obj)
        {
            return PrefabUtility.GetPrefabInstanceStatus(obj) == PrefabInstanceStatus.Connected;
        }

        private void WritePrefabs(bool forceCopy = false)
        {
            for (int i = 0; i < allSegments.Length; i++) Write(allSegments[i], forceCopy);
        }

        void Write(LevelSegment segment, bool forceCopy)
        {
            Vector3 scale = segment.transform.localScale;
            if(!Mathf.Approximately(scale.x, 1f) || !Mathf.Approximately(scale.y, 1f) || !Mathf.Approximately(scale.z, 1f))
            {
                segment.transform.localScale = Vector3.one;
                EditorUtility.DisplayDialog("Non-uniform scale warning", "The level semgent has a scale different than 1,1,1. During generation the scale will be reset to 1,1,1. If you intend scaling, leave the root object unscaled and scale the children instead.", "OK");
            }
            //Check to see if we are currently editing the prefab and if yes (2018.3), just pack everything without rewriting
            bool isPrefabInstance = false;
            Object prefabParent = null;
            PrefabInstanceStatus instanceStatus = PrefabUtility.GetPrefabInstanceStatus(segment.gameObject);
            isPrefabInstance = instanceStatus == PrefabInstanceStatus.Connected;
            if (isPrefabInstance) prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(segment.gameObject);

            if (!forceCopy && prefabParent != null)
            {
                //ForeverSegmentBackup.BackupAsset(AssetDatabase.GetAssetPath(prefabParent));

                segment.EditorPack();
#if DREAMTECK_SPLINES
                for (int i = 0; i < splines.Length; i++)
                {
                    if (splines[i] != null)
                    {
                        //SplineDrawer.UnregisterComputer(splines[i]);
                    }
                }
#endif
                Selection.activeGameObject = PrefabUtility.SaveAsPrefabAsset(segment.gameObject, AssetDatabase.GetAssetPath(prefabParent));
                Undo.DestroyObjectImmediate(segment.gameObject);
            }
            else
            {
                relativePath = EditorPrefs.GetString("LevelSegmentEditor.relativePath", "/");
                if (prefabParent != null)
                {
                    relativePath = AssetDatabase.GetAssetPath(prefabParent);
                    if(relativePath.StartsWith("Assets")) relativePath = relativePath.Substring("Assets".Length);
                    relativePath = System.IO.Path.GetDirectoryName(relativePath);
                }
                string path = EditorUtility.SaveFilePanel("Save Prefab", Application.dataPath + relativePath, segment.name, "prefab");
                if (path.StartsWith(Application.dataPath) && System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(path)))
                {
                    relativePath = path.Substring(Application.dataPath.Length);
                    segment.EditorPack();
#if DREAMTECK_SPLINES
                    for (int i = 0; i < splines.Length; i++)
                    {
                        if (splines[i] != null)
                        {
                            //SplineDrawer.UnregisterComputer(splines[i]);
                        }
                    }
#endif
                    if (isPrefabInstance) PrefabUtility.UnpackPrefabInstance(segment.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                    PrefabUtility.SaveAsPrefabAsset(segment.gameObject, "Assets" + relativePath);
                    Undo.DestroyObjectImmediate(segment.gameObject);
                    EditorPrefs.SetString("LevelSegmentEditor.relativePath", System.IO.Path.GetDirectoryName(relativePath));
                } else
                {
                    if (path != "" && !path.StartsWith(Application.dataPath)) EditorUtility.DisplayDialog("Path Error", "Please select a path inside this project's Assets folder", "OK");
                }
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.GetStyle("box"));
                boxStyle.normal.background = DreamteckEditorGUI.blankImage;
                boxStyle.margin = new RectOffset(0, 0, 0, 2);
            }

            string saveText = "Save";
            string saveAsText = "Save As";
            if (allSegments.Length > 1)
            {
                saveText = "Save All";
                saveAsText = "Save All As";
            }
            if (sceneSegments.Length > 0 && !Application.isPlaying && !PrefabStageCheck.open)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(saveText, GUILayout.Height(40)))
                {
                    WritePrefabs();
                    return;
                }
                if (GUILayout.Button(saveAsText, GUILayout.Height(40), GUILayout.Width(70)))
                {
                    WritePrefabs(true);
                    return;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (allSegments.Length > 1)
            {
                EditorGUILayout.HelpBox("Property editing unavailable with multiple selection", MessageType.Info);
                return;
            }

            LevelSegment segment = sceneSegments[0];

            segment.type = (LevelSegment.Type)EditorGUILayout.EnumPopup("Type", segment.type);
            if (segment.type == LevelSegment.Type.Extruded)
            {
                segment.axis = (LevelSegment.Axis)EditorGUILayout.EnumPopup("Extrude Axis", segment.axis);
                ExtrusionUI();
            } else
            {
                EditorGUILayout.BeginHorizontal();
                segment.customEntrance = (Transform)EditorGUILayout.ObjectField("Entrance", segment.customEntrance, typeof(Transform), true);
                if (segment.customEntrance == null)
                {
                    if (GUILayout.Button("Create", GUILayout.Width(50)))
                    {
                        GameObject go = new GameObject("Entrance");
                        go.transform.parent = segment.transform;
                        segment.customEntrance = go.transform;
                    }
                }
                else if (!IsChildOrSubchildOf(segment.customEntrance, segment.transform))
                {
                    Debug.LogError(segment.customEntrance.name + " must be a child of " + segment.name);
                    segment.customEntrance = null;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                segment.customExit = (Transform)EditorGUILayout.ObjectField("Exit", segment.customExit, typeof(Transform), true);
                if (segment.customExit == null)
                {
                    if (GUILayout.Button("Create", GUILayout.Width(50)))
                    {
                        GameObject go = new GameObject("Exit");
                        go.transform.parent = segment.transform;
                        segment.customExit = go.transform;
                    }
                }
                else if (!IsChildOrSubchildOf(segment.customExit, segment.transform))
                {
                    Debug.LogError(segment.customExit.name + " must be a child of " + segment.name);
                    segment.customExit = null;
                }
                EditorGUILayout.EndHorizontal();
                segment.customKeepUpright = EditorGUILayout.Toggle("Keep Upright", segment.customKeepUpright);
            }
            EditorGUILayout.Space();

            int childCount = 0;
            TransformUtility.GetChildCount(segment.transform, ref childCount);
            if (segment.editorChildCount != childCount && !Application.isPlaying)
            {
                segment.UpdateReferences();
                selectedProperties.Clear();
            }

            CustomPathUI();
            
            EditorGUILayout.Space();
            debugFoldout = EditorGUILayout.Foldout(debugFoldout, "Debug");
            if (debugFoldout)
            {
                if (!Application.isPlaying) segment.drawBounds = EditorGUILayout.Toggle("Draw Bounds", segment.drawBounds);
                if (segment.type == LevelSegment.Type.Custom) segment.drawEntranceAndExit = EditorGUILayout.Toggle("Draw Entrance / Exit", segment.drawEntranceAndExit);
                segment.drawGeneratedSpline = EditorGUILayout.Toggle("Draw Generated Points", segment.drawGeneratedSpline);
                segment.drawGeneratedSamples = EditorGUILayout.Toggle("Draw Generated Samples", segment.drawGeneratedSamples);
                if (segment.drawGeneratedSamples)
                {
                    EditorGUI.indentLevel++;
                    segment.drawSampleScale = EditorGUILayout.FloatField("Sample Scale", segment.drawSampleScale);
                    EditorGUI.indentLevel--;
                }
                segment.drawCustomPaths = EditorGUILayout.Toggle("Draw Custom Paths", segment.drawCustomPaths);
                EditorGUILayout.HelpBox(segment.GetBounds().size.ToString(), MessageType.Info);
            }
        }

        void CustomPathUI()
        {
            LevelSegment segment = sceneSegments[0];
            showCustomPaths = EditorGUILayout.Foldout(showCustomPaths, "Custom Paths (" + segment.customPaths.Length + ")");
            if (showCustomPaths)
            {
                Undo.RecordObject(segment, "Edit Custom Paths");
                if (segment.type == LevelSegment.Type.Custom)
                {
                    if(laneIndices.Length != segment.customPaths.Length + 1)
                    {
                        laneIndices = new int[segment.customPaths.Length + 1];
                        laneNames = new string[segment.customPaths.Length + 1];
                    }
                    laneIndices[0] = -1;
                    laneNames[0] = "None";
                    for (int i = 0; i < segment.customPaths.Length; i++)
                    {
                        laneNames[i + 1] = (i + 1) + " - " + segment.customPaths[i].name;
                        laneIndices[i + 1] = i;
                    }
                    segment.customMainPath = EditorGUILayout.IntPopup("Main Path", segment.customMainPath, laneNames, laneIndices);
                }

                input.Update();
                GUI.backgroundColor = DreamteckEditorGUI.lightColor;
                for (int i = 0; i < segment.customPaths.Length; i++)
                {
                    GUILayout.BeginVertical(boxStyle);
                    EditorGUILayout.BeginHorizontal();
                    segment.customPaths[i].color = EditorGUILayout.ColorField(segment.customPaths[i].color, GUILayout.Width(40));
                    if (renameCustomPath == i)
                    {
                        if (input.enterDown)
                        {
                            input.Use();
                            renameCustomPath = -1;
                        }
                        segment.customPaths[i].name = EditorGUILayout.TextField(segment.customPaths[i].name);
                    }
                    else
                    {
                        GUIStyle style = i == segment.customMainPath ? EditorStyles.boldLabel : EditorStyles.label;
                        EditorGUILayout.LabelField(segment.customPaths[i].name, style);
                    }
                    EditorGUILayout.EndHorizontal();
                    Rect lastRect = GUILayoutUtility.GetLastRect();

                    if (input.mouseRightDown)
                    {
                        if (lastRect.Contains(Event.current.mousePosition))
                        {
                            int index = i;
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("Close"), false, delegate { selectedPath = -1; pathEditor = null; Repaint(); SceneView.RepaintAll(); });
                            menu.AddItem(new GUIContent("Rename"), false, delegate { renameCustomPath = index; Repaint(); SceneView.RepaintAll(); });
                            menu.AddItem(new GUIContent("Duplicate"), false, delegate { ArrayUtility.Insert(ref segment.customPaths, index + 1, segment.customPaths[index].Copy()); Repaint(); SceneView.RepaintAll(); });
                            menu.AddSeparator("");
                            if (i == 0) menu.AddDisabledItem(new GUIContent("Move Up"));
                            else menu.AddItem(new GUIContent("Move Up"), false, delegate {
                                LevelSegment.LevelSegmentPath temp = segment.customPaths[index];
                                segment.customPaths[index] = segment.customPaths[index - 1];
                                segment.customPaths[index - 1] = temp;
                                if (selectedPath == index) selectedPath--;
                                Repaint();
                                SceneView.RepaintAll();
                            });
                            if (i == segment.customPaths.Length - 1) menu.AddDisabledItem(new GUIContent("Move Down"));
                            else menu.AddItem(new GUIContent("Move Down"), false, delegate {
                                LevelSegment.LevelSegmentPath temp = segment.customPaths[index];
                                segment.customPaths[index] = segment.customPaths[index + 1];
                                segment.customPaths[index + 1] = temp;
                                if (selectedPath == index) selectedPath++;
                                Repaint();
                                SceneView.RepaintAll();
                            });
                            menu.AddSeparator("");
                            menu.AddItem(new GUIContent("Delete"), false, delegate { segment.RemoveCustomPath(index); selectedPath = -1; pathEditor = null; Repaint(); SceneView.RepaintAll(); });
                            menu.ShowAsContext();
                        }
                    }

                    if (selectedPath == i && pathEditor != null)
                    {
                        EditorGUILayout.Space();
                        pathEditor.DrawInspector();
                    }

                    GUILayout.EndVertical();
                    lastRect = GUILayoutUtility.GetLastRect();

                    if (input.mouseLeftDown)
                    {
                        if (lastRect.Contains(Event.current.mousePosition))
                        {
                            selectedPath = i;
                            pathEditor = new LevelSegmentCustomPathEditor(this, segment, i);
                            Repaint();
                            SceneView.RepaintAll();
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
                if(GUILayout.Button("Add Path"))
                {
                    segment.AddCustomPath("Lane " + (segment.customPaths.Length + 1));
                    Repaint();
                    SceneView.RepaintAll();
                }


            } else
            {
                renameCustomPath = -1;
                selectedPath = -1;
                pathEditor = null;
            }
            if (GUI.changed)
            {
                SceneView.RepaintAll();
            }
        }

        void ExtrusionUI()
        {
            LevelSegment segment = (LevelSegment)target;
            showProperties = EditorGUILayout.Foldout(showProperties, "Objects (" + segment.objectProperties.Length + ")");
            if (showProperties)
            {
                GUI.color = Color.clear;
                GUILayout.Box("", GUILayout.Width(Screen.width - 50));
                GUI.color = Color.white;
                if (searchField == null) searchField = new UnityEditor.IMGUI.Controls.SearchField();
                string lastFilter = propertyFilter;
                propertyFilter = searchField.OnGUI(GUILayoutUtility.GetLastRect(), propertyFilter);
                if (lastFilter != propertyFilter)
                {
                    List<PropertyBinder> found = new List<PropertyBinder>();
                    for (int i = 0; i < segment.objectProperties.Length; i++)
                    {
                        if (segment.objectProperties[i].transform.name.ToLower().Contains(propertyFilter.ToLower())) found.Add(new PropertyBinder(i, segment.objectProperties[i].transform.name));
                    }
                    properties = found.ToArray();
                }
                else if (propertyFilter == "")
                {
                    if (properties.Length != segment.objectProperties.Length) properties = new PropertyBinder[segment.objectProperties.Length];
                    for (int i = 0; i < segment.objectProperties.Length; i++)
                    {
                        if(properties[i] == null) properties[i] = new PropertyBinder(i, segment.objectProperties[i].transform.name);
                        else
                        {
                            properties[i].name = segment.objectProperties[i].transform.name;
                            properties[i].index = i;
                        }
                    }
                }

                if (selectedProperties.Count > 0)
                {
                    if (propertyWindow == null)
                    {
                        propertyWindow = EditorWindow.GetWindow<PropertyEditWindow>(true);
                        propertyWindow.selectedProperties = selectedProperties;
                        EditorWindow inspectorWindow = GetWindowByName("UnityEditor.InspectorWindow");
                        if (inspectorWindow != null)
                        {
                            propertyWindow.Init(new Vector2(inspectorWindow.position.x, inspectorWindow.position.y + 250), segment, this);
                        }
                        else
                        {
                            propertyWindow.Init(new Vector2(2560 - Screen.width, 1080 / 2), segment, this);
                        }
                    }
                }
                ObjectPropertiesUI(properties, segment.objectProperties);
                if (selectedProperties.Count > 0)
                {
                    if (Event.current.type == EventType.KeyDown)
                    {
                        if (Event.current.keyCode == KeyCode.DownArrow)
                        {
                            if (selectedProperties.Count > 1)
                            {
                                int temp = selectedProperties[selectedProperties.Count - 1];
                                selectedProperties.Clear();
                                selectedProperties.Add(temp);
                            }
                            selectedProperties[0]++;
                        }
                        if (Event.current.keyCode == KeyCode.UpArrow)
                        {
                            if (selectedProperties.Count > 1)
                            {
                                int temp = selectedProperties[0];
                                selectedProperties.Clear();
                                selectedProperties.Add(temp);
                            }
                            selectedProperties[0]--;
                        }
                        if (selectedProperties[0] < 0) selectedProperties[0] = 0;
                        if (selectedProperties[0] >= segment.objectProperties.Length) selectedProperties[0] = segment.objectProperties.Length - 1;
                        Repaint();
                        if (propertyWindow != null) propertyWindow.Repaint();
                        SceneView.RepaintAll();
                        Event.current.Use();
                    }
                }
                else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
                {
                    selectedProperties.Clear();
                    selectedProperties.Add(0);
                }
                GUI.color = Color.white;
            }
        }

        bool IsChildOrSubchildOf(Transform child, Transform parent)
        {
            Transform current = child;
            while (current.parent != null)
            {
                if (current.parent == parent) return true;
                current = current.parent;
            }
            return false;
        }

        void DuringSceneGUI(SceneView currentSceneView)
        {
#if DREAMTECK_SPLINES
            for (int i = 0; i < splines.Length; i++)
            {
                if (splines[i] != null) DSSplineDrawer.DrawSplineComputer(splines[i]);
            }
#endif

            if (Application.isPlaying)
            {
                for (int i = 0; i < sceneSegments.Length; i++)
                {
                    if (sceneSegments[i].drawCustomPaths)
                    {
                        LevelSegmentDebug.DrawCustomPaths(sceneSegments[i]);
                    }
                    if (sceneSegments[i].drawGeneratedSpline)
                    {
                        if (sceneSegments[i].type == LevelSegment.Type.Extruded)
                        {
                            LevelSegmentDebug.DrawGeneratedSpline(sceneSegments[i]);
                        }
                    }
                    if (sceneSegments[i].drawGeneratedSamples)
                    {
                        LevelSegmentDebug.DrawGeneratedSamples(sceneSegments[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < sceneSegments.Length; i++)
                {
                    if (sceneSegments[i].drawCustomPaths) LevelSegmentDebug.DrawCustomPaths(sceneSegments[i]);
                    if (sceneSegments[i].type == LevelSegment.Type.Custom) continue;
                    if (sceneSegments[i].drawBounds) LevelSegmentDebug.DrawBounds(sceneSegments[i]);
                }

                if (sceneSegments.Length == 1 && selectedProperties.Count > 0)
                {
                    Handles.BeginGUI();
                    for (int i = 0; i < selectedProperties.Count; i++)
                    {
                        Vector2 screenPosition = HandleUtility.WorldToGUIPoint(sceneSegments[0].objectProperties[selectedProperties[i]].transform.transform.position);
                        DreamteckEditorGUI.Label(new Rect(screenPosition.x - 120 + sceneSegments[0].objectProperties[selectedProperties[i]].transform.transform.name.Length * 4, screenPosition.y, 120, 25), sceneSegments[0].objectProperties[selectedProperties[i]].transform.transform.name);
                    }
                    Handles.EndGUI();
                }
            }
            if (pathEditor != null)
            {
                pathEditor.DrawScene(SceneView.currentDrawingSceneView);
            }

            for (int i = 0; i < sceneSegments.Length; i++)
            {
                if (!sceneSegments[i].drawEntranceAndExit) continue;
                if(sceneSegments[i].type == LevelSegment.Type.Custom)
                {
                    if (sceneSegments[i].customEntrance != null)
                    {
                        float handleSize = HandleUtility.GetHandleSize(sceneSegments[i].customEntrance.position);
                        Handles.color = ForeverPrefs.entranceColor;
                        Handles.DrawSolidDisc(sceneSegments[i].customEntrance.position, Camera.current.transform.position - sceneSegments[i].customEntrance.position, handleSize * 0.1f);
                        Handles.ArrowHandleCap(0, sceneSegments[i].customEntrance.position, sceneSegments[i].customEntrance.rotation, handleSize * 0.5f, EventType.Repaint);
                        Handles.Label(sceneSegments[i].customEntrance.position + Camera.current.transform.up * handleSize * 0.3f, "Entrance");
                    }
                    if (sceneSegments[i].customExit != null)
                    {
                        Handles.color = ForeverPrefs.exitColor;
                        float handleSize = HandleUtility.GetHandleSize(sceneSegments[i].customExit.position);
                        Handles.DrawSolidDisc(sceneSegments[i].customExit.position, Camera.current.transform.position - sceneSegments[i].customExit.position, handleSize * 0.1f);
                        Handles.ArrowHandleCap(0, sceneSegments[i].customExit.position, sceneSegments[i].customExit.rotation, handleSize * 0.5f, EventType.Repaint);
                        Handles.Label(sceneSegments[i].customExit.position + Camera.current.transform.up * HandleUtility.GetHandleSize(sceneSegments[i].customExit.position) * 0.3f, "Exit");
                    } 
                }
            }
        }

    }
}
