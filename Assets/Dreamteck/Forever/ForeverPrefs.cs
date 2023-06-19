#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Dreamteck.Forever
{
    public static class ForeverPrefs
    {
        public static LevelSegment.Type newSegmentType = LevelSegment.Type.Extruded;

        public static bool extrudeApplyScale = false;
        public static bool extrudeApplyRotation = true;
        public static bool extrudeBendMesh = false;
        public static bool extrudeBendSprite = false;
        public static bool extrudeApplyMeshColors = false;
        public static ExtrusionSettings.MeshColliderHandling extrudeMeshColliderHandle = ExtrusionSettings.MeshColliderHandling.Bypass;
        public static ExtrusionSettings.BoundsInclusion extrudeBoundsInclusion = (ExtrusionSettings.BoundsInclusion)~0;
#if DREAMTECK_SPLINES
        public static bool extrudeExtrudeSpline = false;
#endif
        public static float debugAlpha = 0.75f;
        public static Color debugPointColor = Color.black;
        public static Color debugEntranceColor = Color.green;
        public static Color debugExitColor = Color.red;



        public static Color pointColor
        {
            get
            {
                Color col = debugPointColor;
                col.a = debugAlpha;
                return col;
            }
        }
        public static Color entranceColor
        {
            get
            {
                Color col = debugEntranceColor;
                col.a = debugAlpha;
                return col;
            }
        }
        public static Color exitColor
        {
            get
            {
                Color col = debugExitColor;
                col.a = debugAlpha;
                return col;
            }
        }

        public static Color highlightColor = Color.white;
        public static Color highlightContentColor = new Color(1f, 1f, 1f, 0.95f);


        static ForeverPrefs()
        {
            LoadPrefs();
        }

#if UNITY_2019_1_OR_NEWER
        [SettingsProvider]
        public static SettingsProvider ForeverSettingsProvider()
        {
            SettingsProvider provider = new SettingsProvider("Dreamteck/Forever", SettingsScope.User)
            {
                label = "Forever",
                guiHandler = (searchContext) =>
                {
                    OnGUI();
                },

                keywords = new HashSet<string>(new[] { "Forever", "Endless", "Dreamteck" })
            };

            return provider;
        }
#else
         [PreferenceItem("Forever")]
#endif
        public static void OnGUI()
        {
            EditorGUILayout.LabelField("Newly Created Segments", EditorStyles.boldLabel);
            newSegmentType = (LevelSegment.Type)EditorGUILayout.EnumPopup("Type", newSegmentType);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Extrusion Defaults", EditorStyles.boldLabel);
#if UNITY_2017_4_OR_NEWER
            extrudeBoundsInclusion = (ExtrusionSettings.BoundsInclusion)EditorGUILayout.EnumFlagsField("Bounds Inclusion", extrudeBoundsInclusion);
#else
            extrudeBoundsInclusion = (ExtrusionSettings.BoundsInclusion)EditorGUILayout.EnumMaskField("Bounds Inclusion", extrudeBoundsInclusion);
#endif
            extrudeApplyRotation = EditorGUILayout.Toggle("Apply Rotation", extrudeApplyRotation);
            extrudeApplyScale = EditorGUILayout.Toggle("Apply Scale", extrudeApplyScale);
            extrudeBendSprite = EditorGUILayout.Toggle("Extrude Meshes", extrudeBendSprite);
            if(extrudeBendSprite) extrudeApplyMeshColors = EditorGUILayout.Toggle("Apply Mesh Colors", extrudeApplyMeshColors);
            extrudeMeshColliderHandle = (ExtrusionSettings.MeshColliderHandling)EditorGUILayout.EnumPopup("Mesh Collider Handling", extrudeMeshColliderHandle);
#if DREAMTECK_SPLINES
            extrudeExtrudeSpline = EditorGUILayout.Toggle("Extrude Splines", extrudeExtrudeSpline);
#endif
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Handles", EditorStyles.boldLabel);
            debugAlpha = EditorGUILayout.Slider("Alpha", debugAlpha, 0.1f, 1f);
            debugPointColor = EditorGUILayout.ColorField("Point Visualization Color", debugPointColor);
            debugEntranceColor = EditorGUILayout.ColorField("Entrance Visualization Color", debugEntranceColor);
            debugExitColor = EditorGUILayout.ColorField("Exit Visualization Color", debugExitColor);
            EditorGUILayout.LabelField("Editor GUI", EditorStyles.boldLabel);
            highlightColor = EditorGUILayout.ColorField("Highlight color", highlightColor);
            highlightContentColor = EditorGUILayout.ColorField("Highlight content color", highlightContentColor);

            if (GUILayout.Button("Use Defaults", GUILayout.Width(120)))
            {
                newSegmentType = LevelSegment.Type.Extruded;
                debugPointColor = Color.black;
                debugEntranceColor = new Color(0f, 0.887f, 0.106f, 1f);
                debugExitColor = new Color(0.887f, 0f, 0.25f, 1f);
                highlightColor = new Color(0.4117647f, 0.8705883f, 0.007843138f, 1f);
                highlightContentColor = new Color(1f, 1f, 1f, 0.95f);
                extrudeApplyScale = false;
                extrudeApplyRotation = true;
                extrudeBendSprite = false;
                extrudeApplyMeshColors = false;
                extrudeBoundsInclusion = (ExtrusionSettings.BoundsInclusion)~0;
                extrudeMeshColliderHandle = ExtrusionSettings.MeshColliderHandling.Bypass;
#if DREAMTECK_SPLINES
                extrudeExtrudeSpline = false;
#endif
                SavePrefs();
            }
            if (GUI.changed) SavePrefs();
        }

        public static void LoadPrefs()
        {
            newSegmentType = (LevelSegment.Type)EditorPrefs.GetInt("Dreamteck.Forever.newSegmentType", 0);
            debugEntranceColor = LoadColor("Dreamteck.Forever.debugEntranceColor", new Color(0f, 0.887f, 0.106f, 1f));
            debugExitColor = LoadColor("Dreamteck.Forever.debugExitColor", new Color(0.887f, 0f, 0.25f, 1f));
            debugPointColor = LoadColor("Dreamteck.Forever.debugPointColor", Color.black);
            highlightColor = LoadColor("Dreamteck.Forever.highlightColor", new Color(0.4117647f, 0.8705883f, 0.007843138f, 1f));
            highlightContentColor = LoadColor("Dreamteck.Forever.highlightContentColor", new Color(1f, 1f, 1f, 0.95f));
            extrudeMeshColliderHandle = (ExtrusionSettings.MeshColliderHandling)EditorPrefs.GetInt("Dreamteck.Forever.extrudeMeshColliderHandle", 0);
            extrudeBoundsInclusion = (ExtrusionSettings.BoundsInclusion)EditorPrefs.GetInt("Dreamteck.Forever.extrudeBoundsInclusion", ~0);
            extrudeApplyScale = EditorPrefs.GetBool("Dreamteck.Forever.extrudeApplyScale", false);
            extrudeApplyRotation = EditorPrefs.GetBool("Dreamteck.Forever.extrudeApplyRotation", true);
            extrudeBendSprite = EditorPrefs.GetBool("Dreamteck.Forever.extrudeExtrudeMesh", false);
            extrudeApplyMeshColors = EditorPrefs.GetBool("Dreamteck.Forever.extrudeApplyMeshColors", false);
#if DREAMTECK_SPLINES
            extrudeExtrudeSpline = EditorPrefs.GetBool("Dreamteck.Forever.extrudeExtrudeSpline", false);
#endif
        }

        private static Color LoadColor(string name, Color defaultValue)
        {
            Color col = Color.white;
            string colorString = EditorPrefs.GetString(name, defaultValue.r+":"+defaultValue.g+ ":" + defaultValue.b+ ":" + defaultValue.a);
            string[] elements = colorString.Split(':');
            if (elements.Length < 4) return col;
            float r = 0f, g = 0f, b = 0f, a = 0f;
            float.TryParse(elements[0], out r);
            float.TryParse(elements[1], out g);
            float.TryParse(elements[2], out b);
            float.TryParse(elements[3], out a);
            col = new Color(r, g, b, a);
            return col;
        }

        public static void SavePrefs()
        {
            EditorPrefs.SetFloat("Dreamteck.Forever.debugAlpha", debugAlpha);
            EditorPrefs.SetString("Dreamteck.Forever.debugPointColor", debugPointColor.r+ ":" + debugPointColor.g+ ":" + debugPointColor.b+ ":" + debugPointColor.a);
            EditorPrefs.SetString("Dreamteck.Forever.debugEntranceColor", debugEntranceColor.r+ ":" + debugEntranceColor.g+ ":" + debugEntranceColor.b+ ":" + debugEntranceColor.a);
            EditorPrefs.SetString("Dreamteck.Forever.debugExitColor", debugExitColor.r+ ":" + debugExitColor.g+ ":" + debugExitColor.b+ ":" + debugExitColor.a);
            EditorPrefs.SetString("Dreamteck.Forever.highlightColor", highlightColor.r + ":" + highlightColor.g + ":" + highlightColor.b + ":" + highlightColor.a);
            EditorPrefs.SetString("Dreamteck.Forever.highlightContentColor", highlightContentColor.r + "," + highlightContentColor.g + ":" + highlightContentColor.b + ":" + highlightContentColor.a);
            EditorPrefs.SetInt("Dreamteck.Forever.newSegmentType", (int)newSegmentType);
            EditorPrefs.SetInt("Dreamteck.Forever.extrudeMeshColliderHandle", (int)extrudeMeshColliderHandle);
            EditorPrefs.SetInt("Dreamteck.Forever.extrudeBoundsInclusion", (int)extrudeBoundsInclusion);
            EditorPrefs.SetBool("Dreamteck.Forever.extrudeApplyScale", extrudeApplyScale);
            EditorPrefs.SetBool("Dreamteck.Forever.extrudeApplyRotation", extrudeApplyRotation);
            EditorPrefs.SetBool("Dreamteck.Forever.extrudeExtrudeMesh", extrudeBendSprite);
            EditorPrefs.SetBool("Dreamteck.Forever.extrudeApplyMeshColors", extrudeApplyMeshColors);
#if DREAMTECK_SPLINES
            EditorPrefs.SetBool("Dreamteck.Forever.extrudeExtrudeSpline", extrudeExtrudeSpline);
#endif
        }
    }
}
#endif
