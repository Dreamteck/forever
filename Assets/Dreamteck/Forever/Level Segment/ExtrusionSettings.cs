using UnityEngine;
namespace Dreamteck.Forever
{
    [System.Serializable]
    public class ExtrusionSettings
    {
        public enum Indexing { Normal, Ignore, IgnoreChildren, IgnoreAll }
        public enum MeshColliderHandling { Bypass, Extrude, Copy }
        public MeshColliderHandling meshColliderHandling = MeshColliderHandling.Bypass;
#if DREAMTECK_SPLINES
        public bool bendSpline = true;
#endif
        public BoundsInclusion boundsInclusion = (BoundsInclusion)~0;
        public enum BoundsInclusion
        {
            Transform = 1,
            Mesh = 2,
            Sprite = 4,
            Collider = 8
#if DREAMTECK_SPLINES
            , Spline = 16
#endif
        }

        public Indexing indexing = Indexing.Normal;
        public bool applyRotation = true;
        public bool keepUpright = false;
        public Vector3 upVector = Vector3.up;
        public bool applyScale = false;
        public bool bendMesh = false;
        public bool bendSprite = false;
        public bool bendPolygonCollider = false;
        public bool applyMeshColors = false;

        public bool ignore
        {
            get
            {
                return indexing == Indexing.Ignore || indexing == Indexing.IgnoreAll;
            }
        }

        public bool ignoreChildren
        {
            get
            {
                return indexing == Indexing.IgnoreChildren || indexing == Indexing.IgnoreAll;
            }
        }

        public void CopyFrom(ExtrusionSettings input)
        {
            indexing = input.indexing;
            applyRotation = input.applyRotation;
            applyScale = input.applyScale;
            keepUpright = input.keepUpright;
            upVector = input.upVector;
            bendMesh = input.bendMesh;
            bendSprite = input.bendSprite;
            bendPolygonCollider = input.bendPolygonCollider;
            applyMeshColors = input.applyMeshColors;
#if DREAMTECK_SPLINES
            bendSpline = input.bendSpline;
#endif
            boundsInclusion = input.boundsInclusion;
            meshColliderHandling = input.meshColliderHandling;
        }
    }
}
