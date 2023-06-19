using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Dreamteck.Forever
{
    [CreateAssetMenu(menuName = "Forever/Preserve Objects Data")]
    public class PreserveObjectsData : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private Object[] _objects = new Object[0];
        [SerializeField] [HideInInspector] private UniqueAssetCollection _assetCollection = new UniqueAssetCollection();
        public Object[] assets;

        public UniqueAssetCollection assetCollection
        {
            get { return _assetCollection; }
        }

        public bool ContainsAsset(UniqueAssetCollection.QuickSearchAsset asset)
        {
            return _assetCollection.ContainsAsset(asset);
        }

        public void OnAfterDeserialize()
        {
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            _assetCollection.ClearExtractedAssets();
            foreach (Object obj in _objects)
            {
                _assetCollection.ExtractUniqueAssets(obj);
            }
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Update All Remote Levels")]
        public void UpdateAllRemoteLevels()
        {
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < sceneCount; i++)
            {
                EditorUtility.DisplayProgressBar("Updating remote levels", (i + 1) + " / " + sceneCount, (float)i / (sceneCount - 1));
                try
                {
                    string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                    UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    GameObject[] rootObjects = scene.GetRootGameObjects();
                    bool save = false;
                    foreach(GameObject gameObject in rootObjects)
                    {
                        RemoteLevel level = gameObject.GetComponent<RemoteLevel>();
                        if(level == null)
                        {
                            continue;
                        }
                        Debug.Log("Updated " + gameObject.name + " in scene " + scene.name);
                        level.EditorCacheAssets();
                        save = true;
                    }
                    if (save)
                    {
                        EditorSceneManager.SaveScene(scene);
                    }
                    EditorSceneManager.CloseScene(scene, true);
                } catch (UnityException ex)
                {
                    Debug.LogException(ex);
                }
            }
            EditorUtility.ClearProgressBar();
        }
#endif
    }
}