using UnityEngine;
using Dreamteck.Splines;

namespace Dreamteck.Forever
{
    public class LevelPathGenerator : ScriptableObject
    {
        public enum PathType { Bezier = Spline.Type.Bezier, Linear = Spline.Type.Linear}
        [HideInInspector]
        public PathType pathType = PathType.Bezier;
        [HideInInspector]
        public int sampleRate = 10;
        [HideInInspector]
        public int controlPointsPerSegment = 5;
        [HideInInspector]
        public AnimationCurve normalInterpolation;
        [HideInInspector]
        public bool customNormalInterpolation = false;
        [HideInInspector]
        public AnimationCurve valueInterpolation;
        [HideInInspector]
        public bool customValueInterpolation = false;
        protected Transform transform;
        protected LevelSegment segment = null;
        protected ForeverLevel level = null;
        private bool _isNewLevel = false;
        private bool _isFirstPoint = false;

        protected CustomPathRule[] customRules = new CustomPathRule[0];

        protected bool isNewLevel
        {
            get { return _isNewLevel; }
        }
        protected bool isFirstPoint
        {
            get { return _isFirstPoint; }
        }
        internal SplinePoint lastPoint = new SplinePoint();
        protected int pointStartIndex = 0;

        /// <summary>
        /// Logic for restarting the generation - usually called when the level generator restarts
        /// </summary>
        public virtual void Initialize(LevelGenerator input)
        {
            level = null;
            segment = null;
            _isNewLevel = false;
            transform = input.transform;
            lastPoint = new SplinePoint(Vector3.zero, Vector3.forward, Vector3.up, 1f, Color.white);
            OriginReset.onOriginOffset -= OnOriginOffset;
            OriginReset.onOriginOffset += OnOriginOffset;
        }

        /// <summary>
        /// Logic for transfering internal data from the previous generator to the new one
        /// when changing path generators on-the-fly at runtime
        /// </summary>
        /// <param name="previousGenerator"></param>
        public virtual void Continue(LevelPathGenerator previousGenerator)
        {
            level = previousGenerator.level;
            segment = previousGenerator.segment;
            _isNewLevel = previousGenerator._isNewLevel;
            transform = previousGenerator.transform;
            lastPoint = previousGenerator.lastPoint;
            OriginReset.onOriginOffset -= previousGenerator.OnOriginOffset;
            OriginReset.onOriginOffset -= OnOriginOffset;
            OriginReset.onOriginOffset += OnOriginOffset;
        }

        public virtual void Continue(LevelSegment segment)
        {
            level = segment.level;
            this.segment = segment;
            lastPoint = segment.path.spline.points[segment.path.spline.points.Length - 1];
            InverseTransformPoint(ref lastPoint);
            OriginReset.onOriginOffset -= OnOriginOffset;
            OriginReset.onOriginOffset += OnOriginOffset;
        }

        public virtual void Clear()
        {
            level = null;
            segment = null;
            _isNewLevel = false;
            transform = null;
            OriginReset.onOriginOffset -= OnOriginOffset;
        }

        /// <summary>
        /// Used to move any saved point coordinates with with the floating origin
        /// </summary>
        /// <param name="direction"></param>
        protected virtual void OnOriginOffset(Vector3 direction)
        {
            if (transform != null)
            {
                lastPoint.SetPosition(lastPoint.position - transform.InverseTransformDirection(direction));
            }
        }

        private void OnDestroy()
        {
            OriginReset.onOriginOffset -= OnOriginOffset;
        }

        /// <summary>
        /// Called when a segment that belongs to a new level is passed
        /// </summary>
        protected virtual void OnNewLevel()
        {
        }

        void TransformPoint(ref SplinePoint point)
        {
            point.position = transform.TransformPoint(point.position);
            point.tangent = transform.TransformPoint(point.tangent);
            point.tangent2 = transform.TransformPoint(point.tangent2);
            point.normal = transform.TransformDirection(point.normal).normalized;
        }

        void InverseTransformPoint(ref SplinePoint point)
        {
            point.position = transform.InverseTransformPoint(point.position);
            point.tangent = transform.InverseTransformPoint(point.tangent);
            point.tangent2 = transform.InverseTransformPoint(point.tangent2);
            point.normal = transform.InverseTransformDirection(point.normal).normalized;
        }

        /// <summary>
        /// Generates the path for a level segment
        /// </summary>
        /// <param name="inputSegment"></param>
        /// <returns></returns>
        public void GeneratePath(LevelSegment inputSegment)
        {
            if (controlPointsPerSegment < 2) controlPointsPerSegment = 2;
            segment = inputSegment;
            if (level != segment.level)
            {
                _isNewLevel = true;
                level = segment.level;
                OnNewLevel();
            }
            if(segment.type == LevelSegment.Type.Custom)
            {
                lastPoint = new SplinePoint(segment.customExit.position, segment.customExit.position + segment.customExit.forward, segment.customExit.up, 1f, Color.white);
                InverseTransformPoint(ref lastPoint);
                _isNewLevel = false;
                return;
            }

            customRules = segment.GetComponents<CustomPathRule>();

            //Create points array
            SplinePoint[] points = new SplinePoint[controlPointsPerSegment];
            for (int i = 0; i < points.Length; i++) points[i] = new SplinePoint();

            pointStartIndex = 0;
            if (segment.previous != null)
            {
                if (segment.previous.path != null)
                {
                    points[0] = lastPoint;
                    pointStartIndex = 1;
                }
                else if (segment.previous.customExit != null)
                {
                    Transform exit = segment.previous.customExit;
                    points[0] = new SplinePoint(exit.position, exit.position - exit.forward * GetPointDistance() / 3f, exit.up, 1f, Color.white);
                    pointStartIndex = 1;
                }
            }

            OnBeforeGeneration(points);

            if (customRules.Length > 0)
            {
                for (int i = 0; i < customRules.Length; i++)
                {
                    customRules[i].segment = segment;
                    customRules[i].OnBeforeGeneration(this);
                }

            }
            for (int i = pointStartIndex; i < points.Length; i++)
            {
                if (i == 0 && segment.previous == null) _isFirstPoint = true;
                GeneratePoint(ref points[i], i);
                _isFirstPoint = false;
                lastPoint = points[i];
            }
            OnPostGeneration(points);
            if (customRules.Length > 0)
            {
                for (int i = 0; i < customRules.Length; i++)
                {
                    customRules[i].OnPostGeneration(points);
                }
            }

            lastPoint = points[points.Length - 1];
            _isNewLevel = false;

            for (int i = 0; i < points.Length; i++)
            {
                points[i].position = transform.TransformPoint(points[i].position);
                points[i].tangent = transform.TransformPoint(points[i].tangent);
                points[i].tangent2 = transform.TransformPoint(points[i].tangent2);
                points[i].normal = transform.TransformDirection(points[i].normal).normalized;
            }
            Spline spline = new Spline((Spline.Type)pathType, 10);
            spline.customNormalInterpolation = normalInterpolation;
            spline.customValueInterpolation = valueInterpolation;
            spline.points = points;
            segment.path.spline = spline;
            segment.path.sampleRate = sampleRate;
        }

        /// <summary>
        /// Generate a single path point based on the previous point
        /// </summary>
        /// <param name="point">The new point</param>
        /// <param name="previous">The previous point</param>
        /// <param name="pointIndex">Index of the point [0-controlPointsPerSegment]</param>
        /// <param name="pointCount">Total count of the points to be generated</param>
        protected virtual void GeneratePoint(ref SplinePoint point, int pointIndex)
        {
            point.position = lastPoint.position;
            if (!isFirstPoint) point.position += transform.forward * GetPointDistance();
            point.normal = transform.up;
            point.size = 1f;
            point.color = Color.white;
            if (customRules.Length > 0)
            {
                for (int i = 0; i < customRules.Length; i++)
                {
                    customRules[i].OnGeneratePoint(point, lastPoint, pointIndex, controlPointsPerSegment);
                }
            }
        }

        /// <summary>
        /// Generation preparation
        /// </summary>
        /// <param name="points"></param>
        protected virtual void OnBeforeGeneration(SplinePoint[] points)
        {

        }

        /// <summary>
        /// Path post-processing after the main generation is done.
        /// </summary>
        /// <param name="points"></param>
        protected virtual void OnPostGeneration(SplinePoint[] points)
        {

        }


        public float GetPointDistance()
        {
            switch (segment.axis)
            {
                case LevelSegment.Axis.X: return segment.GetBounds().size.x / (controlPointsPerSegment - 1);
                case LevelSegment.Axis.Y: return segment.GetBounds().size.y / (controlPointsPerSegment - 1);
                case LevelSegment.Axis.Z: return segment.GetBounds().size.z / (controlPointsPerSegment - 1);
            }
            return 0f;
        }

        public static void AutoTangents(ref SplinePoint[] points, int startIndex)
        {
            for (int i = startIndex; i < points.Length; i++)
            {
                Vector3 prevPos = Vector3.zero, forwardPos = Vector3.zero;
                if (i > 0) prevPos = points[i - 1].position;
                else if (points.Length > 2) prevPos = ExtrapolatePoint(points[2].position, points[1].position, points[0].position);
                else prevPos = points[0].position - points[1].position;
                if (i < points.Length - 1) forwardPos = points[i + 1].position;
                else if (points.Length > 2) forwardPos = ExtrapolatePoint(points[i - 2].position, points[i - 1].position, points[i].position);
                else forwardPos = points[i].position - points[i - 1].position;
                Vector3 delta = (forwardPos - prevPos) / 2f;
                points[i].tangent = points[i].position - delta / 3f;
                points[i].tangent2 = points[i].position + delta / 3f;
            }
        }

        public static Vector3 ExtrapolatePoint(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 v1, v2;
            v1 = p2 - p1;
            v2 = p3 - p2;
            Vector3 normal = Vector3.Cross(v1, v2);
            float angle = Vector3.SignedAngle(v1, v2, normal);
            return p3 + Quaternion.AngleAxis(angle, normal) * v2.normalized * (v1 + v2).magnitude * 0.5f;
        }

    }
}
