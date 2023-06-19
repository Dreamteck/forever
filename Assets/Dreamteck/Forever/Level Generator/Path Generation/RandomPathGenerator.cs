using UnityEngine;
using Dreamteck.Splines;

namespace Dreamteck.Forever
{
    public class RandomPathGenerator : HighLevelPathGenerator
    {
        public ForeverRandomizer randomizer;
        [Space()]
        [HideInInspector]
        public bool restrictPitch = false;
        [HideInInspector]
        public bool restrictYaw = false;
        [HideInInspector]
        public bool restrictRoll = false;
        [HideInInspector]
        public bool usePitch = false;
        [HideInInspector]
        public bool useYaw = false;
        [HideInInspector]
        public bool useRoll = false;
        [HideInInspector]
        public bool useColors = false;
        [HideInInspector]
        public bool useSizes = false;

        [HideInInspector]
        public Vector3 minOrientation = Vector3.zero;
        [HideInInspector]
        public Vector3 maxOrientation = Vector3.zero;
        [HideInInspector]
        public Vector3 minRandomStep = Vector3.zero;
        [HideInInspector]
        public Vector3 maxRandomStep = Vector3.zero;
        [HideInInspector]
        public Vector3 minTurnRate = Vector3.zero;
        [HideInInspector]
        public Vector3 maxTurnRate = Vector3.zero;
        [HideInInspector]
        public Vector3 startTargetOrientation = Vector3.zero;
        [HideInInspector]
        public bool useStartPitchTarget = false;
        [HideInInspector]
        public bool useStartYawTarget = false;
        [HideInInspector]
        public bool useStartRollTarget = false;

        [HideInInspector]
        public Gradient minColor = new Gradient();
        [HideInInspector]
        public Gradient maxColor = new Gradient();

        [HideInInspector]
        public AnimationCurve minSize = new AnimationCurve();
        [HideInInspector]
        public AnimationCurve maxSize = new AnimationCurve();

        [HideInInspector]
        public Vector3 minSegmentOffset = Vector3.zero;
        [HideInInspector]
        public Vector3 maxSegmentOffset = Vector3.zero;
        [HideInInspector]
        public Space segmentOffsetSpace = Space.World;
        [HideInInspector]
        public Vector3 newLevelMinOffset = Vector3.zero;
        [HideInInspector]
        public Vector3 newLevelMaxOffset = Vector3.zero;
        [HideInInspector]
        public Space levelOffsetSpace = Space.World;

        protected Vector3 turnRate = Vector3.zero;
        protected Vector3 targetAngle = Vector3.zero;

        protected override void GeneratePoint(ref Point point, int pointIndex)
        {
            if (isFirstPoint)
            {
                point.position = lastPoint.position;
                point.rotation = orientation;
            } else { 
                orientation = MoveOrientation(orientation);
                if (useYaw && orientation.y == targetAngle.y)
                {
                    targetAngle.y = MoveTargetAngle(targetAngle.y, minRandomStep.y, maxRandomStep.y, restrictYaw, minOrientation.y, maxOrientation.y);
                    turnRate.y = randomizer.Next(minTurnRate.y, maxTurnRate.y);
                }
                if (usePitch && orientation.x == targetAngle.x)
                {
                    targetAngle.x = MoveTargetAngle(targetAngle.x, minRandomStep.x, maxRandomStep.x, restrictPitch, minOrientation.x, maxOrientation.x);
                    turnRate.x = randomizer.Next(minTurnRate.x, maxTurnRate.x);
                }
                if (useRoll && orientation.z == targetAngle.z)
                {
                    targetAngle.z = MoveTargetAngle(targetAngle.z, minRandomStep.z, maxRandomStep.z, restrictRoll, minOrientation.z, maxOrientation.z);
                    turnRate.z = randomizer.Next(minTurnRate.z, maxTurnRate.z);
                }
                Vector3 nextOrientation = MoveOrientation(orientation);
                point.position = GetPointPosition();
                point.rotation.x = Mathf.LerpAngle(orientation.x, nextOrientation.x, 0.5f);
                point.rotation.y = Mathf.LerpAngle(orientation.y, nextOrientation.y, 0.5f);
                point.rotation.z = orientation.z;
            }
            point.autoRotation = false;

            float progress = (float)pointIndex / (controlPointsPerSegment - 1);
            if (useColors) point.color = Color.Lerp(minColor.Evaluate(progress), maxColor.Evaluate(progress), randomizer.NextF());
            if (useSizes) point.size = Mathf.Lerp(minSize.Evaluate(progress), maxSize.Evaluate(progress), randomizer.NextF());
        }

        Vector3 MoveOrientation(Vector3 input)
        {
            if (restrictPitch) input.x = Mathf.MoveTowards(input.x, targetAngle.x, turnRate.x);
            else input.x = Mathf.MoveTowardsAngle(input.x, targetAngle.x, turnRate.x);
            if (restrictYaw) input.y = Mathf.MoveTowards(input.y, targetAngle.y, turnRate.y);
            else input.y = Mathf.MoveTowardsAngle(input.y, targetAngle.y, turnRate.y);
            if (restrictRoll)  input.z = Mathf.MoveTowards(input.z, targetAngle.z, turnRate.z);
            else input.z = Mathf.MoveTowardsAngle(input.z, targetAngle.z, turnRate.z);
            return input; 
        }

        float MoveTargetAngle(float input, float minStep, float maxStep, bool restrict, float minTarget, float maxTarget)
        {
            float direction = randomizer.Next(0, 100) > 50f ? 1f : -1f;
            float move = randomizer.Next(minStep, maxStep) * direction;
            input += move;
            if (restrict)
            {
                input = Mathf.Clamp(input, minTarget, maxTarget);
            }
            else
            {
                if (input > 360f) input -= Mathf.FloorToInt(input / 360f) * 360f;
                else if (input < 0f) input += Mathf.FloorToInt(-input / 360f) * 360f;
            }
            return input;
        }

        protected override void OnPostGeneration(SplinePoint[] points)
        {
            base.OnPostGeneration(points);
            
            if (minSegmentOffset != Vector3.zero || maxSegmentOffset != Vector3.zero)
            {
                OffsetPoints(points, Vector3.Lerp(minSegmentOffset, maxSegmentOffset, randomizer.Next(0f, 1f)), segmentOffsetSpace);
            }
            
            if (isNewLevel && (newLevelMinOffset != Vector3.zero || newLevelMaxOffset != Vector3.zero))
            {
                OffsetPoints(points, Vector3.Lerp(newLevelMinOffset, newLevelMaxOffset, randomizer.Next(0f, 1f)), levelOffsetSpace);
            }
        }

        public override void Initialize(LevelGenerator input)
        {
            base.Initialize(input);
            InitAngles();
        }

        public override void Continue(LevelPathGenerator previousGenerator)
        {
            if(previousGenerator is HighLevelPathGenerator)
            {
                HighLevelPathGenerator hlGen = (HighLevelPathGenerator)previousGenerator;
                orientation = hlGen.GetCurrentOrientation();
            }
            InitAngles();
            base.Continue(previousGenerator);
        }

        public override void Continue(LevelSegment segment)
        {
            InitAngles();
            base.Continue(segment);
        }

        private void InitAngles()
        {
            if(randomizer == null)
            {
                Debug.LogError("Path generator " + name + " is missing a Randomizer");
                return;
            }
            randomizer.Initialize();
            turnRate = new Vector3(randomizer.Next(minTurnRate.x, maxTurnRate.x), randomizer.Next(minTurnRate.y, maxTurnRate.y), randomizer.Next(minTurnRate.z, maxTurnRate.z));
            targetAngle = orientation;
            if (useStartYawTarget)
            {
                targetAngle.y = startTargetOrientation.y;
            }
            if (useStartPitchTarget)
            {
                targetAngle.x = startTargetOrientation.x;
            }
            if (useStartRollTarget)
            {
                targetAngle.z = startTargetOrientation.z;
            }
        }

        public void SetTargetPitch(float input)
        {
            targetAngle.x = input;
        }

        public void SetTargetYaw(float input)
        {
            targetAngle.y = input;
        }

        public void SetTargetRoll(float input)
        {
            targetAngle.z = input;
        }

        public void SetPitchStep(float input)
        {
            turnRate.x = input;
        }

        public void SetYawStep(float input)
        {
            turnRate.y = input;
        }

        public void SetRollStep(float input)
        {
            turnRate.z = input;
        }

        public void SetTargetAngle(Vector3 target)
        {
            targetAngle = target;
        }

        public void SetTargetStep(Vector3 target)
        {
            turnRate = target;
        }

        protected void OffsetPoints(SplinePoint[] points, Vector3 offset, Space space)
        {
            if (offset != Vector3.zero) segment.stitch = false;
            SplineSample result = new SplineSample();
            if (space == Space.Self && segment.previous != null)
            {
                segment.previous.Evaluate(1.0, ref result);
                offset = result.forward * offset.z + result.right * offset.x + result.up * offset.y;
            }

            transform.InverseTransformDirection(offset);
            for (int i = 0; i < points.Length; i++)
            {
                points[i].SetPosition(points[i].position + offset);
            }
        }
    }
}
