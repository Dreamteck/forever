namespace Dreamteck.Forever
{
    using UnityEngine;
#if DREAMTECK_SPLINES
    using Dreamteck.Splines;
#endif

    public partial class LevelSegment : MonoBehaviour
    {
        [System.Serializable]
        public class ObjectProperty
        {
            public Transform transform;
            public Quaternion localRotation;
            [System.NonSerialized]
            public Matrix4x4 localToWorldMatrix;
            [System.NonSerialized]
            public Vector3 localScale = Vector3.one;
            [System.NonSerialized]
            public Vector3 targetPosition = Vector3.zero;
            [System.NonSerialized]
            public Quaternion targetRotation = Quaternion.identity;
            [System.NonSerialized]
            public float scaleMultiplier = 1f;

            public Mesh originalMesh
            {
                get { return _originalMesh; }
            }
            private Mesh _originalMesh = null;
            public Mesh originalCollisionMesh
            {
                get { return _originalCollisionMesh; }
            }
            private Mesh _originalCollisionMesh = null;
            public Mesh extrudedMesh = null;
            public TS_Mesh extrusionMesh = null;
            public Vector2[] extrusionSpriteVertices = new Vector2[0];
            public Vector2[] extrusionSpriteUVs = new Vector2[0];
            public Rect extrusionSpriteRect = new Rect();
            public Mesh extrudedCollisionMesh = null;
            public TS_Mesh extrusionCollisionMesh = null;
            [SerializeField]
            [HideInInspector]
            private ExtrusionSettings _extrusionSettings = new ExtrusionSettings();
            [SerializeField]
            [HideInInspector]
            private SegmentObjectSettings _settingsComponent = null;
            public bool overrideSettingsComponent = false;

            public ExtrusionSettings extrusionSettings
            {
                get
                {
                    return hasSettingsComponent && !overrideSettingsComponent ? _settingsComponent.settings : _extrusionSettings;
                }
            }

            public bool hasSettingsComponent
            {
                get
                {
                    return _settingsComponent != null;
                }
            }

            [HideInInspector]
            public MeshFilter meshFilter = null;
            [HideInInspector]
            public MeshCollider meshCollider = null;
            [HideInInspector]
            public SpriteRenderer spriteRenderer = null;


#if DREAMTECK_SPLINES
            [HideInInspector]
            public SplineComputer splineComputer = null;
            [HideInInspector]
            public SplinePoint[] editSplinePoints = new SplinePoint[0];
#endif

            [HideInInspector]
            public Collider[] colliders = new Collider[0];
            [System.NonSerialized]
            internal bool[] collidersEnabled = new bool[0];

            [SerializeField]
            [HideInInspector]
            private bool _isRoot;
            public bool isRoot
            {
                get { return _isRoot; }
            }

            public bool error
            {
                get { return _error; }
            }
            private bool _error = false;

            public ObjectProperty(Transform t, bool isRoot = false)
            {
                _isRoot = isRoot;
                transform = t;
                localRotation = Quaternion.Inverse(t.root.rotation) * t.rotation;
#if UNITY_EDITOR
                _extrusionSettings.boundsInclusion = ForeverPrefs.extrudeBoundsInclusion;
                _extrusionSettings.applyRotation = ForeverPrefs.extrudeApplyRotation;
                _extrusionSettings.applyScale = ForeverPrefs.extrudeApplyScale;
                _extrusionSettings.bendMesh = ForeverPrefs.extrudeBendSprite;
                _extrusionSettings.bendSprite = ForeverPrefs.extrudeBendSprite;
                _extrusionSettings.applyMeshColors = ForeverPrefs.extrudeApplyMeshColors;
                _extrusionSettings.meshColliderHandling = ForeverPrefs.extrudeMeshColliderHandle;
#if DREAMTECK_SPLINES
                _extrusionSettings.bendSpline = ForeverPrefs.extrudeExtrudeSpline;
#endif
#endif
                GetReferences();
            }

            public void GetReferences()
            {
                if (transform == null)
                {
                    Debug.LogError("Null transform found for property");
                    return;
                }
                _settingsComponent = transform.GetComponent<SegmentObjectSettings>();
                meshFilter = transform.GetComponent<MeshFilter>();
                spriteRenderer = transform.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null || spriteRenderer.sprite == null) extrusionSettings.bendSprite = false;
                if (extrusionSettings.bendMesh && meshFilter.sharedMesh == null) extrusionSettings.bendMesh = false;
                colliders = transform.GetComponents<Collider>();
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] is MeshCollider)
                    {
                        meshCollider = (MeshCollider)colliders[i];
                        break;
                    }
                }
                localRotation = Quaternion.Inverse(transform.root.rotation) * transform.rotation;

#if DREAMTECK_SPLINES
                if (extrusionSettings.bendSpline)
                {
                    if (!isRoot)
                    {
                        splineComputer = transform.GetComponent<SplineComputer>();
                        if (splineComputer != null) editSplinePoints = splineComputer.GetPoints();
                        else extrusionSettings.bendSpline = false;
                    } else extrusionSettings.bendSpline = false;
                }
#endif
            }


            /// <summary>
            /// Called by the extrusion sequence prior to extruding
            /// </summary>
            public void RuntimeInitialize()
            {
                if (transform == null)
                {
                    _error = true;
                    return;
                }
                localToWorldMatrix = transform.localToWorldMatrix;
                localScale = transform.localScale;
               
#if DREAMTECK_SPLINES
                if (splineComputer == null) extrusionSettings.bendSpline = false;
#endif
                if (meshFilter != null)
                {
                    _originalMesh = meshFilter.sharedMesh;
                    if (extrusionSettings.bendMesh) extrusionMesh = new TS_Mesh(_originalMesh);
                } else extrusionSettings.bendMesh = false;

                if (spriteRenderer != null)
                {
                    if (spriteRenderer.sprite != null)
                    {
                        extrusionSpriteVertices = spriteRenderer.sprite.vertices;
                        extrusionSpriteUVs = spriteRenderer.sprite.uv;
                    }
                } else extrusionSettings.bendSprite = false;

                if (meshCollider != null)
                {
                    _originalCollisionMesh = meshCollider.sharedMesh;
                    if (extrusionSettings.meshColliderHandling == ExtrusionSettings.MeshColliderHandling.Extrude) extrusionCollisionMesh = new TS_Mesh(_originalCollisionMesh);
                }
                else if (extrusionSettings.meshColliderHandling == ExtrusionSettings.MeshColliderHandling.Extrude) extrusionSettings.meshColliderHandling = ExtrusionSettings.MeshColliderHandling.Bypass;
            }

            /// <summary>
            /// Called by the extrusion sequence after extruding to apply results
            /// </summary>
            public void RuntimeApply()
            {
                if (_error) return;
                if (extrusionSettings.ignore) return;
                if (extrusionSettings.applyRotation) transform.SetPositionAndRotation(targetPosition, targetRotation);
                else transform.position = targetPosition;
                if (extrusionSettings.applyScale)
                {
                    Transform parent = transform.parent;
                    transform.parent = null;
                    transform.localScale *= scaleMultiplier;
                    transform.parent = parent;
                }

                if (extrusionSettings.bendMesh)
                {
                    extrudedMesh = new Mesh();
                    extrudedMesh.name = _originalMesh.name + "_extruded";
                    extrusionMesh.WriteMesh(ref extrudedMesh);
                    MeshFilter filter = transform.GetComponent<MeshFilter>();
                    filter.sharedMesh = extrudedMesh;
                    extrusionMesh.Clear();
                }

                if (extrusionSettings.bendSprite)
                {
                    Sprite originalSprite = spriteRenderer.sprite;
                    Material instanceMaterial = spriteRenderer.sharedMaterial;
                    extrudedMesh = new Mesh();
                    extrudedMesh.name = spriteRenderer.sprite.name + "_mesh";
                    Vector3[] verts = new Vector3[extrusionSpriteVertices.Length];
                    ushort[] shortTris = spriteRenderer.sprite.triangles;
                    int[] tris = new int[shortTris.Length];
                    for (int i = 0; i < tris.Length; i++) tris[i] = shortTris[i];
                    for (int i = 0; i < verts.Length; i++) verts[i] = extrusionSpriteVertices[i];
                    extrudedMesh.vertices = verts;
                    extrudedMesh.uv = extrusionSpriteUVs;
                    extrudedMesh.triangles = tris;
                    DestroyImmediate(spriteRenderer);
                    MeshRenderer spriteMeshRenderer = transform.gameObject.AddComponent<MeshRenderer>();
                    spriteMeshRenderer.sharedMaterial = instanceMaterial;
                    MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                    spriteMeshRenderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetTexture("_MainTex", originalSprite.texture);
                    spriteMeshRenderer.SetPropertyBlock(propertyBlock);
                    MeshFilter spriteMeshFilter = transform.gameObject.AddComponent<MeshFilter>();
                    spriteMeshFilter.sharedMesh = extrudedMesh;
                }

                switch (extrusionSettings.meshColliderHandling)
                {
                    case ExtrusionSettings.MeshColliderHandling.Extrude:
                        extrudedCollisionMesh = new Mesh();
                        extrudedCollisionMesh.name = _originalCollisionMesh.name + "_extruded";
                        extrusionCollisionMesh.WriteMesh(ref extrudedCollisionMesh);
                        meshCollider.sharedMesh = extrudedCollisionMesh;
                        break;
                    case ExtrusionSettings.MeshColliderHandling.Copy:
                        if(meshCollider == null) meshCollider = transform.gameObject.AddComponent<MeshCollider>();
                        if (extrudedMesh != null) meshCollider.sharedMesh = extrudedMesh;
                        else meshCollider.sharedMesh = _originalMesh;
                        break;
                }

#if DREAMTECK_SPLINES
                if (splineComputer != null)
                {
                    if (extrusionSettings.bendSpline)
                    {
                        splineComputer.SetPoints(editSplinePoints);
                    }
                }
#endif
            }

            /// <summary>
            /// Destroy the generated content to free up memory
            /// </summary>
            public void Dispose()
            {
                if (_error) return;
                if (extrusionSettings.bendMesh)
                {
                    Destroy(extrudedMesh);
                    extrusionMesh.Clear();
                }
                
                if (extrusionSettings.bendSprite)
                {
                    Destroy(extrudedMesh);
                }

                if (extrusionSettings.meshColliderHandling == ExtrusionSettings.MeshColliderHandling.Extrude)
                {
                    Destroy(extrudedCollisionMesh);
                    extrusionCollisionMesh.Clear();
                }
            }
        }
    }
}
