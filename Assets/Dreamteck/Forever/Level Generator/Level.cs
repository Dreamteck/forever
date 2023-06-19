using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using System;

namespace Dreamteck.Forever
{
    /// <summary>
    /// Obsolete level class. No Longer used in Forever 1.10
    /// </summary>
    [System.Serializable]
    public class Level
    {
        public bool enabled = true;
        public string title = "";
        public SegmentSequenceCollection sequenceCollection = new SegmentSequenceCollection();
        public bool remoteSequence = false;
        public string remoteSceneName = "";
        public ThreadPriority loadingPriority = ThreadPriority.BelowNormal;
        public delegate void SequenceHandler(SegmentSequence sequence);
        public event SequenceHandler onSequenceEntered;
        private SegmentSequence lastSequence;

        private SegmentSequence[] sequences
        {
            get { return sequenceCollection.sequences; }
        }

        public bool isReady
        {
            get
            {
                return !remoteSequence || _isRemoteLoaded;
            }
        }

        private bool _isRemoteLoaded = false;

        public System.Collections.IEnumerator Load()
        {
            if (_isRemoteLoaded) yield break;
            Scene checkScene = SceneManager.GetSceneByName(remoteSceneName);
            if (!checkScene.isLoaded)
            {
                ThreadPriority lastPriority = Application.backgroundLoadingPriority;
                Application.backgroundLoadingPriority = loadingPriority;
                AsyncOperation async = SceneManager.LoadSceneAsync(remoteSceneName, LoadSceneMode.Additive);
                yield return async;
                Application.backgroundLoadingPriority = lastPriority;
            }
            RemoteLevel[] remoteLevels = UnityEngine.Object.FindObjectsOfType<RemoteLevel>();
            Scene scene = SceneManager.GetSceneByName(remoteSceneName);
            for (int i = 0; i < remoteLevels.Length; i++)
            {
                if (remoteLevels[i].gameObject.scene.path == scene.path)
                {
                    sequenceCollection = remoteLevels[i].sequenceCollection;
                    break;
                }
            }
            _isRemoteLoaded = true;
        }

        public System.Collections.IEnumerator Unload()
        {
            if (!_isRemoteLoaded)
            {
                yield break;
            }

            Scene checkScene = SceneManager.GetSceneByName(remoteSceneName);

            if (checkScene.isLoaded)
            {
                ThreadPriority lastPriority = Application.backgroundLoadingPriority;
                Application.backgroundLoadingPriority = loadingPriority;
                AsyncOperation async = SceneManager.UnloadSceneAsync(remoteSceneName);
                yield return async;
                Application.backgroundLoadingPriority = lastPriority;
            }

            lastSequence = null;
            sequenceCollection = null;
            _isRemoteLoaded = false;
        }

        public void UnloadImmediate()
        {
            if (!_isRemoteLoaded)
            {
                return;
            }

            Scene checkScene = SceneManager.GetSceneByName(remoteSceneName);
            if (checkScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(remoteSceneName);
            }

            lastSequence = null;
            sequenceCollection = null;
            _isRemoteLoaded = false;
        }

        public void Initialize()
        {
            lastSequence = null;
            for (int i = 0; i < sequences.Length; i++)
            {
                sequences[i].Initialize();
            }
        }

        public bool IsDone()
        {
            for (int i = 0; i < sequences.Length; i++)
            {
                if (!sequences[i].IsDone() && sequences[i].enabled) return false;
            }

            return true;
        }

        public void SkipSequence()
        {
            GetSequence().Stop();
        }

        public void GoToSequence(int index)
        {
            for (int i = 0; i < sequences.Length; i++)
            {
                if (i < index)
                {
                    if (!sequences[i].IsDone()) sequences[i].Stop();
                }
                else break;
            }
            for (int i = index; i < sequences.Length; i++) sequences[i].Initialize();

        }

        public LevelSegment InstantiateSegment()
        {
            SegmentSequence sequence = GetSequence();
            if (sequence == null)
            {
                throw new System.NullReferenceException(title + " has null sequence");
            }


            if (sequence != lastSequence)
            {
                if (onSequenceEntered != null) onSequenceEntered(sequence);
                lastSequence = sequence;
            }
            SegmentDefinition definition = sequence.Next();
            return definition.Instantiate();
        }

        private SegmentSequence GetSequence()
        {
            for (int i = 0; i < sequences.Length; i++)
            {
                if (sequences[i].enabled && !sequences[i].IsDone())
                {
                    return sequences[i];
                }
            }
            return null;
        }

        public Level Duplicate()
        {
            Level level = new Level();
            level.title = title;
            level.remoteSequence = remoteSequence;
            level.remoteSceneName = remoteSceneName;
            level.loadingPriority = loadingPriority;
            level.sequenceCollection = new SegmentSequenceCollection();
            level.sequenceCollection.sequences = new SegmentSequence[sequenceCollection.sequences.Length];
            for (int i = 0; i < sequenceCollection.sequences.Length; i++) level.sequenceCollection.sequences[i] = sequenceCollection.sequences[i].Duplicate();
            return level;
        }
    }
}
