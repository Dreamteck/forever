using System.Collections;
using System.Collections.Generic;
using Dreamteck.Splines;
using UnityEngine;

namespace Dreamteck.Forever
{
    public class HighLevelPathGenerator : LevelPathGenerator
    {
        public class Point
        {
            public Vector3 position = Vector3.zero;
            public float size = 1f;
            public Color color = Color.white;
            public bool autoRotation = true;
            public Vector3 rotation = Vector3.zero;
        }

        [HideInInspector]
        public bool useCustomNormalDirection = false;
        [HideInInspector]
        public Vector3 customNormalDirection = Vector3.up;
        public Vector3 startOrientation = Vector3.zero;
        protected Vector3 orientation = Vector3.zero;
        protected float roll = 0f;
        private Point[] points = new Point[0];
        private Point lastPointHL = new Point();

        public override void Initialize(LevelGenerator input)
        {
            base.Initialize(input);
            SetOrientation(startOrientation);
            roll = 0f;
            lastPointHL = new Point();
            points = new Point[0];
        }

        public override void Continue(LevelPathGenerator previousGenerator)
        {
            base.Continue(previousGenerator);
            if (!(previousGenerator is HighLevelPathGenerator))
            {
                Quaternion lookRot = Quaternion.LookRotation(previousGenerator.lastPoint.tangent2 - previousGenerator.lastPoint.position, previousGenerator.lastPoint.normal);
                orientation = lookRot.eulerAngles;
                return;
            }
            HighLevelPathGenerator previousHighLevel = (HighLevelPathGenerator)previousGenerator;
            orientation = previousHighLevel.orientation;
            roll = previousHighLevel.roll;
            lastPointHL = previousHighLevel.lastPointHL;
        }

        public override void Continue(LevelSegment segment)
        {
            base.Continue(segment);
            SplineSample sample = new SplineSample();
            segment.Evaluate(1.0, ref sample);
            SplinePoint lastPoint = segment.path.spline.points[segment.path.spline.points.Length - 1];
            lastPointHL.position = transform.InverseTransformPoint(lastPoint.position);
            lastPointHL.rotation = (Quaternion.Inverse(transform.rotation) * sample.rotation).eulerAngles;
            lastPointHL.size = sample.size;
            lastPointHL.color = sample.color;
        }

        public Vector3 GetCurrentOrientation()
        {
            return orientation;
        }

        protected override void OnOriginOffset(Vector3 direction)
        {
            base.OnOriginOffset(direction);
            lastPointHL.position -= direction;
        }

        protected void SetOrientation(Vector3 input)
        {
            orientation = input;
            if (orientation.x > 180f) orientation.x -= Mathf.FloorToInt(orientation.x / 180f) * 360f;
            if (orientation.y > 180f) orientation.y -= Mathf.FloorToInt(orientation.y / 180f) * 360f;
            if (orientation.z > 180f) orientation.z -= Mathf.FloorToInt(orientation.z / 180f) * 360f;
        }

        protected Vector3 GetPointPosition()
        {
            Vector3 direction = Quaternion.Euler(orientation) * Vector3.forward;
            float pointDistance = GetPointDistance();
            return lastPointHL.position + direction * pointDistance;
        }

        protected Vector3 GetPointNormal()
        {
            Vector3 direction = Quaternion.Euler(orientation) * Vector3.forward;
            return Quaternion.AngleAxis(roll, direction) * Vector3.up;
        }

        protected override void OnBeforeGeneration(SplinePoint[] splinePoints)
        {
            base.OnBeforeGeneration(splinePoints);
            lastPointHL.position = lastPoint.position;
            points = new Point[splinePoints.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Point();
                points[i].position = splinePoints[i].position;
            }
            if (segment.previous == null) return;
            if (segment.previous.customExit != null) SetOrientation(segment.previous.customExit.eulerAngles);
        }

        protected override void GeneratePoint(ref SplinePoint point, int pointIndex)
        {
            base.GeneratePoint(ref point, pointIndex);
            GeneratePoint(ref points[pointIndex], pointIndex);
            if (customRules.Length > 0)
            {
                for (int i = 0; i < customRules.Length; i++)
                {
                    customRules[i].OnGeneratePoint(points[pointIndex], lastPointHL, pointIndex, controlPointsPerSegment);
                }
            }
            lastPointHL.position = points[pointIndex].position;
            lastPointHL.rotation = points[pointIndex].rotation;
        }

        protected virtual void GeneratePoint(ref Point point, int pointIndex)
        {
            point.position = lastPointHL.position;
            point.rotation = lastPointHL.rotation;
            point.autoRotation = lastPointHL.autoRotation;
            point.size = lastPointHL.size;
            point.color = lastPointHL.color;
        }

        //Create the spline points using the simplified points array
        protected override void OnPostGeneration(SplinePoint[] splinePoints)
        {
            int start = segment.previous != null && segment.stitch ? 1 : 0;
            for (int i = start; i < points.Length; i++)
            {
                WritePoint(i, ref splinePoints[i]);
            }
            base.OnPostGeneration(splinePoints);
        }

        protected void WritePoint(int index, ref SplinePoint target)
        {
            Vector3 prevPos = Vector3.zero, forwardPos = Vector3.zero;
            if (index > 0) prevPos = points[index - 1].position;
            else if (points.Length > 2) prevPos = ExtrapolatePoint(points[2].position, points[1].position, points[0].position);
            else prevPos = points[0].position + (points[0].position - points[1].position);

            if (index < points.Length - 1) forwardPos = points[index + 1].position;
            else if (points.Length > 2) forwardPos = ExtrapolatePoint(points[index - 2].position, points[index - 1].position, points[index].position);
            else forwardPos = points[index].position + (points[index].position - points[index - 1].position);
            Vector3 delta = (forwardPos - prevPos) / 2f;

            target.position = points[index].position;
            if (points[index].autoRotation)
            {
                target.normal = Quaternion.AngleAxis(0f, delta) * Vector3.up;
                target.tangent = target.position - delta / 3f;
                target.tangent2 = target.position + delta / 3f;
            }
            else
            {
                Quaternion rot = Quaternion.Euler(points[index].rotation);
                target.normal = rot * Vector3.up;
                target.tangent = target.position - rot * Vector3.forward * delta.magnitude / 3f;
                target.tangent2 = target.position + (target.position - target.tangent);
            }
            if (useCustomNormalDirection) target.normal = customNormalDirection;
            target.size = points[index].size;
            target.color = points[index].color;
        }

    }
}
