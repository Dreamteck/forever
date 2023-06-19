namespace Dreamteck.Forever
{
    using Dreamteck.Splines;
    using UnityEngine;

    [AddComponentMenu("Dreamteck/Forever/Gameplay/Lane Runner")]
    public class LaneRunner : Runner
    {
        public float width = 5f;
        public int laneCount = 3;
        public int startLane = 2;
        public float laneSwitchSpeed = 5f;
        public bool useCustomPaths = false;
        public AnimationCurve laneSwitchSpeedCurve;
        public Vector2 laneVector = Vector2.right;

        private MotionModule laneModule = new MotionModule();
        SplineSample[] customPathResults = new SplineSample[0];

        public int lane
        {
            get { return _lane; }
            set {
                if (value > laneCount) value = laneCount;
                else if (value < 1) value = 1;
                lastLane = _lane;
                _lane = value;
                laneBlend = Mathf.InverseLerp(lastLane, _lane, laneValue);
            }
        }
        public float laneBlend
        {
            get { return _laneBlend; }
            set { _laneBlend = value; }
        }
        public int lastLane
        {
            get { return _lastLane; }
            set { _lastLane = value; }
        }
        public float laneValue
        {
            get { return _laneValue; }
            set { _laneValue = value; }
        }

        [SerializeField]
        [HideInInspector]
        private int _lane = 2;
        private float _laneBlend = 0f;
        private int _lastLane = 2;
        float _laneValue = 2f;


        private void OnValidate()
        {
            if (laneCount <= 0) laneCount = 1;
            if (startLane <= 0) startLane = 1;
            else if (startLane > laneCount) startLane = laneCount;
            if (width < 0f) width = 0f;
        }

        public override void StartFollow()
        {
            lane = lastLane = startLane;
            base.StartFollow();
        }

        public override void StartFollow(LevelSegment segment, double percent)
        {
            lane = lastLane = startLane;
            base.StartFollow(segment, percent);
        }


        protected override void OnFollow(SplineSample followResult)
        {
            laneBlend = Mathf.MoveTowards(laneBlend, 1f, Time.deltaTime * laneSwitchSpeed);
            laneValue = Mathf.Lerp(lastLane, _lane, laneSwitchSpeedCurve.Evaluate(laneBlend));
            if (useCustomPaths) //Custom lane following
            {
                if (customPathResults.Length < _segment.customPaths.Length)
                {
                    SplineSample[] newResults = new SplineSample[_segment.customPaths.Length];
                    customPathResults.CopyTo(newResults, 0);
                    customPathResults = newResults;
                }
                for (int i = 0; i < _segment.customPaths.Length; i++) _segment.customPaths[i].Evaluate(_result.percent, ref customPathResults[i]);

                if (_segment.customPaths.Length > 1) //Interpolate between custom paths
                {
                    if (lane > _segment.customPaths.Length) lane = _segment.customPaths.Length;
                    if (laneValue > _segment.customPaths.Length) laneValue = _segment.customPaths.Length;
                    if (lastLane > _segment.customPaths.Length) lastLane = _segment.customPaths.Length;
                    _result = customPathResults[lastLane - 1];
                    _result.Lerp(ref customPathResults[lane - 1], Mathf.Abs(laneValue - lastLane));
                    ApplyMotion(_result, motion);
                    return;
                } else if(_segment.customPaths.Length > 0) _result = customPathResults[0]; //Use custom path but apply offset
            }

            laneModule.CopyFrom(motion); //Copy the motion from the existing module
            //Apply lane offset:
            laneModule.offset += Vector2.Lerp(-laneVector * width * 0.5f, laneVector * width * 0.5f, (laneValue - 1f) / (laneCount - 1));
            ApplyMotion(_result, laneModule);
        }
    }
}
