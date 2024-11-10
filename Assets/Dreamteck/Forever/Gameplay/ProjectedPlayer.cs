namespace Dreamteck.Forever
{
    using UnityEngine;
    using Dreamteck.Splines;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("Dreamteck/Forever/Gameplay/Projected Player")]
    public class ProjectedPlayer : MonoBehaviour
    {
        public enum UpdateMode { Update, FixedUpdate, LateUpdate }
        public UpdateMode updateMode = UpdateMode.Update;
        public float updateTime = 0f;
        public bool useAccurateMode = false;
        public LevelSegment levelSegment { get { return _levelSegment; } }
        public int segmentIndex { get { return _segmentIndex; } } 
        private float lastUpdateTime = 0f;
        private LevelSegment _levelSegment = null;
        private int _segmentIndex = 0;
        private LevelSegment lastSegment = null;
        private Transform trs;
        private SplineSample _result = new SplineSample();
        public SplineSample result
        {
            get { return _result; }
        }
        public delegate void EmptyHandler();
        public event EmptyHandler onProject;

#if UNITY_EDITOR
        public bool drawDebug = false;
        private SplineSample gizmoResult = new SplineSample();
        private float lastDebugUpdateTime = 0f;
        public Color debugColor = Color.white;

#endif

        private void Awake()
        {
            trs = transform;
        }

        private void OnValidate()
        {
            if (updateTime < 0f) updateTime = 0f;
        }

        protected virtual void OnEnable()
        {
            OriginReset.onOriginOffset += OnOriginOffset;
        }

        protected virtual void OnDisable()
        {
            OriginReset.onOriginOffset -= OnOriginOffset;
        }

        void OnOriginOffset(Vector3 direction)
        {
            _result.position -= direction;
        }

        void Update()
        {
            if (updateMode == UpdateMode.Update) DoUpdate();
        }

        void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate) DoUpdate();
        }

        void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate) DoUpdate();
        }

        /// <summary>
        /// Externally force the projector to calculate projection immediately
        /// </summary>
        public void DoProject()
        {
            DoUpdate();
        }

        private void DoUpdate()
        {
            if (LevelGenerator.instance == null || !LevelGenerator.instance.ready)
            {
                _result.position = trs.position;
                _result.up = trs.up;
                _result.forward = trs.forward;
                _result.percent = 0.0;
                return;
            }
            if (Time.unscaledTime - lastUpdateTime < updateTime) return;
            LevelGenerator.instance.Project(trs.position, ref _result, useAccurateMode);
            int index = 0;
            LevelGenerator.instance.GlobalToLocalPercent(_result.percent, out index);
            _segmentIndex = index;
            _levelSegment = LevelGenerator.instance.segments[_segmentIndex];
            if (onProject != null) onProject();
            if (_levelSegment != lastSegment)
            {
                _levelSegment.Enter();
                lastSegment = _levelSegment;
            }
            lastUpdateTime = Time.unscaledTime;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!drawDebug) return;
            if (!Application.isPlaying) return;
            Handles.color = debugColor;
            Gizmos.color = debugColor;
            if (Time.unscaledTime - lastDebugUpdateTime >= updateTime)
            {
                LevelGenerator.instance.Project(trs.position, ref gizmoResult);
                lastDebugUpdateTime = Time.unscaledTime;
            }
            Handles.DrawDottedLine(trs.position, gizmoResult.position, 10f);
            Gizmos.DrawSphere(gizmoResult.position, HandleUtility.GetHandleSize(gizmoResult.position) * 0.1f);
        }
#endif
    }
}
