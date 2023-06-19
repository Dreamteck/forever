namespace Dreamteck.Forever.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(PreserveObjectsData))]
    public class PreserveObjectsDataEditor : Editor
    {
        private bool _showAssets = false;
        private Vector2 _assetsScroll = Vector2.zero;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            PreserveObjectsData preserveObjectsData = (PreserveObjectsData)target;
            _showAssets = EditorGUILayout.Foldout(_showAssets, preserveObjectsData.assetCollection.assets.Length + " Unique assets found");
            if (_showAssets)
            {
                _assetsScroll = GUILayout.BeginScrollView(_assetsScroll, GUILayout.MaxHeight(500));
                for (int i = 0; i < preserveObjectsData.assetCollection.assets.Length; i++)
                {
                    if(GUILayout.Button((i + 1) + ": " + preserveObjectsData.assetCollection.assets[i].asset.name))
                    {
                        Selection.activeObject = preserveObjectsData.assetCollection.assets[i].asset;
                    }
                }
                GUILayout.EndScrollView();
            }
        }
    }
}
