namespace Dreamteck.Forever
{
    using UnityEngine;
    using Dreamteck.Splines;

    [AddComponentMenu("Dreamteck/Forever/Gameplay/Basic Runner")]
    public class Runner : MonoBehaviour
    {
        public enum UpdateMode { Update, FixedUpdate, LateUpdate }
        public UpdateMode updateMode = UpdateMode.Update;
        public enum PhysicsMode { Transform, Rigidbody, Rigidbody2D }
        public enum StartMode { Percent, Distance, Project }
        [HideInInspector]
        public double startPercent = 0.0;
        [HideInInspector]
        public float startDistance = 0f;
        [HideInInspector]
        public StartMode startMode = StartMode.Percent;
        public float followSpeed = 1f;
        public bool follow = true;
        public bool isPlayer = false;
        public bool loop = false;
        public PhysicsMode physicsMode = PhysicsMode.Transform;
        public MotionModule motion
        {
            get { return _motion; }
        }
        [SerializeField]
        [HideInInspector]
        protected MotionModule _motion = new MotionModule();

        protected LevelSegment _segment = null;
        protected SplineSample _result = new SplineSample();

        public LevelSegment segment
        {
            get { return _segment; }
        }

        public SplineSample result
        {
            get { return _result; }
        }

        protected Rigidbody targetRigidbody = null;
        protected Rigidbody2D targetRigidbody2D = null;
        protected Transform targetTransform = null;

        protected static Runner playerInstance = null;
        private bool isPlayerInstance
        {
            get
            {
                return playerInstance == this;
            }
        }

        protected virtual void Awake()
        {
            if (isPlayer)
            {
                if(playerInstance != null)
                {
                    Debug.LogError("WARNING: Overriding Player Runner. Only one runner should have isPlayer = true. Overriding runner " + playerInstance.name + " with " + name);
                }
                playerInstance = this;
            }
        }

        public virtual void StartFollow()
        {
            if(LevelGenerator.instance == null || !LevelGenerator.instance.ready || LevelGenerator.instance.segments.Count == 0)
            {
                Debug.LogError(name + " Runner attempting to start following but the Level Generator isn't ready.");
                return;
            }
            int segmentIndex = 0;
            double localPercent = 0.0;
            switch (startMode)
            {
                case StartMode.Percent:
                    localPercent = LevelGenerator.instance.GlobalToLocalPercent(startPercent, out segmentIndex);
                    break;
                case StartMode.Distance:
                    localPercent = LevelGenerator.instance.GlobalToLocalPercent(LevelGenerator.instance.Travel(0.0, startDistance, Spline.Direction.Forward), out segmentIndex);
                    Debug.DrawRay(LevelGenerator.instance.EvaluatePosition(LevelGenerator.instance.Travel(0.0, startDistance, Spline.Direction.Forward)), Vector3.up * 10f, Color.red, 5f);
                    break;
                case StartMode.Project:
                    SplineSample result = new SplineSample();
                    LevelGenerator.instance.Project(transform.position, ref result);
                    localPercent = LevelGenerator.instance.GlobalToLocalPercent(result.percent, out segmentIndex);
                    break;
            }
            Init(LevelGenerator.instance.segments[segmentIndex], localPercent);
            follow = true;
        }

        public virtual void SetPercent(double percent)
        {
            int segmentIndex = 0;
            double localPercent = LevelGenerator.instance.GlobalToLocalPercent(percent, out segmentIndex);
            LevelSegment lastSegment = _segment;
            _segment = LevelGenerator.instance.segments[segmentIndex];
            _segment.Evaluate(localPercent, ref _result);
            if(_segment != lastSegment)
            {
                OnEnteredSegment(_segment);
            }
            OnFollow(_result);
        }

        public virtual void StartFollow(LevelSegment segment, double percent)
        {
            if (LevelGenerator.instance == null || !LevelGenerator.instance.ready || LevelGenerator.instance.segments.Count == 0)
            {
                Debug.LogError(name + " Runner attempting to start following but the Level Generator isn't ready.");
                return;
            }
            Init(segment, percent);
            follow = true;
        }

        private void Init(LevelSegment input, double percentAlong = 0.0)
        {
            _segment = input;
            Evaluate(percentAlong, ref _result);
            if (isPlayerInstance) _segment.Enter();
            OnEnteredSegment(_segment);
            RefreshTargets();
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

        protected virtual void Update()
        {
            if (updateMode == UpdateMode.Update) DoUpdate();
        }

        protected virtual void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate) DoUpdate();
        }

        protected virtual void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate) DoUpdate();
        }

        protected virtual void Evaluate(double percent, ref SplineSample result)
        {
            if (_segment.type == LevelSegment.Type.Custom && _segment.customMainPath >= 0 && _segment.customPaths.Length > 0)
            {
                _segment.customPaths[_segment.customMainPath].Evaluate(percent, ref result);
            }
            else
            {
                _segment.Evaluate(percent, ref result);
            }
        }

        protected virtual double Travel(double start, float distance, Spline.Direction direction, out float traveled)
        {
            if (_segment.type == LevelSegment.Type.Custom && _segment.customMainPath >= 0 && _segment.customPaths.Length > 0)
            {
               return _segment.customPaths[_segment.customMainPath].Travel(start, distance, direction, out traveled);
            }
            else
            {
               return _segment.Travel(start, distance, direction, out traveled);
            }
        }

        protected void Traverse(ref SplineSample input)
        {
            float absFollowSpeed = followSpeed;
            Spline.Direction direction = Spline.Direction.Forward;
            if (absFollowSpeed < 0f)
            {
                absFollowSpeed *= -1f;
                direction = Spline.Direction.Backward;
            }
            float travelDistance = Time.deltaTime * absFollowSpeed;
            float traveled;
            Evaluate(Travel(input.percent, travelDistance, direction, out traveled), ref input);

            if (traveled < travelDistance && ((direction == Spline.Direction.Forward && input.percent > 0.99999) || (direction == Spline.Direction.Backward && input.percent < 0.00001)))
            {
                //we have reached the end of the segment
                if (direction == Spline.Direction.Forward)
                {
                    if (_segment.next != null)
                    {
                        _segment = _segment.next;
                        OnEnteredSegment(_segment);
                        Evaluate(Travel(0.0, travelDistance - traveled, direction, out traveled), ref input);
                    }
                }
                else
                {
                    if (_segment.previous != null)
                    {
                        _segment = _segment.previous;
                        OnEnteredSegment(_segment);
                        Evaluate(Travel(1.0, travelDistance - traveled, direction, out traveled), ref input);
                    }
                }
            }
        }

        void DoUpdate()
        {
            if (!follow) return;
            if (_segment == null)
            {
                if(LevelGenerator.instance != null && LevelGenerator.instance.ready)
                {
                    if (LevelGenerator.instance.segments.Count > 0) StartFollow();
                    else return;
                } else return;
            }
            Traverse(ref _result);
            OnFollow(_result);
        }

        protected virtual void DoFollow(float move, Spline.Direction direction)
        {

        }

        protected virtual void OnFollow(SplineSample followResult)
        {
            ApplyMotion(followResult, motion);
        }

        protected virtual void OnEnteredSegment(LevelSegment entered)
        {
            if (isPlayerInstance) entered.Enter();
        }

        protected void ApplyMotion(SplineSample sample, MotionModule module)
        {
            module.sample = sample;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (targetTransform == null) RefreshTargets();
                if (targetTransform == null) return;
                module.ApplyTransform(targetTransform);
                return;
            }
#endif
            switch (physicsMode)
            {
                case PhysicsMode.Transform:
                    if (targetTransform == null) RefreshTargets();
                    if (targetTransform == null) return;
                    module.ApplyTransform(targetTransform);
                    break;
                case PhysicsMode.Rigidbody:
                    if (targetRigidbody == null)
                    {
                        RefreshTargets();
                        if (targetRigidbody == null) throw new MissingComponentException("There is no Rigidbody attached to " + name + " but the Physics mode is set to use one.");
                    }
                    module.ApplyRigidbody(targetRigidbody);
                    break;
                case PhysicsMode.Rigidbody2D:
                    if (targetRigidbody2D == null)
                    {
                        RefreshTargets();
                        if (targetRigidbody2D == null) throw new MissingComponentException("There is no Rigidbody2D attached to " + name + " but the Physics mode is set to use one.");
                    }
                    module.ApplyRigidbody2D(targetRigidbody2D);
                    break;
            }
        }

        protected void RefreshTargets()
        {
            switch (physicsMode)
            {
                case PhysicsMode.Transform:
                    targetTransform = transform;
                    break;
                case PhysicsMode.Rigidbody:
                    targetRigidbody = GetComponent<Rigidbody>();
                    break;
                case PhysicsMode.Rigidbody2D:
                    targetRigidbody2D = GetComponent<Rigidbody2D>();
                    break;
            }
        }

        [System.Serializable]
        public class MotionModule
        {
            public bool enabled = true;
            public Vector2 offset;
            public bool useSplineSizes = true;
            public Vector3 rotationOffset = Vector3.zero;
            public Vector3 baseScale = Vector3.one;
            public SplineSample sample = new SplineSample();

            public bool applyPositionX = true;
            public bool applyPositionY = true;
            public bool applyPositionZ = true;
            public bool applyPosition
            {
                get
                {
                    return applyPositionX || applyPositionY || applyPositionZ;
                }
                set
                {
                    applyPositionX = applyPositionY = applyPositionZ = value;
                }
            }

            public bool applyRotationX = true;
            public bool applyRotationY = true;
            public bool applyRotationZ = true;
            public bool applyRotation
            {
                get
                {
                    return applyRotationX || applyRotationY || applyRotationZ;
                }
                set
                {
                    applyRotationX = applyRotationY = applyRotationZ = value;
                }
            }

            public bool applyScaleX = false;
            public bool applyScaleY = false;
            public bool applyScaleZ = false;
            public bool applyScale
            {
                get
                {
                    return applyScaleX || applyScaleY || applyScaleZ;
                }
                set
                {
                    applyScaleX = applyScaleY = applyScaleZ = value;
                }
            }

            private static Vector3 position = Vector3.zero;
            private static Quaternion rotation = Quaternion.identity;

            public void ApplyTransform(Transform input)
            {
                if (!enabled) return;
                input.position = GetPosition(input.position);
                input.rotation = GetRotation(input.rotation);
                input.localScale = GetScale(input.localScale);
            }

            public void ApplyRigidbody(Rigidbody input)
            {
                if (!enabled) return;
                input.transform.localScale = GetScale(input.transform.localScale);
                input.MovePosition(GetPosition(input.position));
                Vector3 velocity = input.velocity;
                if (applyPositionX) velocity.x = 0f;
                if (applyPositionY) velocity.y = 0f;
                if (applyPositionZ) velocity.z = 0f;
                input.velocity = velocity;
                input.MoveRotation(GetRotation(input.rotation));
                velocity = input.angularVelocity;
                if (applyRotationX) velocity.x = 0f;
                if (applyRotationY) velocity.y = 0f;
                if (applyRotationZ) velocity.z = 0f;
                input.angularVelocity = velocity;
            }

            public void ApplyRigidbody2D(Rigidbody2D input)
            {
                if (!enabled) return;
                input.transform.localScale = GetScale(input.transform.localScale);
                input.position = GetPosition(input.position);
                Vector2 velocity = input.velocity;
                if (applyPositionX) velocity.x = 0f;
                if (applyPositionY) velocity.y = 0f;
                input.velocity = velocity;
                input.rotation = -GetRotation(Quaternion.Euler(0f, 0f, input.rotation)).eulerAngles.z;
                if (applyRotationX) input.angularVelocity = 0f;
            }

            internal Vector3 GetPosition(Vector3 inputPosition)
            {
                position = sample.position;
                Vector2 finalOffset = offset;
                if (useSplineSizes)
                {
                    finalOffset.x *= sample.size;
                    finalOffset.y *= sample.size;
                }
                if (finalOffset != Vector2.zero) position += sample.right * finalOffset.x + sample.up * finalOffset.y;
                if (applyPositionX) inputPosition.x = position.x;
                if (applyPositionY) inputPosition.y = position.y;
                if (applyPositionZ) inputPosition.z = position.z;
                return inputPosition;
            }

            internal Quaternion GetRotation(Quaternion inputRotation)
            {
                Vector3 resultDirection = sample.forward;
                rotation = Quaternion.LookRotation(resultDirection, sample.up);
                if (rotationOffset != Vector3.zero) rotation = rotation * Quaternion.Euler(rotationOffset);
                if (!applyRotationX || !applyRotationY)
                {
                    Vector3 euler = rotation.eulerAngles;
                    if (!applyRotationX) euler.x = inputRotation.eulerAngles.x;
                    if (!applyRotationY) euler.y = inputRotation.eulerAngles.y;
                    if (!applyRotationZ) euler.z = inputRotation.eulerAngles.z;
                    inputRotation.eulerAngles = euler;
                } else inputRotation = rotation;
                return inputRotation;
            }

            internal Vector3 GetScale(Vector3 inputScale)
            {
                if (applyScaleX) inputScale.x = baseScale.x * sample.size;
                if (applyScaleY) inputScale.y = baseScale.y * sample.size;
                if (applyScaleZ) inputScale.z = baseScale.z * sample.size;
                return inputScale;
            }

            internal void CopyFrom(MotionModule input)
            {
                enabled = input.enabled;
                offset = input.offset;
                useSplineSizes = input.useSplineSizes;
                rotationOffset = input.rotationOffset;
                baseScale = input.baseScale;
                sample = input.sample;
                applyPositionX = input.applyPositionX;
                applyPositionY = input.applyPositionY;
                applyPositionZ = input.applyPositionZ;
                applyRotationX = input.applyRotationX;
                applyRotationY = input.applyRotationY;
                applyRotationZ = input.applyRotationZ;
                applyScaleX = input.applyScaleX;
                applyScaleY = input.applyScaleY;
                applyScaleZ = input.applyScaleZ;
            }
        }
    }
}
