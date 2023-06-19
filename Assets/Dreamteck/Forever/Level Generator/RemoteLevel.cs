using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Dreamteck.Forever
{
    [AddComponentMenu("Dreamteck/Forever/Remote Level")]
    public class RemoteLevel : MonoBehaviour
    {
        [HideInInspector] public SegmentSequenceCollection sequenceCollection = new SegmentSequenceCollection();
        [SerializeField] [HideInInspector] private bool _usePooling = false;
        [SerializeField] [HideInInspector] 
        [Tooltip("If checked, the level will unload all unique registered assets when destroyed.")] 
        private bool _unloadAssetsOnDestroy = false;
        [SerializeField] [HideInInspector] private PreserveObjectsData _preventObjectsFromUnloading;
        [SerializeField] [HideInInspector] private UniqueAssetCollection _assetCollection = new UniqueAssetCollection();
        public bool usePooling
        {
            get { return _usePooling; }
        }

        public bool unloadAssetsOnDestroy
        {
            get { return _unloadAssetsOnDestroy; }
        }

        public UniqueAssetCollection assetCollection
        {
            get
            {
                return _assetCollection;
            }
        }

        public PreserveObjectsData preventObjectsFromUnloading
        {
            get; set;
        }

        private bool ContainsAsset(UniqueAssetCollection.QuickSearchAsset asset)
        {
            return _assetCollection.ContainsAsset(asset);
        }

        private void Awake()
        {
            LevelGenerator.instance.RegisterRemoteLevel(this);
        }

        private void OnDestroy()
        {
            if(_usePooling && _unloadAssetsOnDestroy && LevelGenerator.instance != null)
            {
                List<ForeverLevel> loaded = LevelGenerator.instance.loadedLevels;
                for (int i = loaded.Count-1; i >= 0 ; i--)
                {
                    if (loaded[i].associatedRemoteLevel == null)
                    {
                        loaded.RemoveAt(i);
                    }
                }

                int loadedIndex = 0;
                for (int i = 0; i < loaded.Count; i++)
                {
                    if(LevelGenerator.instance.loadedLevels[i].associatedRemoteLevel == this)
                    {
                        loadedIndex = i;
                        break;
                    }
                }

                for (int i = 0; i < _assetCollection.assets.Length; i++)
                {
                    bool isAssetUsed = false;
                    for (int j = loadedIndex + 1; j < loaded.Count; j++)
                    {
                        if (loaded[j].associatedRemoteLevel.ContainsAsset(_assetCollection.assets[i]))
                        {
                            isAssetUsed = true;
                            break;
                        }
                    }
                    if (!isAssetUsed)
                    {
                        Resources.UnloadAsset(_assetCollection.assets[i].asset);
                    }
                }
            }
            LevelGenerator.instance.UnregisterRemoteLevel(this);
        }

       

#if UNITY_EDITOR
        [ContextMenu("Editor cache")]
        public void EditorCacheAssets()
        {
            _assetCollection.ClearExtractedAssets();
            if (!_usePooling || !_unloadAssetsOnDestroy) return;
            _assetCollection.ClearExtractedAssets();
            foreach (Transform child in transform)
            {
                if (child == transform) continue;
                _assetCollection.ExtractUniqueAssets(child, _preventObjectsFromUnloading);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void EditorCacheAssets(Object obj)
        {
            _assetCollection.ExtractUniqueAssets(obj, _preventObjectsFromUnloading);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
