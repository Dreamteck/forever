namespace Dreamteck.Forever.Editor
{
    using UnityEditor;
    using UnityEngine;
    using Dreamteck.Splines;

    public static class LevelSegmentDebug
    {

        public static void DrawCustomPaths(LevelSegment segment)
        {
            if (segment.alwaysDraw) return;
            for (int i = 0; i < segment.customPaths.Length; i++)
            {
                Color col = segment.customPaths[i].color;
                col.a *= 0.5f;
                segment.customPaths[i].Transform();
                Splines.Editor.SplineDrawer.DrawSpline(segment.customPaths[i].spline, col);
            }
        }

        public static void DrawBounds(LevelSegment segment)
        {
            if (segment.alwaysDraw) return;
            TS_Bounds bound = segment.GetBounds();
            Transform trs = segment.transform;
            Vector3 a = trs.TransformPoint(bound.min);
            Vector3 b = trs.TransformPoint(new Vector3(bound.max.x, bound.min.y, bound.min.z));
            Vector3 c = trs.TransformPoint(new Vector3(bound.max.x, bound.min.y, bound.max.z));
            Vector3 d = trs.TransformPoint(new Vector3(bound.min.x, bound.min.y, bound.max.z));

            Vector3 e = trs.TransformPoint(new Vector3(bound.min.x, bound.max.y, bound.min.z));
            Vector3 f = trs.TransformPoint(new Vector3(bound.max.x, bound.max.y, bound.min.z));
            Vector3 g = trs.TransformPoint(new Vector3(bound.max.x, bound.max.y, bound.max.z));
            Vector3 h = trs.TransformPoint(new Vector3(bound.min.x, bound.max.y, bound.max.z));

            Handles.color = Color.green;
            Handles.DrawLine(a, b);
            Handles.DrawLine(b, c);
            Handles.DrawLine(c, d);
            Handles.DrawLine(d, a);

            Handles.DrawLine(e, f);
            Handles.DrawLine(f, g);
            Handles.DrawLine(g, h);
            Handles.DrawLine(h, e);

            Handles.DrawLine(a, e);
            Handles.DrawLine(b, f);
            Handles.DrawLine(c, g);
            Handles.DrawLine(d, h);
        }

        public static void DrawGeneratedSpline(LevelSegment segment)
        {
            Vector3 cameraPos = SceneView.currentDrawingSceneView.camera.transform.position;
            Handles.color = ForeverPrefs.pointColor;
            //Debug spline points
            if (segment.path != null)
            {
                for (int i = 0; i < segment.path.spline.points.Length; i++)
                {
                    Vector3 pos = segment.path.spline.points[i].position;
                    float handleSize = HandleUtility.GetHandleSize(segment.path.spline.points[i].position);
                    Handles.DrawSolidDisc(pos, cameraPos - pos, handleSize * 0.1f);
                    Handles.DrawLine(pos, pos + segment.path.spline.points[i].normal * segment.drawSampleScale);
                    if (segment.path.spline.type == Spline.Type.Bezier)
                    {
                        Handles.DrawDottedLine(segment.path.spline.points[i].position, segment.path.spline.points[i].tangent, 10f);
                        Handles.DrawDottedLine(segment.path.spline.points[i].position, segment.path.spline.points[i].tangent2, 10f);
                        Handles.DrawWireDisc(segment.path.spline.points[i].tangent, cameraPos - segment.path.spline.points[i].tangent, handleSize * 0.075f);
                        Handles.DrawWireDisc(segment.path.spline.points[i].tangent2, cameraPos - segment.path.spline.points[i].tangent2, handleSize * 0.075f);
                    }
                }
            }
        }

        public static void DrawGeneratedSamples(LevelSegment segment)
        {
            //Debug spline samples
            for (int i = 0; i < segment.path.samples.Length; i++)
            {
                Vector3 pos = segment.path.samples[i].position;
                Vector3 right = segment.path.samples[i].right;
                Vector3 normal = segment.path.samples[i].up;
                float size = segment.path.samples[i].size;
                Handles.DrawLine(pos - right * segment.drawSampleScale * 0.5f * size, pos + right * segment.drawSampleScale * 0.5f * size);
                Handles.DrawLine(pos, pos + normal * segment.drawSampleScale * 0.5f);
            }
            Handles.color = Color.white;
        }
    }
}
