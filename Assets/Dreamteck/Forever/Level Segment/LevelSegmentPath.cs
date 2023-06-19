namespace Dreamteck.Forever
{
    using UnityEngine;
    using Dreamteck.Splines;

    public partial class LevelSegment : MonoBehaviour
    {
        [System.Serializable]
        public class LevelSegmentPath : SplinePath
        {
            public string name = "Path";
            public Color color = Color.white;
            public bool seamlessEnds = true;
            public bool confineToBounds = false;
            [SerializeField]
            [HideInInspector]
            private LevelSegment segment;
            [SerializeField]
            [HideInInspector]
            private Transform transform;
            public SplinePoint[] localPoints = new SplinePoint[0];

            internal LevelSegmentPath(LevelSegment s)
            {
                segment = s;
                transform = s.transform;
                spline = new Spline(Spline.Type.Bezier);
            }

            public void Transform()
            {
                if (spline == null || localPoints == null) return;
                if (spline.points.Length != localPoints.Length) spline.points = new SplinePoint[localPoints.Length];
                for (int i = 0; i < localPoints.Length; i++)
                {
                    if (confineToBounds && segment.bounds.size != Vector3.zero)
                    {
                        Vector3 pos = localPoints[i].position;
                        pos.x = Mathf.Clamp(localPoints[i].position.x, segment.bounds.min.x, segment.bounds.max.x);
                        pos.y = Mathf.Clamp(localPoints[i].position.y, segment.bounds.min.y, segment.bounds.max.y);
                        pos.z = Mathf.Clamp(localPoints[i].position.z, segment.bounds.min.z, segment.bounds.max.z);
                        localPoints[i].SetPosition(pos);
                    }

                    spline.points[i].size = localPoints[i].size;
                    spline.points[i].color = localPoints[i].color;
                    TransformPoint(ref localPoints[i], ref spline.points[i]);
                }
            }

            public void InverseTransform()
            {
                if (spline == null || localPoints == null) return;
                if (spline.points.Length != localPoints.Length) localPoints = new SplinePoint[spline.points.Length];
                for (int i = 0; i < localPoints.Length; i++)
                {
                    localPoints[i].size = spline.points[i].size;
                    localPoints[i].color = spline.points[i].color;
                    InverseTransformPoint(ref spline.points[i], ref localPoints[i]);
                }
            }

            public LevelSegmentPath Copy()
            {
                LevelSegmentPath newPath = new LevelSegmentPath(segment);
                newPath.name = name;
                newPath.localPoints = new SplinePoint[localPoints.Length];
                localPoints.CopyTo(newPath.localPoints, 0);
                newPath.spline = new Spline(spline.type, spline.sampleRate);
                newPath.Transform();
                return newPath;
            }

            private void TransformPoint(ref SplinePoint source, ref SplinePoint target)
            {
                target.position = transform.TransformPoint(source.position);
                target.tangent = transform.TransformPoint(source.tangent);
                target.tangent2 = transform.TransformPoint(source.tangent2);
                target.normal = transform.TransformDirection(source.normal).normalized;
            }

            private void InverseTransformPoint(ref SplinePoint source, ref SplinePoint target)
            {
                target.position = transform.InverseTransformPoint(source.position);
                target.tangent = transform.InverseTransformPoint(source.tangent);
                target.tangent2 = transform.InverseTransformPoint(source.tangent2);
                target.normal = transform.InverseTransformDirection(source.normal).normalized;
            }
        }
    }
}
