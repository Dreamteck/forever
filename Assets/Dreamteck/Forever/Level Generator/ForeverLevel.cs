namespace Dreamteck.Forever
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    [CreateAssetMenu(menuName = "Forever/Level")]
    public class ForeverLevel : ScriptableObject
    {
        [SerializeField]
        private bool _enabled = true;
        [SerializeField]
        private LevelPathGenerator _pathGenerator;
        [SerializeField]
        [HideInInspector]
        private SegmentSequenceCollection _sequenceCollection = new SegmentSequenceCollection();
        [SerializeField]
        private bool _remoteSequence = false;
        [SerializeField]
        [HideInInspector]
        private string _remoteSceneName = "";
        [SerializeField]
        [HideInInspector]
        private ThreadPriority _loadingPriority = ThreadPriority.BelowNormal;

        public event System.Action<SegmentSequence> onSequenceEntered;

        public SegmentSequenceCollection sequenceCollection
        {
            get { return _sequenceCollection; }
        }

        private SegmentSequence[] sequences
        {
            get { return _sequenceCollection.sequences; }
        }

        public RemoteLevel associatedRemoteLevel
        {
            get { return _associatedRemoteLevel; }
        }

        public bool enabled { get { return _enabled; } set { _enabled = value; } }
        public bool remoteSequence { get { return _remoteSequence; } set { _remoteSequence = value; } }
        public string remoteSceneName { get { return _remoteSceneName; } set { _remoteSceneName = value; } }
        public LevelPathGenerator pathGenerator { get { return _pathGenerator; } set { _pathGenerator = value; } }
        public ThreadPriority loadingPriority { get { return _loadingPriority; } set { _loadingPriority = value; } }


        public bool isReady { get { return !_remoteSequence || _isRemoteLoaded; } }

        private bool _isRemoteLoaded = false;
        private SegmentSequence _lastSequence;
        private RemoteLevel _associatedRemoteLevel = null;

        public IEnumerator Load()
        {
            if (_isRemoteLoaded) yield break;
            Scene checkScene = SceneManager.GetSceneByName(_remoteSceneName);
            if (!checkScene.isLoaded)
            {
                ThreadPriority lastPriority = Application.backgroundLoadingPriority;
                Application.backgroundLoadingPriority = _loadingPriority;
                AsyncOperation async = SceneManager.LoadSceneAsync(_remoteSceneName, LoadSceneMode.Additive);
                yield return async;
                Application.backgroundLoadingPriority = lastPriority;
            }
            RemoteLevel[] remoteLevels = UnityEngine.Object.FindObjectsOfType<RemoteLevel>();
            Scene scene = SceneManager.GetSceneByName(_remoteSceneName);
            for (int i = 0; i < remoteLevels.Length; i++)
            {
                if (remoteLevels[i].gameObject.scene.path == scene.path)
                {
                    _associatedRemoteLevel = remoteLevels[i];
                    _sequenceCollection = _associatedRemoteLevel.sequenceCollection;
                    break;
                }
            }
            _isRemoteLoaded = true;
            OnLoaded();
        }

        public IEnumerator Unload()
        {
            if (!_isRemoteLoaded)
            {
                yield break;
            }

            Scene checkScene = SceneManager.GetSceneByName(_remoteSceneName);

            if (checkScene.isLoaded)
            {
                ThreadPriority lastPriority = Application.backgroundLoadingPriority;
                Application.backgroundLoadingPriority = _loadingPriority;
                AsyncOperation async = SceneManager.UnloadSceneAsync(_remoteSceneName);
                yield return async;
                Application.backgroundLoadingPriority = lastPriority;
            }

            _isRemoteLoaded = false;
            OnUnloaded();
        }

        public void UnloadImmediate()
        {
            if (!_isRemoteLoaded)
            {
                return;
            }

            Scene checkScene = SceneManager.GetSceneByName(_remoteSceneName);
            if (checkScene.isLoaded)
            {
                SceneManager.UnloadSceneAsync(_remoteSceneName);
            }

            _isRemoteLoaded = false;
            OnUnloaded();
        }

        public void Initialize()
        {
            _lastSequence = null;
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
                throw new System.NullReferenceException(name + " has null sequence");
            }

            if (sequence != _lastSequence)
            {
                if (onSequenceEntered != null) onSequenceEntered(sequence);
                _lastSequence = sequence;
            }
            SegmentDefinition definition = sequence.Next();
            return definition.Instantiate();
        }

        public void CopyFromLegacy(Level legacy)
        {
            _sequenceCollection = legacy.sequenceCollection;
            _enabled = legacy.enabled;
            _remoteSceneName = legacy.remoteSceneName;
            _remoteSequence = legacy.remoteSequence;
            _loadingPriority = legacy.loadingPriority;
        }

        protected virtual void OnLoaded()
        {

        }

        protected virtual void OnUnloaded()
        {

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
    }
}
