using UnityEngine;
using Dreamteck.Splines;

namespace Dreamteck.Forever
{
    public class WavyPathGenerator : HighLevelPathGenerator
    {
        public float angle = 45f;
        public float turnRate = 0f;
        public Vector3 turnAxis = Vector3.up;

        private float currentAngle = 0f;
        private bool positive = true;


        protected override void GeneratePoint(ref Point point, int pointIndex)
        {
            base.GeneratePoint(ref point, pointIndex);
            if (isFirstPoint) return;
            if (positive && currentAngle == angle) positive = false;
            else if (currentAngle == -angle) positive = true;
            currentAngle = MoveAngle(currentAngle);
            SetOrientation(orientation + currentAngle * turnAxis.normalized);
            point.position = GetPointPosition();
            point.autoRotation = true;
        }

        float MoveAngle(float current)
        {
            if (positive)
            {
                return Mathf.MoveTowards(current, angle, turnRate);
            }
            else
            {
                return Mathf.MoveTowards(current, -angle, turnRate);
            }
        }


        public override void Initialize(LevelGenerator input)
        {
            base.Initialize(input);
            currentAngle = 0f;
        }

        protected void OffsetPoints(SplinePoint[] points, Vector3 offset, Space space)
        {
            if (offset != Vector3.zero) segment.stitch = false;
            SplineSample result = new SplineSample();
            if (space == Space.Self && segment.previous != null) segment.previous.Evaluate(1.0, ref result);
            for (int i = 0; i < points.Length; i++) points[i].SetPosition(points[i].position + offset);
        }
    }
}
