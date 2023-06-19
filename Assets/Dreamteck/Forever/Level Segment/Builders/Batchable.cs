namespace Dreamteck.Forever
{
    using UnityEngine;
    [AddComponentMenu("Dreamteck/Forever/Builders/Batchable (For Mesh Batching)")]
    public class Batchable : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        MeshRenderer meshRenderer;
        [SerializeField]
        [HideInInspector]
        MeshFilter meshFilter;

#if UNITY_EDITOR
        [SerializeField]
        [HideInInspector]
        bool renderEnabled = false;

        public void Pack()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                DestroyImmediate(this);
                return;
            }
            renderEnabled = meshRenderer.enabled;

            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                DestroyImmediate(this);
                return;
            }
        }

        public void Unpack()
        {
            MeshRenderer rend = GetComponent<MeshRenderer>();
            if (rend == null)
            {
                DestroyImmediate(this);
                return;
            }

            if (GetComponent<MeshFilter>() == null)
            {
                DestroyImmediate(this);
            }
        }
#endif

        public void UpdateImmediate()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
        }

        public void Prepare()
        {
            meshRenderer.enabled = false;
        }

        public Mesh GetMesh()
        {
            return meshFilter.sharedMesh;
        }

        public Material[] GetMaterials()
        {
            return meshRenderer.sharedMaterials;
        }
    }
}
