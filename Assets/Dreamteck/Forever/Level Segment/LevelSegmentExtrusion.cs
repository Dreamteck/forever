namespace Dreamteck.Forever
{
    using UnityEngine;
    using Dreamteck.Splines;
    using System.Collections.Generic;
    using System.Collections;

    public partial class LevelSegment : MonoBehaviour
    {
        public const int BEFORE_EXTRUDE_ACTION_FRAMES = 10;
        public const int POST_EXTRUDE_ACTION_FRAMES = 10;

        private SplineSample extrudeResult = new SplineSample();
        private Matrix4x4 extrudeMatrix = new Matrix4x4();
        private Quaternion extrudeRotation = Quaternion.identity;
        public event System.Action onExtruded;
        private bool _extruded = false;
        public bool extruded
        {
            get { return _extruded; }
        }

        /// <summary>
        /// Beginning of the extrusion sequence. Called for initialization.
        /// </summary>
        public virtual IEnumerator OnBeforeExtrude()
        {
            rootMatrix = transform.localToWorldMatrix;
            inverseRootMatrix = rootMatrix.inverse;
            
            if (type == Type.Custom)
            {
                yield return _generator.ScheduleAsyncJob(new AsyncJobSystem.JobData<LevelSegmentPath>
                (
                    customPaths,
                    customPaths.Length / BEFORE_EXTRUDE_ACTION_FRAMES,
                    (AsyncJobSystem.JobData<LevelSegmentPath> data) =>
                    {
                        data.current.Transform();
                    }
                ));
            }

            yield return _generator.ScheduleAsyncJob(new AsyncJobSystem.JobData<ObjectProperty>
            (
                objectProperties,
                objectProperties.Length / BEFORE_EXTRUDE_ACTION_FRAMES,
                (AsyncJobSystem.JobData<ObjectProperty> data) =>
                {
                    data.current.RuntimeInitialize();
                }
            ));

            Evaluate(0.0, ref extrudeResult);

            if(type == Type.Custom)
            {
                path.samples = new SplineSample[2];
                path.samples[0] = new SplineSample(customEntrance.position, customEntrance.up, customEntrance.forward, Color.white, 1f, 0.0);
                path.samples[1] = new SplineSample(customExit.position, customExit.up, customExit.forward, Color.white, 1f, 1.0);
            }
        }

        /// <summary>
        /// Called by the SegmentExtruder on the main thread or on a separate thread to perform extrusion calculations
        /// </summary>
        public void Extrude()
        {
            if (type == Type.Custom)
            {
                for (int i = 0; i < customPaths.Length; i++)
                {
                    customPaths[i].CalculateSamples();
                }
                return;
            } else path.CalculateSamples();
            
            //Make the first result the same as the previous one to prevent seams (stitch)
            if (stitch && _previous != null)
            {
                path.samples[0] = _previous.path.samples[_previous.path.samples.Length - 1];
                path.samples[0].percent = 0.0;
            }
            for (int i = 0; i < objectProperties.Length; i++)
            {
                ExtrudeObject(objectProperties[i]);
            }

            for (int i = 0; i < customPaths.Length; i++)
            {
                Spline spline = customPaths[i].spline;
                SplinePoint[] localPoints = customPaths[i].localPoints;
                if (spline.points.Length != localPoints.Length)
                {
                    spline.points = new SplinePoint[localPoints.Length];
                    localPoints.CopyTo(spline.points, 0);
                }
                for (int j = 0; j < localPoints.Length; j++)
                {
                    GetExtrudeResult(GetPercentage(customPaths[i].localPoints[j].position));
                    Quaternion pointRotation = extrudeRotation;
                    Vector3 tangent1Delta = localPoints[j].tangent - localPoints[j].position;
                    Vector3 tangent2Delta = localPoints[j].tangent2 - localPoints[j].position;
                    spline.points[j].position = extrudeResult.position;
                    spline.points[j].normal = extrudeMatrix.MultiplyVector(localPoints[j].normal);

                    GetExtrudeResult(GetPercentage(customPaths[i].localPoints[j].tangent));
                    Vector3 tan1Pos = extrudeResult.position;
                    GetExtrudeResult(GetPercentage(customPaths[i].localPoints[j].tangent2));
                    Vector3 tan2Pos = extrudeResult.position;

                    spline.points[j].tangent = tan1Pos;
                    spline.points[j].tangent2 = tan2Pos;

                    if (spline.points[j].type != SplinePoint.Type.Broken && j > 0 && j < localPoints.Length-1)
                    {
                        Quaternion tan1Rot = Quaternion.LookRotation(spline.points[j].position - tan1Pos, spline.points[j].normal);
                        Quaternion tan2Rot = Quaternion.LookRotation(tan2Pos - spline.points[j].position, spline.points[j].normal);
                        Quaternion averageRot = Quaternion.Slerp(tan1Rot, tan2Rot, 0.5f);
                        spline.points[j].tangent = spline.points[j].position + averageRot * tangent1Delta;
                        spline.points[j].tangent2 = spline.points[j].position + averageRot * tangent2Delta;
                    }

                    if (customPaths[i].seamlessEnds)
                    {
                        if (j == 0) spline.points[j].SetTangent2Position(spline.points[j].position + pointRotation * tangent2Delta);
                        else if (j == spline.points.Length - 1) spline.points[j].SetTangentPosition(spline.points[j].position + pointRotation * tangent1Delta);
                    }
                }
                customPaths[i].CalculateSamples();
            }
        }

        /// <summary>
        /// Logic for extruding a single object inside the level segment
        /// </summary>
        /// <param name="p">The object property associated with the object</param>
        public void ExtrudeObject(ObjectProperty p)
        {
            if (p.extrusionSettings.ignore) return;
            if (p.error) return;
            GetExtrudeResult(GetPercentage(inverseRootMatrix.MultiplyPoint3x4(p.localToWorldMatrix.MultiplyPoint3x4(Vector3.zero))));
            p.targetPosition = extrudeResult.position;
            p.targetRotation = TransformUtility.GetRotation(p.localToWorldMatrix);
            Quaternion originalRotation = p.targetRotation;
            if (p.extrusionSettings.applyRotation)
            {
                Quaternion rot = extrudeRotation;
                if (p.extrusionSettings.keepUpright)
                {
                    rot = GetUprightRotation(extrudeRotation, p.extrusionSettings.upVector);
                }

                p.targetRotation = rot;
                if (!p.isRoot)
                {
                    p.targetRotation *= p.localRotation;
                }

            }

            p.scaleMultiplier = extrudeResult.size;

            Matrix4x4 toWorldMatrix = Matrix4x4.TRS(p.targetPosition, p.targetRotation, p.localScale);
            Matrix4x4 toLocalMatrix = toWorldMatrix.inverse;
            Quaternion rotationDelta = Quaternion.Inverse(originalRotation) * p.targetRotation;

            if (p.extrusionSettings.bendMesh)
            {
                if (p.extrusionSettings.applyMeshColors && p.extrusionMesh.colors.Length < p.extrusionMesh.vertexCount)
                {
                    p.extrusionMesh.colors = new Color[p.extrusionMesh.vertexCount];
                    for (int i = 0; i < p.extrusionMesh.vertexCount; i++) p.extrusionMesh.colors[i] = Color.white;
                }
                for (int i = 0; i < p.extrusionMesh.vertexCount; i++)
                {
                    GetExtrudeResult(GetPercentage(inverseRootMatrix.MultiplyPoint3x4(p.localToWorldMatrix.MultiplyPoint3x4(p.extrusionMesh.vertices[i]))));
                    Matrix4x4 normalMatrix = Matrix4x4.TRS(extrudeResult.position, extrudeRotation, Vector3.one);
                    p.extrusionMesh.vertices[i] = toLocalMatrix.MultiplyPoint3x4(extrudeResult.position);
                    Vector3 worldNormal;
                    if (p.isRoot) worldNormal = normalMatrix.MultiplyVector(p.extrusionMesh.normals[i]);
                    else worldNormal = p.localRotation * normalMatrix.MultiplyVector(p.extrusionMesh.normals[i]);
                    p.extrusionMesh.normals[i] = toLocalMatrix.MultiplyVector(worldNormal);
                    if (p.extrusionSettings.applyMeshColors) p.extrusionMesh.colors[i] *= extrudeResult.color;
                }
            }

            if (p.extrusionSettings.bendSprite)
            {
                for (int i = 0; i < p.extrusionSpriteVertices.Length; i++)
                {
                    GetExtrudeResult(GetPercentage(inverseRootMatrix.MultiplyPoint3x4(p.localToWorldMatrix.MultiplyPoint3x4(p.extrusionSpriteVertices[i]))));
                    p.extrusionSpriteVertices[i] = toLocalMatrix.MultiplyPoint3x4(extrudeResult.position);
                }
            }

            if (p.extrusionSettings.meshColliderHandling == ExtrusionSettings.MeshColliderHandling.Extrude)
            {
                for (int i = 0; i < p.extrusionCollisionMesh.vertexCount; i++)
                {
                    GetExtrudeResult(GetPercentage(inverseRootMatrix.MultiplyPoint3x4(p.localToWorldMatrix.MultiplyPoint3x4(p.extrusionCollisionMesh.vertices[i]))));
                    p.extrusionCollisionMesh.vertices[i] = toLocalMatrix.MultiplyPoint3x4(extrudeResult.position);
                    p.extrusionCollisionMesh.normals[i] = toLocalMatrix.MultiplyVector(extrudeMatrix.MultiplyVector(p.localToWorldMatrix.MultiplyVector(p.extrusionCollisionMesh.normals[i])));
                }
            }

#if DREAMTECK_SPLINES
            if (p.extrusionSettings.bendSpline && p.splineComputer != null)
            {
                SplinePoint[] points = p.splineComputer.GetPoints();
                for (int i = 0; i < points.Length; i++)
                {
                    GetExtrudeResult(GetPercentage(inverseRootMatrix.MultiplyPoint3x4(points[i].position)));
                    Vector3 tangent1Delta = p.editSplinePoints[i].tangent - p.editSplinePoints[i].position;
                    Vector3 tangent2Delta = p.editSplinePoints[i].tangent2 - p.editSplinePoints[i].position;

                    p.editSplinePoints[i].position = extrudeResult.position;
                    p.editSplinePoints[i].normal = extrudeMatrix.MultiplyVector(p.editSplinePoints[i].normal);

                    GetExtrudeResult(GetPercentage(inverseRootMatrix.MultiplyPoint3x4(points[i].tangent)));
                    Vector3 tan1Pos = extrudeResult.position;
                    GetExtrudeResult(GetPercentage(inverseRootMatrix.MultiplyPoint3x4(points[i].tangent2)));
                    Vector3 tan2Pos = extrudeResult.position;
                    Quaternion tan1Rot = Quaternion.identity;
                    if (tan1Pos != p.editSplinePoints[i].position) tan1Rot = Quaternion.LookRotation(p.editSplinePoints[i].position - tan1Pos, p.editSplinePoints[i].normal);
                    Quaternion tan2Rot = Quaternion.identity;
                    if (tan2Pos != p.editSplinePoints[i].position) tan2Rot = Quaternion.LookRotation(tan2Pos - p.editSplinePoints[i].position, p.editSplinePoints[i].normal);
                    Quaternion averageRot = Quaternion.Slerp(tan1Rot, tan2Rot, 0.5f);
                    p.editSplinePoints[i].tangent = p.editSplinePoints[i].position + averageRot * tangent1Delta;
                    p.editSplinePoints[i].tangent2 = p.editSplinePoints[i].position + averageRot * tangent2Delta;
                }
            }
#endif
        }

        /// <summary>
        /// Convert a local point to percent inside the segment bounds
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vector3 GetPercentage(Vector3 point)
        {
            point.x = Mathf.InverseLerp(bounds.min.x, bounds.max.x, point.x);
            point.y = Mathf.InverseLerp(bounds.min.y, bounds.max.y, point.y);
            point.z = Mathf.InverseLerp(bounds.min.z, bounds.max.z, point.z);
            return point;
        }

        /// <summary>
        /// Convert a local percent inside the bounds to a world result along the sampled spline
        /// </summary>
        /// <param name="boundsPercent"></param>
        private void GetExtrudeResult(Vector3 boundsPercent)
        {
            Quaternion axisRotation = Quaternion.identity;

            switch (axis)
            {
                case Axis.X: Evaluate(boundsPercent.x, ref extrudeResult); break;
                case Axis.Y: Evaluate(boundsPercent.y, ref extrudeResult); break;
                case Axis.Z: Evaluate(boundsPercent.z, ref extrudeResult); break;
            }
            Vector3 right = extrudeResult.right;

            switch (axis)
            {
                case Axis.Z:
                    extrudeResult.position += right * Mathf.Lerp(bounds.min.x, bounds.max.x, boundsPercent.x) * extrudeResult.size;
                    extrudeResult.position += extrudeResult.up * Mathf.Lerp(bounds.min.y, bounds.max.y, boundsPercent.y) * extrudeResult.size;
                    break;
                case Axis.X:
                    axisRotation = Quaternion.Euler(0f, -90f, 0f);
                    extrudeResult.position += right * Mathf.Lerp(bounds.max.z, bounds.min.z, boundsPercent.z) * extrudeResult.size;
                    extrudeResult.position += extrudeResult.up * Mathf.Lerp(bounds.min.y, bounds.max.y, boundsPercent.y) * extrudeResult.size;
               break;
                case Axis.Y:
                    axisRotation = Quaternion.Euler(90f, 0f, 0f);
                    extrudeResult.position += right * Mathf.Lerp(bounds.min.x, bounds.max.x, boundsPercent.x) * extrudeResult.size;
                    extrudeResult.position += extrudeResult.up * Mathf.Lerp(bounds.min.z, bounds.max.z, boundsPercent.z) * extrudeResult.size;
                    break;
            }

            extrudeRotation = extrudeResult.rotation * axisRotation;
            extrudeMatrix.SetTRS(extrudeResult.position, extrudeRotation, Vector3.one * extrudeResult.size);
        }

        public void GetExtrudeResult(Vector3 boundsPercent, SplineSample result, ref Quaternion rotation)
        {
            Quaternion axisRotation = Quaternion.identity;

            switch (axis)
            {
                case Axis.X: Evaluate(boundsPercent.x, ref result); break;
                case Axis.Y: Evaluate(boundsPercent.y, ref result); break;
                case Axis.Z: Evaluate(boundsPercent.z, ref result); break;
            }
            Vector3 right = result.right;

            switch (axis)
            {
                case Axis.Z:
                    result.position += right * Mathf.Lerp(bounds.min.x, bounds.max.x, boundsPercent.x) * result.size;
                    result.position += result.up * Mathf.Lerp(bounds.min.y, bounds.max.y, boundsPercent.y) * result.size;
                    break;
                case Axis.X:
                    axisRotation = Quaternion.Euler(0f, -90f, 0f);
                    result.position += right * Mathf.Lerp(bounds.max.z, bounds.min.z, boundsPercent.z) * result.size;
                    result.position += result.up * Mathf.Lerp(bounds.min.y, bounds.max.y, boundsPercent.y) * result.size;
                    break;
                case Axis.Y:
                    axisRotation = Quaternion.Euler(90f, 0f, 0f);
                    result.position += right * Mathf.Lerp(bounds.min.x, bounds.max.x, boundsPercent.x) * result.size;
                    result.position += result.up * Mathf.Lerp(bounds.min.z, bounds.max.z, boundsPercent.z) * result.size;
                    break;
            }
            rotation = result.rotation * axisRotation;
        }

        Quaternion GetUprightRotation(Quaternion input, Vector3 uprightVector)
        {
            return Quaternion.FromToRotation(Vector3.Cross(extrudeResult.forward, extrudeResult.right), uprightVector) * input;
        }

        /// <summary>
        /// End of the extrusion sequence - apply the calculated results
        /// </summary>
        public virtual IEnumerator OnPostExtrude()
        {
            //Apply bending properties
            if (type == Type.Extruded)
            {
                yield return _generator.ScheduleAsyncJob(new AsyncJobSystem.JobData<ObjectProperty>
                (
                    objectProperties,
                    objectProperties.Length / POST_EXTRUDE_ACTION_FRAMES,
                    (AsyncJobSystem.JobData<ObjectProperty> data) =>
                    {
                        data.current.RuntimeApply();
                    }
                ));


                yield return _generator.ScheduleAsyncJob(new AsyncJobSystem.JobData<LevelSegmentPath>
                (
                    customPaths,
                    customPaths.Length / POST_EXTRUDE_ACTION_FRAMES,
                    (AsyncJobSystem.JobData<LevelSegmentPath> data) =>
                    {
                        data.current.InverseTransform();
                    }
                ));
                gameObject.SetActive(true);
                _extruded = true;
            }
            yield return _generator.ScheduleAsyncJob(new AsyncJobSystem.JobData<Builder>
            (
                builders,
                builders.Length / POST_EXTRUDE_ACTION_FRAMES,
                (AsyncJobSystem.JobData<Builder> data) =>
                {
                    if (data.current.enabled && data.current.gameObject.activeInHierarchy && data.current.queue == Builder.Queue.OnGenerate)
                    {
                        data.current.StartBuild();
                    }
                }
            ));

            var list = new List<Transform>();
            SceneUtility.GetChildrenRecursively(transform, ref list);
            yield return _generator.ScheduleAsyncJob(new AsyncJobSystem.JobData<Transform>
            (
                list,
                list.Count / POST_EXTRUDE_ACTION_FRAMES,
                (AsyncJobSystem.JobData<Transform> data) =>
                {
                    var handler = data.current.GetComponent<ISegmentCompleteHandler>();
                    if (handler != null)
                    {
                        handler.OnSegmentComplete(this);
                    }
                }
            ));

            if (onExtruded != null) {
                onExtruded();
            }
        }

    }
}
