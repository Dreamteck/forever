namespace Dreamteck.Forever
{
    using Dreamteck.Splines;
    using UnityEngine;

    [AddComponentMenu("Dreamteck/Forever/Gameplay/Custom Lane Runner")]
    public class CustomLaneRunner : Runner
    {
        int _lane = 1;
        int _lastLane = 1;
        public int lane
        {
            get { return _lane; }
            set
            {
                if(_lane != value) _lastLane = _lane;
                _lane = value;
                if (_lane > _segment.customPaths.Length) _lane = _segment.customPaths.Length;
                if (_lane < 1) _lane = 1;
                if(_lane != _lastLane)
                {
                    laneLerp = 0f;
                    previousLaneResult = _result;
                    _segment.customPaths[_lane-1].Project(transform.position, ref _result);
                }
            }
        }
        public float laneSwitchSpeed = 5f;
        public AnimationCurve laneSwitchSpeedCurve;
        public int startLane = 1;
        float laneLerp = 1f;
        SplineSample previousLaneResult = new SplineSample();
        SplineSample newLaneResult = new SplineSample();
        bool usePreviousLane = false;

        protected override void Awake()
        {
            base.Awake();
            _lastLane = _lane = startLane;
        }

        protected override void OnEnteredSegment(LevelSegment entered)
        {
            base.OnEnteredSegment(entered);
            if (_lane >= _segment.customPaths.Length) _lane = _segment.customPaths.Length;
            if (_lastLane >= _segment.customPaths.Length) _lastLane = _segment.customPaths.Length;
        }

        protected override void Evaluate(double percent, ref SplineSample result)
        {
            if(usePreviousLane) _segment.customPaths[_lastLane - 1].Evaluate(percent, ref result);
            else _segment.customPaths[_lane - 1].Evaluate(percent, ref result);
        }

        protected override double Travel(double start, float distance, Spline.Direction direction, out float traveled)
        {
            if (usePreviousLane) return _segment.customPaths[_lastLane - 1].Travel(start, distance, direction, out traveled);
            return _segment.customPaths[_lane - 1].Travel(start, distance, direction, out traveled);
        }

        protected override void OnFollow(SplineSample followResult)
        {
            if(laneLerp != 1f)
            {
                usePreviousLane = true;
                Traverse(ref previousLaneResult);
                usePreviousLane = false;
                laneLerp = Mathf.MoveTowards(laneLerp, 1f, Time.deltaTime * laneSwitchSpeed);
                SplineSample.Lerp(ref previousLaneResult, ref _result, laneSwitchSpeedCurve.Evaluate(laneLerp), ref newLaneResult);
                followResult = newLaneResult;
            }
            base.OnFollow(followResult);
        }
    }
}
