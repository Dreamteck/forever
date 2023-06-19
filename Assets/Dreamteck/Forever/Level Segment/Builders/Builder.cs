namespace Dreamteck.Forever
{
    using UnityEngine;
    using System.Collections;

 
    [AddComponentMenu("Dreamteck/Forever/Builders/Builder")]
    public class Builder : MonoBehaviour //Basic behavior for level segment generation.
    {
        public enum Queue { OnGenerate, OnActivate }
        public enum BuildState { Building, Idle }

        private static Builder lastBuilder = null;
        public static BuildState buildState = BuildState.Idle;

        public Queue queue = Queue.OnGenerate;
        
        public int priority = 0;
        public bool isBuilding
        {
            get { return _isBuilding; }
        }
        public bool isDone
        {
            get
            {
                return _isDone;
            }
        }
        private bool _isDone = false;
        private bool _isBuilding = false;
        private bool _isSetup = false;
        protected bool buildQueued = false;
        public LevelSegment levelSegment {
            get { return _levelSegment; }
        }
        private LevelSegment _levelSegment;
        protected Transform trs { get; private set; }

        protected virtual void Awake()
        {
            trs = transform;
        }

#if UNITY_EDITOR
        public virtual void OnPack()
        {

        }

        public virtual void OnUnpack()
        {
        }
#endif

        public void Setup(LevelSegment segment)
        {
            if(!_isSetup)
            {
                _levelSegment = segment;
                _isSetup = true;
            }
        }

        public void StartBuild()
        {
            if (_isDone) return;
            if (_isBuilding) return;
            if (buildQueued) return;
            buildQueued = true;
            buildState = BuildState.Building;
            lastBuilder = this;

            Build();

            if (this.gameObject.activeSelf)
            {
                StartCoroutine(BuildRoutine());
            }
        }

        protected virtual void Build()
        {

        }

        protected virtual IEnumerator BuildAsync()
        {
            yield return null;
        }

        protected static float Random(float min, float max)
        {
            return LevelGenerator.instance.Random(min, max);
        }

        protected static int Random(int min, int max)
        {
            return LevelGenerator.instance.Random(min, max);
        }

        IEnumerator BuildRoutine()
        {
            yield return StartCoroutine(BuildAsync());
            FinalizeBuild();
        }

        private void FinalizeBuild()
        {
            _isBuilding = buildQueued = false;
            _isDone = true;
            
            if (lastBuilder == this)
            {
                buildState = BuildState.Idle;
            }
        }
    }
}
