namespace Dreamteck.Forever
{
    using UnityEngine;
    using Dreamteck.Splines;

    public class CustomPathGenerator : LevelPathGenerator
    {
        public bool loop = false;
        public bool useRelativeCoordinates = false;
        public int segmentCount = 10;
        int currentSegmentIndex = 0;
        Matrix4x4 trsMatrix = new Matrix4x4();
        [HideInInspector]
        public SplinePoint[] points = new SplinePoint[0];
        public Spline.Type customPathType
        {
            get { return _spline.type; }
            set { _spline.type = value; }
        }
        public int customPathSampleRate
        {
            get { return _spline.sampleRate; }
            set { _spline.sampleRate = value; }
        }
        [UnityEngine.Serialization.FormerlySerializedAs("spline")]
        [SerializeField] private Spline _spline = new Spline(Spline.Type.CatmullRom, 10);
        SplineSample[] samples = new SplineSample[0];
        float pathLength = 0f;

        public override void Initialize(LevelGenerator input)
        {
            base.Initialize(input);
            currentSegmentIndex = 0;
            CreateSpline();
            if (useRelativeCoordinates) SetTRS();
        }

        public override void Continue(LevelPathGenerator previousGenerator)
        {
            base.Continue(previousGenerator);
            currentSegmentIndex = 0;
            CreateSpline();
            if (useRelativeCoordinates) SetTRS();
        }

        void CreateSpline()
        {
            _spline = new Spline(customPathType, customPathSampleRate);
            _spline.points = points;
            if (loop) _spline.Close();
            pathLength = _spline.CalculateLength();
            float travel = pathLength / (_spline.iterations - 1);
            samples = new SplineSample[_spline.iterations];
            samples[0] = _spline.Evaluate(0.0);
            for (int i = 1; i < _spline.iterations - 1; i++)
            {
                samples[i] = _spline.Evaluate(_spline.Travel(samples[i - 1].percent, travel, Spline.Direction.Forward));
            }
            samples[_spline.iterations - 1] = _spline.Evaluate(1.0);
        }

        void SetTRS()
        {
            if (LevelGenerator.instance.segments.Count > 0)
            {
                SplineSample result = new SplineSample();
                LevelGenerator.instance.Evaluate(1.0, ref result);
                trsMatrix.SetTRS(transform.InverseTransformPoint(result.position), Quaternion.Inverse(transform.rotation) * result.rotation, Vector3.one);
            }
            else trsMatrix.SetTRS(Vector3.zero, Quaternion.identity, Vector3.one);
        }

        void Evaluate(double percent, ref SplineSample result)
        {
            if (samples.Length == 0) return;
            percent = DMath.Clamp01(percent);
            int index = DMath.FloorInt(percent * (samples.Length - 1));
            double percentExcess = (samples.Length - 1) * percent - index;
            result = samples[index];
            if (percentExcess > 0.0 && index < samples.Length - 1) result.Lerp(ref samples[index + 1], percentExcess);
            if (useRelativeCoordinates)
            {
                result.position = trsMatrix.MultiplyPoint3x4(result.position);
                result.forward = trsMatrix.MultiplyVector(result.forward);
                result.up = trsMatrix.MultiplyVector(result.up);
            }
        }

        protected override void OnPostGeneration(SplinePoint[] points)
        {
            base.OnPostGeneration(points);
            double range = 1.0 / segmentCount;
            int loopedSegmentIndex = currentSegmentIndex % segmentCount;
            double from = range * loopedSegmentIndex;
            double to = range * (loopedSegmentIndex + 1);
            SplineSample result = new SplineSample();
            for (int i = 0; i < points.Length; i++)
            {
                double percent = DMath.Lerp(from, to, (double)i / (points.Length - 1));
                Evaluate(percent, ref result);
                points[i].position = result.position;
                points[i].tangent2 = result.forward;
                points[i].normal = result.up;
                points[i].size = result.size;
                points[i].color = result.color;
            }
            for (int i = 0; i < points.Length; i++)
            {
                float pointDistance = 0f;
                if (i == 0) pointDistance = Vector3.Distance(points[i].position, points[i + 1].position);
                else pointDistance = Vector3.Distance(points[i].position, points[i - 1].position);
                points[i].tangent2 = points[i].position + points[i].tangent2 * pointDistance / 3f;
                points[i].tangent = points[i].position + (points[i].position - points[i].tangent2);
            }
            currentSegmentIndex++;
        }
    }
}
