namespace Dreamteck.Forever
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Dreamteck.Splines;

    public class SplinePath
    {
        public enum EvaluateMode { Cached, Accurate }
        public Spline spline;
        public int sampleRate = 10;
        public SplineSample[] samples = new SplineSample[0];

        /// <summary>
        /// Sample the spline and write the results into the samples array
        /// </summary>
        public virtual void CalculateSamples()
        {
            int sampleCount = sampleRate * (spline.points.Length - 1);
            if (spline.type == Spline.Type.Linear) sampleCount = spline.points.Length;
            samples = new SplineSample[sampleCount + 1];
            for (int i = 0; i < sampleCount; i++) samples[i] = spline.Evaluate((double)i / sampleCount);
            samples[sampleCount] = spline.Evaluate(1.0);
        }

        /// <summary>
        /// Evaluate the generated path of the segment. This is the path after the segment is extruded.
        /// </summary>
        /// <param name="percent">Percent along the segment path [0-1]</param>
        /// <param name="result">The SplineSample object to write the result into</param>
        /// <param name="mode">If set to EvaluateMode.Accurate, the actual spline will be evaluated instead of the cached samples.</param>
        public virtual void Evaluate(double percent, ref SplineSample result, EvaluateMode mode = EvaluateMode.Cached)
        {
            if (mode == EvaluateMode.Accurate)
            {
                spline.Evaluate(percent, ref result);
                return;
            }
            if (samples.Length == 0) return;
            percent = DMath.Clamp01(percent);
            int index = DMath.FloorInt(percent * (samples.Length - 1));
            double percentExcess = (samples.Length - 1) * percent - index;
            result = samples[index];
            if (percentExcess > 0.0 && index < samples.Length - 1)
            {
                result.Lerp(ref samples[index + 1], percentExcess);
            }
        }

        /// <summary>
        /// Evaluate the generated path of the segment and return only the position. This is the path after the segment is extruded.
        /// </summary>
        /// <param name="percent">Percent along the segment path [0-1]</param>
        /// <param name="mode">If set to EvaluateMode.Accurate, the actual spline will be evaluated instead of the cached samples.</param>
        /// <returns></returns>
        public virtual Vector3 EvaluatePosition(double percent, EvaluateMode mode = EvaluateMode.Cached)
        {
            if (mode == EvaluateMode.Accurate) return spline.EvaluatePosition(percent);
            if (samples.Length == 0) return Vector3.zero;
            if (samples.Length == 1) return samples[0].position;
            percent = DMath.Clamp01(percent);
            int index = DMath.FloorInt(percent * (samples.Length - 1));
            double percentExcess = (samples.Length - 1) * percent - index;
            if (percentExcess > 0.0 && index < samples.Length - 1) return Vector3.Lerp(samples[index].position, samples[index + 1].position, (float)percentExcess);
            else return samples[index].position;
        }

        /// <summary>
        /// Calculates the length of the generated path in world units.
        /// </summary>
        /// <param name="from">The start of the segment to calculate the length of</param>
        /// <param name="to">The end of the segment</param>
        /// <returns></returns>
        public virtual float CalculateLength(double from = 0.0, double to = 1.0, EvaluateMode mode = EvaluateMode.Cached)
        {
            if (mode == EvaluateMode.Accurate) return spline.CalculateLength(from, to);
            float length = 0f;
            Vector3 pos = EvaluatePosition(from);
            int sampleIndex = DMath.CeilInt(from * (samples.Length - 1));
            int endSampleIndex = DMath.FloorInt(to * (samples.Length - 1));
            for (int i = sampleIndex; i < endSampleIndex; i++)
            {
                length += Vector3.Distance(samples[i].position, pos);
                pos = samples[i].position;
            }
            length += Vector3.Distance(EvaluatePosition(to), pos);
            return length;
        }

        /// <summary>
        /// Returns the percent from the spline at a given distance from the start point
        /// </summary>
        /// <param name="start">The start point</param>
        /// <param name="distance">The distance to travel</param>
        /// <param name="direction">The direction towards which to move</param>
        /// <param name="traveled">Returns the distance actually traveled. Usually equal to distance but could be less if the travel distance exceeds the remaining spline length.</param>
        /// <param name="mode">If set to EvaluateMode.Accurate, the actual spline will be evaluated instead of the cached samples.</param>
        /// <returns></returns>
        public virtual double Travel(double start, float distance, Spline.Direction direction, out float traveled, EvaluateMode mode = EvaluateMode.Cached)
        {
            traveled = 0f;
            if (mode == EvaluateMode.Accurate) return spline.Travel(start, distance, direction);
            if (samples.Length <= 1) return 0.0;
            if (direction == Spline.Direction.Forward && start >= 1.0) return 1.0;
            else if (direction == Spline.Direction.Backward && start <= 0.0) return 0.0;
            if (distance == 0f) return DMath.Clamp01(start);
            Vector3 lastPosition = EvaluatePosition(start);
            double lastPercent = start;
            int nextSampleIndex = direction == Spline.Direction.Forward ? DMath.CeilInt(start * (samples.Length - 1)) : DMath.FloorInt(start * (samples.Length - 1));
            float lastDistance = 0f;
            while (true)
            {
                lastDistance = Vector3.Distance(samples[nextSampleIndex].position, lastPosition);
                lastPosition = samples[nextSampleIndex].position;
                traveled += lastDistance;
                if (traveled >= distance) break;
                lastPercent = samples[nextSampleIndex].percent;
                if (direction == Spline.Direction.Forward)
                {
                    if (nextSampleIndex == samples.Length - 1) break;
                    nextSampleIndex++;
                }
                else
                {
                    if (nextSampleIndex == 0) break;
                    nextSampleIndex--;
                }
            }
            return DMath.Lerp(lastPercent, samples[nextSampleIndex].percent, 1f - (traveled - distance) / lastDistance);
        }

        /// <summary>
        /// Project a world space point onto the spline and write to a SplineSample.
        /// </summary>
        /// <param name="point">3D Point in world space</param>
        /// <param name="result">The SplineSample object to write the result into</param>
        /// <param name="from">Sample from [0-1] default: 0.0</param>
        /// <param name="to">Sample to [0-1] default: 1.0</param>
        /// <returns></returns>
        public virtual void Project(Vector3 point, ref SplineSample result, double from = 0.0, double to = 1.0, EvaluateMode mode = EvaluateMode.Cached)
        {
            if (mode == EvaluateMode.Accurate)
            {
                spline.Evaluate(spline.Project(point, 4, from, to), ref result);
                return;
            }
            if (samples.Length == 0) return;
            if (samples.Length == 1)
            {
                result = samples[0];
                return;
            }
            //First make a very rough sample of the from-to region 
            int steps = 2;
            if (spline != null) steps = (spline.points.Length - 1) * 6; //Sampling six points per segment is enough to find the closest point range
            int step = samples.Length / steps;
            if (step < 1) step = 1;
            float minDist = (point - samples[0].position).sqrMagnitude;
            int fromIndex = 0;
            int toIndex = samples.Length - 1;
            if (from != 0.0) fromIndex = DMath.FloorInt(from * (samples.Length - 1));
            if (to != 1.0) toIndex = Mathf.CeilToInt((float)to * (samples.Length - 1));
            int checkFrom = fromIndex;
            int checkTo = toIndex;

            //Find the closest point range which will be checked in detail later
            for (int i = fromIndex; i <= toIndex; i += step)
            {
                if (i > toIndex) i = toIndex;
                float dist = (point - samples[i].position).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    checkFrom = Mathf.Max(i - step, 0);
                    checkTo = Mathf.Min(i + step, samples.Length - 1);
                }
                if (i == toIndex) break;
            }
            minDist = (point - samples[checkFrom].position).sqrMagnitude;

            int index = checkFrom;
            //Find the closest result within the range
            for (int i = checkFrom + 1; i <= checkTo; i++)
            {
                float dist = (point - samples[i].position).sqrMagnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    index = i;
                }
            }
            //Project the point on the line between the two closest samples
            int backIndex = index - 1;
            if (backIndex < 0) backIndex = 0;
            int frontIndex = index + 1;
            if (frontIndex > samples.Length - 1) frontIndex = samples.Length - 1;
            Vector3 back = LinearAlgebraUtility.ProjectOnLine(samples[backIndex].position, samples[index].position, point);
            Vector3 front = LinearAlgebraUtility.ProjectOnLine(samples[index].position, samples[frontIndex].position, point);
            float backLength = (samples[index].position - samples[backIndex].position).magnitude;
            float frontLength = (samples[index].position - samples[frontIndex].position).magnitude;
            float backProjectDist = (back - samples[backIndex].position).magnitude;
            float frontProjectDist = (front - samples[frontIndex].position).magnitude;
            if (backIndex < index && index < frontIndex)
            {
                if ((point - back).sqrMagnitude < (point - front).sqrMagnitude) SplineSample.Lerp(ref samples[backIndex], ref samples[index], backProjectDist / backLength, ref result);
                else SplineSample.Lerp(ref samples[frontIndex], ref samples[index], frontProjectDist / frontLength, ref result);
            }
            else if (backIndex < index) SplineSample.Lerp(ref samples[backIndex], ref samples[index], backProjectDist / backLength, ref result);
            else SplineSample.Lerp(ref samples[frontIndex], ref samples[index], frontProjectDist / frontLength, ref result);
        }
    }
}