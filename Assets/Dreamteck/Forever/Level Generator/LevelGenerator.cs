namespace Dreamteck.Forever
{
    using Dreamteck.Splines;
    using Dreamteck.Utilities;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Threading;

    public delegate void LevelLoadHandler(ForeverLevel level, int index);
    public delegate void LevelEnterHandler(ForeverLevel oldLevel, ForeverLevel level, int index);

    [AddComponentMenu("Dreamteck/Forever/Level Generator")]
    public class LevelGenerator : MonoBehaviour
    {
        private enum ExtrusionState { Idle, Prepare, Extrude, Post }
        public enum LevelIteration { OrderedFinite, OrderedClamp, OrderedLoop, Random, SingleRepeat, SingleFinite }

        [SerializeField] private int _generateSegmentsAhead = 5;
        [SerializeField] private int _activateSegmentsAhead = 1;
        [Tooltip("How long to wait for a level to load before terminating the procedure")]
        [SerializeField] private float _loadTimeout = 10f;
        [SerializeField] private int _maxSegments = 10;
        [SerializeField] private bool _buildOnAwake = true;
        [SerializeField] private bool _multithreaded = true;
        [Tooltip("If enabled, Forever will call Resources.UnloadUnusedAssets after unloading a level")]
        [SerializeField] private bool _useUnloadUnusedAssets = false;
        [SerializeField] private bool _loopSegmentLogic = false;
        [SerializeField] private ForeverLevel[] _levelCollection = new ForeverLevel[0];
        [SerializeField] private LevelIteration _levelIteration = LevelIteration.OrderedFinite;
        [SerializeField] private ForeverRandomizer _levelRandomizer = null;
        [Tooltip("An arbitrary randomizer which can be used for various purposes using the Random method")]
        [SerializeField] private ForeverRandomizer _generationRandomizer;
        [SerializeField] private int _startLevel = 0;
        [SerializeField] private LevelPathGenerator _sharedPathGenerator;
        /// <summary>
        /// Deprecated in 1.10. Use <see cref="levelCollection"/>
        /// </summary>
        public Level[] levels = new Level[0];

        public List<ForeverLevel> loadedLevels
        {
            get { return _loadedLevels; }
        }

        private static SplineSample sampleAlloc = new SplineSample();
        private List<LGAction> _generationActons = new List<LGAction>();
        private object _locker = new object();
        private LevelSegment _extrudeSegment = null;
        private volatile ExtrusionState _extrusionState = ExtrusionState.Idle;
        private bool _isBusy = false;
        private AsyncJobSystem _asyncJobSystem;
        private List<ForeverLevel> _loadedLevels = new List<ForeverLevel>();
        private bool _hasGenerationRandomizer = false;
        private List<RemoteLevel> _remoteLevels;

        public int generateSegmentsAhead
        {
            get { return _generateSegmentsAhead; }
            set
            {
                _generateSegmentsAhead = value;
                if (_generateSegmentsAhead <= 0)
                {
                    _generateSegmentsAhead = 1;
                }
            }
        }

        public bool buildOnAwake
        {
            get { return _buildOnAwake; }
            set { _buildOnAwake = value; }
        }

        public bool multithreaded
        {
            get { return _multithreaded; }
            set { _multithreaded = value; }
        }

        public bool loopSegmentLogic
        {
            get { return _loopSegmentLogic; }
            set { _loopSegmentLogic = value; }
        }

        public LevelIteration levelIteration
        {
            get { return _levelIteration; }
            set
            {
                _levelIteration = value;
                UpdateLevelChangeHandler();
            }
        }

        public int activateSegmentsAhead
        {
            get { return _activateSegmentsAhead; }
            set { _activateSegmentsAhead = Mathf.Clamp(1, _generateSegmentsAhead, value); }
        }

        public int maxSegments
        {
            get { return _maxSegments; }
            set
            {
                _maxSegments = value;
                if (_maxSegments < _generateSegmentsAhead + 1)
                {
                    _maxSegments = _generateSegmentsAhead + 1;
                }
            }
        }

        public ForeverLevel currentLevel
        {
            get { return _levelCollection[_levelIndex]; }
        }

        public int currentLevelIndex
        {
            get { return _levelIndex; }
        }
        public ForeverLevel[] levelCollection
        {
            get { return _levelCollection; }
            set { _levelCollection = value; }
        }

        public bool isBusy
        {
            get { return _isBusy; }
        }

        private int _levelIndex = 0;

        private bool isLoadingLevel = false;
        private int segmentIndex = 0;

        private List<LevelSegment> _segments = new List<LevelSegment>();
        public List<LevelSegment> segments
        {
            get
            {
                return _segments;
            }
        }

        public int startLevel
        {
            get { return _startLevel; }
            set { _startLevel = value; }
        }

        public ForeverRandomizer levelRandomizer
        {
            get { return _levelRandomizer; }
            set { _levelRandomizer = value; }
        }

        public static LevelGenerator instance;


        public static event LevelEnterHandler onLevelEntered;
        public static event LevelLoadHandler onLevelLoaded;
        public static event LevelLoadHandler onWillLoadLevel;
        public static event System.Action onReady;
        public static event System.Action onLevelsDepleted;
        public static event System.Action<LevelSegment> onSegmentCreated;

        public bool debugMode { get; private set; }
        private GameObject[] _debugModeSegments = new GameObject[0];
        private System.Action _readyCallback = null;

        private bool _ready = false;
        public bool ready { get { return _ready; } }

        private ForeverLevel _enteredLevel = null;

        int _enteredSegmentIndex = -1;
        public int enteredSegmentIndex
        {
            get { return _enteredSegmentIndex; }
        }

        public delegate int LevelChangeHandler(int currentLevel, int levelCount);
        public LevelChangeHandler levelChangeHandler;

        private float _generationProgress = 0f;
        public float generationProgress
        {
            get { return _generationProgress; }
        }

        private bool _waitCrashed = false;

        private LevelPathGenerator _overridePathGenerator;
        private LevelPathGenerator _pathGeneratorInstance;
        public LevelPathGenerator pathGenerator
        {
            get
            {
                if (Application.isPlaying && usePathGeneratorInstance) return _pathGeneratorInstance;
                return _sharedPathGenerator;
            }
            set
            {
                if (value == _sharedPathGenerator || (usePathGeneratorInstance && value == _pathGeneratorInstance)) return;
                if (Application.isPlaying && !usePathGeneratorInstance && _sharedPathGenerator != null) value.Continue(_sharedPathGenerator);

                if (Application.isPlaying && usePathGeneratorInstance)
                {
                    if (_pathGeneratorInstance != null) Destroy(_pathGeneratorInstance);
                    _pathGeneratorInstance = Instantiate(value);
                    _pathGeneratorInstance.Continue(_sharedPathGenerator);
                }
                _sharedPathGenerator = value;
            }
        }
        private LevelPathGenerator currentPathGenerator
        {
            get
            {
                if (_overridePathGenerator != null)
                {
                    return _overridePathGenerator;
                }
                else
                {
                    return pathGenerator;
                }
            }
        }

        public void RegisterRemoteLevel(RemoteLevel level)
        {
            if (!_remoteLevels.Contains(level))
            {
                _remoteLevels.Add(level);
            }
        }

        public void UnregisterRemoteLevel(RemoteLevel level)
        {
            _remoteLevels.Remove(level);
        }

        [HideInInspector]
        public bool usePathGeneratorInstance = false;

        void Awake()
        {
            instance = this;
            _asyncJobSystem = GetComponent<AsyncJobSystem>();
            if (_asyncJobSystem == null)
            {
                _asyncJobSystem = gameObject.AddComponent<AsyncJobSystem>();
            }
            LevelSegment.onSegmentEntered += OnSegmentEntered;
            UpdateLevelChangeHandler();
            if (_buildOnAwake)
            {
                StartGeneration();
            }
        }

        private void OnDestroy()
        {
            LevelSegment.onSegmentEntered -= OnSegmentEntered;
            if (instance == this)
            {
                instance = null;
            }
            for (int i = 0; i < _levelCollection.Length; i++)
            {
                if (_levelCollection[i].isReady)
                {
                    _levelCollection[i].UnloadImmediate();
                }
            }
            onLevelEntered = null;
            onWillLoadLevel = null;
            onLevelLoaded = null;
            _loadedLevels.Clear();
        }

        IEnumerator StartRoutine()
        {
            _ready = false;
            _generationProgress = 0f;
            while (isLoadingLevel)
            {
                yield return null;
            }
            int count = Mathf.Min(1 + _generateSegmentsAhead, _maxSegments);
            StartCoroutine(ProgressRoutine(count));
            QueueSegmentCreation(count);
            while (_isBusy)
            {
                yield return null;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                while (segments[i].type == LevelSegment.Type.Extruded && !segments[i].extruded)
                {
                    yield return null;
                }
                if (i < _activateSegmentsAhead)
                {
                    segments[i].Activate();
                }
            }
            while (!segments[0].isReady)
            {
                yield return null;
            }

            _ready = true;
            if(_readyCallback != null)
            {
                _readyCallback.Invoke();
                _readyCallback = null;
            }
            if (onReady != null)
            {
                onReady();
            }
            _segments[0].Enter();
        }

        IEnumerator ProgressRoutine(int targetCount)
        {
            while (!_ready)
            {
                _generationProgress = 0f;
                for (int i = 0; i < segments.Count; i++)
                {
                    if (segments[i].type == LevelSegment.Type.Custom || segments[i].extruded) _generationProgress++;
                }
                _generationProgress /= targetCount;
                yield return null;
            }
            _generationProgress = 1f;
        }

        private IEnumerator ExtrudeRoutine()
        {
            if (_extrusionState == ExtrusionState.Prepare)
            {
                yield return _extrudeSegment.OnBeforeExtrude();
                _extrusionState++;
                if (!_multithreaded)
                {
                    _extrudeSegment.Extrude();
                    _extrusionState = ExtrusionState.Post;
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(state => ExtrudeThread());
                }
            }

            while (_extrusionState == ExtrusionState.Extrude)
            {
                yield return null;
            }

            if (_extrusionState == ExtrusionState.Post)
            {
                yield return _extrudeSegment.OnPostExtrude();
                _extrusionState = ExtrusionState.Idle;
            }
        }

        public void LoadLevel(int index, bool forceHighPriority)
        {
            _levelIndex = index;
            if (onWillLoadLevel != null)
            {
                onWillLoadLevel(currentLevel, _levelIndex);
            }
            currentLevel.onSequenceEntered += OnSequenceEntered;

            if (currentLevel.remoteSequence)
            {
                EnqueueAction(LoadRemoteLevelAction, currentLevel);
            }
            else
            {
                currentLevel.Initialize();
                if (currentLevel.pathGenerator != null)
                {
                    OverridePathGenerator(currentLevel.pathGenerator);
                }
            }
        }

        public void UnloadLevel(ForeverLevel lvl, bool forceHighPriority)
        {
            if (lvl.remoteSequence && lvl.isReady)
            {
                EnqueueAction(UnloadRemoteLevelAction, lvl);
            }
        }

        public void AddLevel(ForeverLevel lvl)
        {
            ArrayUtility.Add(ref _levelCollection, lvl);
        }

        public void RemoveLevel(int index)
        {
            ArrayUtility.RemoveAt(ref _levelCollection, index);
        }

        IEnumerator LoadRemoteLevelRoutine(ForeverLevel lvl, System.Action completeHandler, UnityEngine.ThreadPriority priority = UnityEngine.ThreadPriority.Normal)
        {
            while (isLoadingLevel)
            {
                yield return null;
            }
            isLoadingLevel = true;
            yield return lvl.Load();
            if (!lvl.isReady)
            {
                Debug.LogError("Failed loading remote level " + lvl.name);
            }
            else
            {
                _loadedLevels.Add(lvl);
                if (onLevelLoaded != null)
                {
                    int index = 0;
                    for (int i = 0; i < _levelCollection.Length; i++)
                    {
                        if (_levelCollection[i] == lvl)
                        {
                            index = i;
                            break;
                        }
                    }
                    if (onLevelLoaded != null)
                    {
                        onLevelLoaded.SafeInvoke(lvl, index);
                    }
                }
            }
            lvl.Initialize();
            if (lvl.pathGenerator != null)
            {
                OverridePathGenerator(lvl.pathGenerator);
            }
            isLoadingLevel = false;
            if (completeHandler != null)
            {
                completeHandler();
            }
        }

        private void OverridePathGenerator(LevelPathGenerator overrideGen)
        {
            LevelPathGenerator lastGenerator = currentPathGenerator;
            if (usePathGeneratorInstance)
            {
                if (_overridePathGenerator != null)
                {
                    Destroy(_overridePathGenerator);
                }
                _overridePathGenerator = Instantiate(overrideGen);
            }
            else
            {
                _overridePathGenerator = overrideGen;
            }
            _overridePathGenerator.Continue(lastGenerator);
        }

        private void UnloadRemoteLevelAction(System.Action completeHandler, params object[] args)
        {
            ForeverLevel level = args[0] as ForeverLevel;
            StartCoroutine(UnloadRemoteLevelRoutine(level, completeHandler));
        }
         
        private IEnumerator UnloadRemoteLevelRoutine(ForeverLevel lvl, System.Action completeHandler)
        {
            isLoadingLevel = true;
            yield return null; //Make sure the unloading starts on the next frame to give time for resources to get freed up
            yield return lvl.Unload();
            if (_useUnloadUnusedAssets)
            {
                yield return Resources.UnloadUnusedAssets();
            }
            _loadedLevels.Remove(lvl);
            isLoadingLevel = false;
            if (completeHandler != null)
            {
                completeHandler();
            }
        }

        public void Clear()
        {
            StartCoroutine(ClearRoutine());
        }

        private IEnumerator ClearRoutine()
        {
            InterruptWork();
            while (_isBusy)
            {
                yield return null;
            }
            StopExtrusion();
            if (usePathGeneratorInstance && _overridePathGenerator != null)
            {
                Destroy(_overridePathGenerator);
            }
            _overridePathGenerator = null;

            for (int i = 0; i < _segments.Count; i++)
            {
                _segments[i].Destroy();
            }


            for (int i = 0; i < _levelCollection.Length; i++)
            {
                if (_levelCollection[i].remoteSequence && _levelCollection[i].isReady)
                {
                    EnqueueAction(UnloadRemoteLevelAction, _levelCollection[i]);
                }
            }

            _loadedLevels.Clear();

            while (_isBusy)
            {
                yield return null;
            }

            _segments.Clear();
            _enteredLevel = null;
            _enteredSegmentIndex = -1;
            _ready = false;
        }

        public void Restart(System.Action readyCallback = null)
        {
            StartCoroutine(RestartRoutine(readyCallback));
        }

        private IEnumerator RestartRoutine(System.Action readyCallback = null)
        {
            yield return ClearRoutine();
            StartGeneration(readyCallback);
        }

        public void StartGeneration(System.Action readyCallback = null)
        {
            _readyCallback = readyCallback;
            if (_levelRandomizer != null)
            {
                _levelRandomizer.Initialize();
            }

            if (_generationRandomizer != null)
            {
                _generationRandomizer.Initialize();
                _hasGenerationRandomizer = true;
            } else
            {
                _hasGenerationRandomizer = false;
            }

            StopAllCoroutines();
            if (usePathGeneratorInstance)
            {
                if (_overridePathGenerator != null)
                {
                    Destroy(_overridePathGenerator);
                }
                if (_pathGeneratorInstance != null)
                {
                    Destroy(_pathGeneratorInstance);
                }
                _pathGeneratorInstance = Instantiate(_sharedPathGenerator);
            }
            _overridePathGenerator = null;
            if (currentPathGenerator == null)
            {
                Debug.LogError("Level Generator " + name + " does not have a Path Generator assigned");
                return;
            }

            _enteredLevel = null;
            _enteredSegmentIndex = -1;
            segmentIndex = 0;
            if (_startLevel >= _levelCollection.Length)
            {
                _startLevel = _levelCollection.Length - 1;
            }

            _levelIndex = _startLevel;
            while (!_levelCollection[_levelIndex].enabled)
            {
                _levelIndex++;
                if (_levelIndex >= _levelCollection.Length) break;
            }
            currentPathGenerator.Initialize(this);
            LoadLevel(_levelIndex, true);
            StartCoroutine(StartRoutine());
        }

        public LevelSegment GetSegmentAtPercent(double percent)
        {
            int pathIndex;
            GlobalToLocalPercent(percent, out pathIndex);
            if (_segments.Count == 0) return null;
            return _segments[pathIndex];
        }

        public LevelSegment FindSegmentForPoint(Vector3 point)
        {
            Project(point, ref sampleAlloc);
            return GetSegmentAtPercent(sampleAlloc.percent);
        }

        public void SetDebugMode(bool enabled, GameObject[] debugSegments = null)
        {
            debugMode = enabled;
            if (!debugMode)
            {
                _debugModeSegments = null;
                UnloadLevel(currentLevel, true);
            }
            else
            {
                _debugModeSegments = debugSegments;
            }
        }

        public void Project(Vector3 point, ref SplineSample result, bool bypassCache = false)
        {
            if (_segments.Count == 0) return;
            int closestPath = 0;
            float closestDist = Mathf.Infinity;
            for (int i = 0; i < _segments.Count; i++)
            {
                if (!_segments[i].extruded && _segments[i].type == LevelSegment.Type.Extruded) continue;
                _segments[i].Project(point, ref sampleAlloc, 0.0, 1.0, bypassCache ? SplinePath.EvaluateMode.Accurate : SplinePath.EvaluateMode.Cached);
                float dist = (sampleAlloc.position - point).sqrMagnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPath = i;
                    result = sampleAlloc;
                }
            }
            result.percent = LocalToGlobalPercent(result.percent, closestPath);
        }

        public float CalculateLength(double from = 0.0, double to = 1.0, double resolution = 1.0)
        {
            if (_segments.Count == 0) return 0f;
            if (to < from)
            {
                double temp = from;
                from = to;
                to = temp;
            }
            int fromSegmentIndex = 0, toSegmentIndex = 0;
            double fromSegmentPercent = 0.0, toSegmentPercent = 0.0;
            fromSegmentPercent = GlobalToLocalPercent(from, out fromSegmentIndex);
            toSegmentPercent = GlobalToLocalPercent(to, out toSegmentIndex);
            float length = 0f;

            for (int i = fromSegmentIndex; i <= toSegmentIndex; i++)
            {
                double f = 0.0;
                double t = 1.0;
                if (i == fromSegmentIndex)
                {
                    f = fromSegmentPercent;
                }
                if (i == toSegmentIndex)
                {
                    t = toSegmentPercent;
                }

                length += segments[i].CalculateLength(f, t);
            }

            return length;
        }

        public double Travel(double start, float distance, Spline.Direction direction)
        {
            if (_segments.Count == 0) return 0.0;
            if (direction == Spline.Direction.Forward && start >= 1.0) return 1.0;
            else if (direction == Spline.Direction.Backward && start <= 0.0) return 0.0;
            if (distance == 0f) return DMath.Clamp01(start);
            float moved = 0f;
            Vector3 lastPosition = EvaluatePosition(start);
            double lastPercent = start;
            int iterations = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                iterations += segments[i].path.spline.iterations;
            }
            int step = iterations - 1;
            int nextSampleIndex = direction == Spline.Direction.Forward ? DMath.CeilInt(start * step) : DMath.FloorInt(start * step);
            float lastDistance = 0f;
            Vector3 pos = Vector3.zero;
            double percent = start;

            while (true)
            {
                percent = (double)nextSampleIndex / step;
                pos = EvaluatePosition(percent);
                lastDistance = Vector3.Distance(pos, lastPosition);
                lastPosition = pos;
                moved += lastDistance;
                if (moved >= distance) break;
                lastPercent = percent;
                if (direction == Spline.Direction.Forward)
                {
                    if (nextSampleIndex == step) break;
                    nextSampleIndex++;
                }
                else
                {
                    if (nextSampleIndex == 0) break;
                    nextSampleIndex--;
                }
            }
            return DMath.Lerp(lastPercent, percent, 1f - (moved - distance) / lastDistance);
        }

        public void Evaluate(double percent, ref SplineSample result)
        {
            if (_segments.Count == 0) return;
            int pathIndex;
            double localPercent = GlobalToLocalPercent(percent, out pathIndex);
            _segments[pathIndex].Evaluate(localPercent, ref result);
            result.percent = percent;
        }

        public Vector3 EvaluatePosition(double percent)
        {
            if (_segments.Count == 0) return Vector3.zero;
            int pathIndex;
            double localPercent = GlobalToLocalPercent(percent, out pathIndex);
            return _segments[pathIndex].EvaluatePosition(localPercent);
        }

        public double GlobalToLocalPercent(double percent, out int segmentIndex)
        {
            double segmentValue = percent * _segments.Count;
            segmentIndex = Mathf.Clamp(DMath.FloorInt(segmentValue), 0, _segments.Count - 1);
            if (_segments.Count == 0)
            {
                return 0.0;
            }
            return DMath.InverseLerp(segmentIndex, segmentIndex + 1, segmentValue);
        }

        public double LocalToGlobalPercent(double localPercent, int segmentIndex)
        {
            if (_segments.Count == 0) return 0.0;
            double percentPerPath = 1.0 / _segments.Count;
            return DMath.Clamp01(segmentIndex * percentPerPath + localPercent * percentPerPath);
        }

        public void NextLevel(bool forceHighPriority = false)
        {
            if (_levelIteration == LevelIteration.SingleFinite)
            {
                return;
            }
            currentLevel.onSequenceEntered -= OnSequenceEntered;
            _levelIndex = GetNextLevelIndex();
            LoadLevel(_levelIndex, forceHighPriority);
        }

        public AsyncJobSystem.AsyncJobOperation ScheduleAsyncJob<T>(AsyncJobSystem.JobData<T> data)
        {
            return _asyncJobSystem.ScheduleJob(data);
        }

        public float Random(float min, float max)
        {
            if(_hasGenerationRandomizer)
            {
                return _generationRandomizer.Next(min, max);
            }
            return UnityEngine.Random.Range(min, max);
        }

        public int Random(int min, int max)
        {
            if (_hasGenerationRandomizer)
            {
                return _generationRandomizer.Next(min, max);
            }
            return UnityEngine.Random.Range(min, max);
        }

        private void LoadRemoteLevelAction(System.Action completeHandler, params object[] args)
        {
            ForeverLevel level = args[0] as ForeverLevel;
            StartCoroutine(LoadRemoteLevelRoutine(level, completeHandler));
        }

        private void OnSequenceEntered(SegmentSequence sequence)
        {
            if (sequence.customPathGenerator != null)
            {
                OverridePathGenerator(sequence.customPathGenerator);
            }
            else
            {
                if (currentLevel.pathGenerator != null && currentLevel.pathGenerator != _overridePathGenerator)
                {
                    OverridePathGenerator(currentLevel.pathGenerator);
                }
                else if(_overridePathGenerator != null && currentLevel.pathGenerator == null)
                {
                    pathGenerator.Continue(_overridePathGenerator);
                    if (usePathGeneratorInstance)
                    {
                        Destroy(_overridePathGenerator);
                    }
                    _overridePathGenerator = null;
                }
            }
        }

        public void SetLevel(int index, bool forceHighPriority = false)
        {
            if (index < 0 || index >= _levelCollection.Length) return;
            if (currentLevel)
            {
                currentLevel.onSequenceEntered -= OnSequenceEntered;
            }
            LoadLevel(index, forceHighPriority);
        }

        private int GetNextLevelIndex()
        {
            int nextLevel = levelChangeHandler(_levelIndex, _levelCollection.Length - 1);
            int attempts = _levelCollection.Length;
            while (attempts > 0 && !_levelCollection[nextLevel].enabled)
            {
                nextLevel = levelChangeHandler(nextLevel, _levelCollection.Length - 1);
                attempts--;
            }
            return nextLevel;
        }

        private void UpdateLevelChangeHandler()
        {
            switch (_levelIteration)
            {
                case LevelIteration.OrderedFinite: levelChangeHandler = IncrementClamp; break;
                case LevelIteration.OrderedClamp: levelChangeHandler = IncrementClamp; break;
                case LevelIteration.OrderedLoop: levelChangeHandler = IncrementRepeat; break;
                case LevelIteration.Random: levelChangeHandler = RandomLevel; break;
                case LevelIteration.SingleRepeat: levelChangeHandler = SameLevel; break;
            }
        }

        private int IncrementClamp(int current, int max)
        {
            return Mathf.Clamp(current + 1, 0, max);
        }

        private int IncrementRepeat(int current, int max)
        {
            current++;
            if (current > max) current = 0;
            return current;
        }

        private int SameLevel(int current, int max)
        {
            return current;
        }

        private int RandomLevel(int current, int max)
        {
            int index = _levelRandomizer.Next(0, _levelCollection.Length);
            if (index == current) index++;
            if (index >= max) index = 0;
            return index;
        }

        /// <summary>
        /// Forces the level generator to create new segments. Will create them as soon as it becomes free of other tasks.
        /// </summary>
        /// <param name="count">How many segments to create.</param>
        public void QueueSegmentCreation(int count = 1)
        {
            StartCoroutine(QueueNextSegmentRoutine(count));
        }

        /// <summary>
        /// Forces the level generator to create a new segment from the specified level segment prefab
        /// </summary>
        public void QueueSegmentCreation(LevelSegment segmentPrefab)
        {
            StartCoroutine(QueueCustomSegmentRoutine(segmentPrefab));
        }

        private IEnumerator QueueNextSegmentRoutine(int count)
        {
            while (_isBusy)
            {
                yield return null;
            }
            for (int i = 0; i < count; i++)
            {
                EnqueueAction(CreateNextSegment);
                while (_isBusy)
                {
                    yield return null;
                }
                if (currentLevel.IsDone())
                {
                    break;
                }
            }
        }

        private IEnumerator QueueCustomSegmentRoutine(LevelSegment segmentPrefab)
        {
            while (_isBusy)
            {
                yield return null;
            }
            EnqueueAction(CreateCustomSegment, segmentPrefab);
        }

        private void CreateNextSegment(System.Action completeHandler, params object[] args)
        {
            StartCoroutine(CreateSegment(completeHandler));
        }

        private void CreateCustomSegment(System.Action completeHandler, params object[] args)
        {
            LevelSegment segmentPrefab = args[0] as LevelSegment;
            if(segmentPrefab != null)
            {
                StartCoroutine(CreateSegment(segmentPrefab, completeHandler));
            }
        }

        public void DestroySegment(int index)
        {
            _segments[index].Destroy();
            _segments.RemoveAt(index);
            segmentIndex--;
            if (index >= _segments.Count)
            {
                currentPathGenerator.Continue(_segments[_segments.Count - 1]);
            }
        }

        private IEnumerator CreateSegment(System.Action completeHandler)
        {
            yield return WaitForSegmentCreation();
            if (_waitCrashed || currentLevel.IsDone())
            {
                if (completeHandler != null)
                {
                    completeHandler();
                }
                yield break;
            }

            LevelSegment segment = null;

            if (debugMode)
            {
                GameObject go = Instantiate(_debugModeSegments[Random(0, _debugModeSegments.Length)]);
                segment = go.GetComponent<LevelSegment>();
            }
            else
            {
                segment = currentLevel.InstantiateSegment();
            }

            yield return InitializeSegmentRoutine(segment);

            //Remove old segments
            if (!_loopSegmentLogic && _segments.Count > _maxSegments)
            {
                StartCoroutine(CleanupRoutine());
            }
            yield return null;

            if (completeHandler != null)
            {
                completeHandler();
            }

            if (!debugMode)
            {
                HandleLevelChange();
            }
        }

        private IEnumerator CreateSegment(LevelSegment segmentPrefab, System.Action completeHandler)
        {
            yield return WaitForSegmentCreation();
            LevelSegment segment = Instantiate(segmentPrefab);
            yield return InitializeSegmentRoutine(segment);
            if (!_loopSegmentLogic && _segments.Count > _maxSegments)
            {
                StartCoroutine(CleanupRoutine());
            }
            yield return null;

            if (completeHandler != null)
            {
                completeHandler();
            }
        }

        private IEnumerator WaitForSegmentCreation()
        {
            _waitCrashed = false;
            if (!debugMode)
            {
                float startTime = Time.realtimeSinceStartup;
                while (!currentLevel.isReady)
                {
                    if (Time.realtimeSinceStartup - startTime > _loadTimeout)
                    {
                        Debug.LogError("Level " + currentLevel + " was not properly loaded. Skipping segment creation.");
                        _waitCrashed = true;
                        break;
                    }
                    yield return null;
                }
            }
        }

        private IEnumerator InitializeSegmentRoutine(LevelSegment segment)
        {
            segment.gameObject.SetActive(false);
            Transform segmentTrs = segment.transform;
            Vector3 spawnPos = segmentTrs.position;
            Quaternion spawnRot = segmentTrs.rotation;
            if (segments.Count > 0)
            {
                SplineSample lastSegmentEndResult = new SplineSample();
                _segments[_segments.Count - 1].Evaluate(1.0, ref lastSegmentEndResult);
                spawnPos = lastSegmentEndResult.position;
                spawnRot = lastSegmentEndResult.rotation;
                switch (segment.axis)
                {
                    case LevelSegment.Axis.X: spawnRot = Quaternion.AngleAxis(90f, Vector3.up) * spawnRot; break;
                    case LevelSegment.Axis.Y: spawnRot = Quaternion.AngleAxis(90f, Vector3.right) * spawnRot; break;
                }
            }

            if (segment.type == LevelSegment.Type.Custom)
            {
                if(_segments.Count == 0)
                {
                    spawnPos = transform.position;
                }
                Quaternion entranceRotationDelta = segment.customEntrance.rotation * Quaternion.Inverse(spawnRot);
                segmentTrs.rotation = segmentTrs.rotation * Quaternion.Inverse(entranceRotationDelta);
                if (segment.customKeepUpright) segmentTrs.rotation = Quaternion.FromToRotation(segment.customEntrance.up, Vector3.up) * segmentTrs.rotation;
                Vector3 entranceOffset = segmentTrs.position - segment.customEntrance.position;
                segmentTrs.position = spawnPos + entranceOffset;
                segment.gameObject.SetActive(true);
            }
            else
            {
                if (segment.objectProperties[0].extrusionSettings.applyRotation)
                {
                    segmentTrs.rotation = spawnRot;
                }
            }

            if (segmentIndex == int.MaxValue)
            {
                segmentIndex = 2;
            }

            segment.Setup(this, segmentIndex++);
            if (_loopSegmentLogic && _segments.Count > 0)
            {
                segment.SetLoop(_segments[0]);
            }
            currentPathGenerator.GeneratePath(segment);

            _segments.Add(segment);
            if (onSegmentCreated != null)
            {
                onSegmentCreated(segment);
            }

            Extrude(segment);
            while (_extrusionState != ExtrusionState.Idle)
            {
                yield return null;
            }
        }

        private bool HandleLevelChange()
        {
            if (!currentLevel.IsDone())
            {
                return false;
            }
            if ((_levelIteration == LevelIteration.OrderedFinite && _levelIndex >= _levelCollection.Length - 1) || _levelIteration == LevelIteration.SingleFinite)
            {
                if (onLevelsDepleted != null)
                {
                    onLevelsDepleted();
                }
                return true;
            }
            NextLevel();
            return true;
        }

        private IEnumerator CleanupRoutine()
        {
            yield return StartCoroutine(DestroySegmentRoutine(0));
            if (_segments.Count > _maxSegments)
            {
                StartCoroutine(CleanupRoutine());
            }
        }

        //First wait for the SegmentBuilder to start building and only after that queue the destruction. Building should come before destruction
        private IEnumerator DestroySegmentRoutine(int index)
        {
            ForeverLevel segmentLevel = _segments[index].level;
            _segments[index].Destroy();
            if (segmentLevel.remoteSequence)
            {
                bool levelFound = false;
                for (int i = 0; i < _segments.Count; i++)
                {
                    if (i != index && _segments[i].level == segmentLevel)
                    {
                        levelFound = true;
                        break;
                    }
                }
                yield return null;
                if (!levelFound)
                {
                    UnloadLevel(segmentLevel, false);
                }
            }
            _segments.RemoveAt(index);
        }

        public void EnterLevel(int index)
        {
            if (_levelCollection[index].isReady)
            {
                if (onLevelEntered != null)
                {
                    onLevelEntered.SafeInvoke(_enteredLevel, _levelCollection[index], index);
                }
            }
        }

        public void EnqueueAction(LGAction.LGHandler action, params object[] args)
        {
            _generationActons.Add(new LGAction(action, OnActionComplete, args));
            if (!_isBusy)
            {
                _isBusy = true;
                _generationActons[0].Start();
            }
        }

        private void OnActionComplete()
        {
            _generationActons.RemoveAt(0);
            if (_generationActons.Count > 0)
            {
                _generationActons[0].Start();
            }
            else
            {
                _isBusy = false;
            }
        }

        private void InterruptWork()
        {
            //Remove all actions except the current one
            for (int i = _generationActons.Count - 1; i >= 1; i--)
            {
                _generationActons.RemoveAt(i);
            }
        }

        private void OnSegmentEntered(LevelSegment entered)
        {
            if (!_ready)
            {
                return;
            }
            if (entered.index <= _enteredSegmentIndex)
            {
                return;
            }

            _enteredSegmentIndex = entered.index;
            if (_enteredLevel != entered.level)
            {
                var oldLevel = _enteredLevel;
                _enteredLevel = entered.level;
                int enteredIndex = 0;
                for (int i = 0; i < _levelCollection.Length; i++)
                {
                    if (_enteredLevel == _levelCollection[i])
                    {
                        enteredIndex = i;
                        break;
                    }
                }
                if (onLevelEntered != null)
                {
                    onLevelEntered.SafeInvoke(oldLevel, _enteredLevel, enteredIndex);
                }
            }

            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i].index == _enteredSegmentIndex)
                {
                    int segmentsAhead = _segments.Count - (i + 1);
                    if (segmentsAhead < _generateSegmentsAhead)
                    {
                        QueueSegmentCreation(_generateSegmentsAhead - segmentsAhead);
                    }
                    //Segment activation
                    for (int j = i; j <= i + _activateSegmentsAhead && j < _segments.Count; j++)
                    {
                        if (!segments[j].activated)
                        {
                            StartCoroutine(ActivateSegmentRoutine(_segments[j]));
                        }
                    }
                    break;
                }
            }
        }

        private IEnumerator ActivateSegmentRoutine(LevelSegment segment)
        {
            while (segment.type == LevelSegment.Type.Extruded && !segment.extruded)
            {
                yield return null;
            }
            segment.Activate();
        }

        public void StopExtrusion()
        {
            _extrusionState = ExtrusionState.Idle;
            StopCoroutine(ExtrudeRoutine());
        }

        private void Extrude(LevelSegment segment)
        {
            if (_extrusionState != ExtrusionState.Idle)
            {
                Debug.LogError("Cannot extrude segment " + segment.name + " because another segment is currently being computed");
                return;
            }
            if (segment != null)
            {
                _extrudeSegment = segment;
                _extrusionState = ExtrusionState.Prepare;
                StartCoroutine(ExtrudeRoutine());
            }
            else
            {
                Debug.LogError("Extrusion error - level segment is NULL");
            }
        }

        private void ExtrudeThread()
        {
            lock (_locker)
            {
                if (_extrusionState == ExtrusionState.Extrude)
                {
                    try
                    {
                        _extrudeSegment.Extrude();
                        _extrusionState = ExtrusionState.Post;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }
    }
}
