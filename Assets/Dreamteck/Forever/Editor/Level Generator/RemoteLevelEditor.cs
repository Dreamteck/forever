using System;
using System.Linq;
using System.Reflection;

namespace Dreamteck.Forever.Editor
{
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(RemoteLevel))]
    public class RemoteLevelEditor : Editor
    {
        private bool _showAssets = false;
        private Vector2 _assetsScroll = Vector2.zero;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            RemoteLevel remoteLvl = (RemoteLevel)target;
            serializedObject.Update();
            SerializedProperty usePooling = serializedObject.FindProperty("_usePooling");
            SerializedProperty unloadAssetsOnDestroy = serializedObject.FindProperty("_unloadAssetsOnDestroy");

            if (GUILayout.Button("Edit Sequence", GUILayout.Height(50)))
            {
                SequenceEditWindow window = EditorWindow.GetWindow<SequenceEditWindow>(true);
                window.Init(((RemoteLevel)target).sequenceCollection, target, OnApplySequences);
                window.onClose += OnSequenceWindowClosed;
            }
            EditorGUI.BeginChangeCheck();
            bool lastPooling = usePooling.boolValue;
            EditorGUILayout.PropertyField(usePooling);
            if (usePooling.boolValue)
            {
                
                EditorGUILayout.PropertyField(unloadAssetsOnDestroy);
                SerializedProperty preventObjectsFromUnloading = serializedObject.FindProperty("_preventObjectsFromUnloading");
                EditorGUILayout.PropertyField(preventObjectsFromUnloading);
                if (unloadAssetsOnDestroy.boolValue)
                {
                    _showAssets = EditorGUILayout.Foldout(_showAssets, remoteLvl.assetCollection.assets.Length + " Unique assets found");
                    if (_showAssets)
                    {
                        _assetsScroll = GUILayout.BeginScrollView(_assetsScroll, GUILayout.MaxHeight(500));
                        for (int i = 0; i < remoteLvl.assetCollection.assets.Length; i++)
                        {
                            if (GUILayout.Button((i + 1) + ": " + remoteLvl.assetCollection.assets[i].asset.name))
                            {
                                Selection.activeObject = remoteLvl.assetCollection.assets[i].asset;
                            }
                        }
                        GUILayout.EndScrollView();
                    }
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                remoteLvl.EditorCacheAssets();
                if (!Application.isPlaying && lastPooling != usePooling.boolValue)
                {
                    HandlePool(usePooling.boolValue);
                }
            }
        }

        private void OnSequenceWindowClosed()
        {
            if (!Application.isPlaying)
            {
                serializedObject.Update();
                SerializedProperty usePooling = serializedObject.FindProperty("_usePooling");
                HandlePool(usePooling.boolValue);
                RemoteLevel remoteLvl = (RemoteLevel)target;
                remoteLvl.EditorCacheAssets();
            }
        }

        private void HandlePool(bool usePool)
        {
            RemoteLevel remoteLvl = (RemoteLevel)target;
            for (int i = 0; i < remoteLvl.sequenceCollection.sequences.Length; i++)
            {
                if (usePool)
                {
                    remoteLvl.sequenceCollection.sequences[i].EditorDeployPool(remoteLvl.transform);
                }
                else
                {
                    remoteLvl.sequenceCollection.sequences[i].EditorDismantlePool();
                }
            }
        }

        private void OnApplySequences(SegmentSequence[] sequences)
        {
            RemoteLevel level = (RemoteLevel)target;
            level.sequenceCollection.sequences = sequences;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                serializedObject.Update();
                SerializedProperty usePooling = serializedObject.FindProperty("_usePooling");
                HandlePool(usePooling.boolValue);
                RemoteLevel remoteLvl = (RemoteLevel)target;
                remoteLvl.EditorCacheAssets();
            }
        }
    }
}
