using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dreamteck.Splines;

namespace Dreamteck.Forever
{
    public class CustomPathRule : MonoBehaviour
    {
        public LevelSegment segment = null;

        public virtual void OnBeforeGeneration(LevelPathGenerator generator)
        {

        }

        public virtual void OnGeneratePoint(SplinePoint point, SplinePoint previous, int pointIndex, int pointCount)
        {

        }

        public virtual void OnGeneratePoint(HighLevelPathGenerator.Point point, HighLevelPathGenerator.Point previous, int pointIndex, int pointCount)
        {

        }

        public virtual void OnPostGeneration(SplinePoint[] points)
        {

        }
    }
}
