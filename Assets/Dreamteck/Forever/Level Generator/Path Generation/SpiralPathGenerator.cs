using UnityEngine;
using Dreamteck.Splines;

namespace Dreamteck.Forever
{

    public class SpiralPathGenerator : LevelPathGenerator
    {
        public enum Axis { X, Y, Z, NegativeX, NegativeY, NegativeZ}
        public Axis axis = Axis.Y;


        public float spinRate = 10f;
        public float steepness = 10f;
        [Range(-360f, 360f)]
        public float normalRotation = 0f;

        private float spin = 0f;

        public override void Initialize(LevelGenerator input)
        {
            base.Initialize(input);
        }

        protected override void GeneratePoint(ref SplinePoint point, int pointIndex)
        {
            base.GeneratePoint(ref point, pointIndex);
            Vector3 right = Vector3.forward;
            Vector3 up = Vector3.up;
            Vector3 forward = Vector3.forward;

            switch (axis)
            {
                case Axis.X: right = Vector3.up;  up = Vector3.left; forward = Vector3.forward; break;
                case Axis.Y: right = Vector3.right;  up = Vector3.up; forward = Vector3.forward; break;
                case Axis.Z: right = Vector3.right; up = Vector3.back; forward = Vector3.up; break;
                case Axis.NegativeX: right = Vector3.down; up = Vector3.right; forward = Vector3.forward; break;
                case Axis.NegativeY: right = Vector3.left; up = Vector3.down; forward = Vector3.forward; break;
                case Axis.NegativeZ: right = Vector3.left; up = Vector3.forward; forward = Vector3.up; break;
            }
            Quaternion steepnessRot = Quaternion.AngleAxis(steepness, right);
            point.position = lastPoint.position;
            if(!isFirstPoint) point.position += Quaternion.AngleAxis(spin, up) * steepnessRot * forward * GetPointDistance();
            Vector3 tanDir = Quaternion.AngleAxis(spin + spinRate * 0.5f, up) * steepnessRot * forward;
            float radius = (GetPointDistance()) / ((spinRate * 0.9f) * Mathf.Deg2Rad);
            float pointsPerTurn = 360f / spinRate;
            Vector3 tangentDirection = tanDir * radius * pointsPerTurn * Mathf.Tan((spinRate * Mathf.Deg2Rad) / pointsPerTurn) / 3f;
            point.tangent = point.position - tangentDirection;
            point.tangent2 = point.position + tangentDirection;
            tanDir.y = 0f;
            point.normal = Quaternion.AngleAxis(normalRotation, tanDir) * up;
            spin += spinRate;
            if (spin > 360f) spin -= 360f;
        }
    }
}
