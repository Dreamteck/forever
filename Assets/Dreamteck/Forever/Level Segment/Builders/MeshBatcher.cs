namespace Dreamteck.Forever
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [AddComponentMenu("Dreamteck/Forever/Builders/Mesh Batcher")]
    public class MeshBatcher : Builder
    {
        public bool includeParent = false;
        public enum Mode { Cached, Dynamic }
        public Mode mode = Mode.Cached;
        [SerializeField] MeshBatchSettings _batchSettings;
        [SerializeField]
        [HideInInspector]
        Batchable[] batchables = new Batchable[0];
        private List<CombineChildQueue> combineQueues = new List<CombineChildQueue>();
        private Mesh[] combinedMeshes = new Mesh[0];

        internal class CombineChildQueue
        {
            private Material[] materials = new Material[0];
            private Transform parent;
            private List<Batchable> batchers = new List<Batchable>();
            private int vertexCount = 0;

            internal CombineChildQueue(Transform p, Material[] m)
            {
                parent = p;
                materials = m;
            }

            internal Mesh Combine(string name)
            {
                if (batchers.Count == 0) return null;
                GameObject combined = new GameObject(name);
                combined.transform.parent = parent;
                combined.transform.localPosition = Vector3.zero;
                combined.transform.localRotation = Quaternion.identity;
                combined.transform.localScale = Vector3.one;
                MeshFilter combinedFilter = combined.AddComponent<MeshFilter>();
                MeshRenderer combinedRenderer = combined.AddComponent<MeshRenderer>();
                int instanceCount = 0;
                for (int i = 0; i < batchers.Count; i++)
                {
                    instanceCount += batchers[i].GetMesh().subMeshCount;
                }
                CombineInstance[] combineInstances = new CombineInstance[instanceCount];
                int instanceIndex = 0;
                for (int i = 0; i < batchers.Count; i++)
                {
                    for (int j = 0; j < batchers[i].GetMesh().subMeshCount; j++)
                    {
                        combineInstances[instanceIndex].mesh = batchers[i].GetMesh();
                        combineInstances[instanceIndex].subMeshIndex = j;
                        combineInstances[instanceIndex].transform = combined.transform.worldToLocalMatrix * batchers[i].transform.localToWorldMatrix;
                        instanceIndex++;
                    }
                }
                Mesh mesh = new Mesh();
                mesh.name = name;
                mesh.CombineMeshes(combineInstances, true, true);
                combinedFilter.sharedMesh = mesh;
                combinedRenderer.sharedMaterials = materials;
                return mesh;
            }

            internal bool Add(Batchable batcher)
            {
                if (!CanAddBatcher(batcher))
                {
                    return false;
                }
                vertexCount += batcher.GetMesh().vertexCount;
                batchers.Add(batcher);
                return true;
            }

            private bool CanAddBatcher(Batchable batcher)
            {
                if (batcher.GetMesh().vertexCount + vertexCount > 65535)
                {
                    return false;
                }
                if (batcher.GetMaterials().Length != materials.Length)
                {
                    return false;
                }
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != batcher.GetMaterials()[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private bool IsBatchingEnabled()
        {
            return _batchSettings != null && !_batchSettings.excludeBatching;
        }

        protected override void Awake()
        {
            base.Awake();
            if (!IsBatchingEnabled()) return;
            for (int i = 0; i < batchables.Length; i++)
            {
                if (batchables[i] != null)
                {
                    batchables[i].Prepare();
                }
            }
        }
        private void GetBatchers()
        {
            List<Batchable> batchableList = new List<Batchable>();
            if (includeParent)
            {
                Batchable batchable = GetComponent<Batchable>();
                if (batchable == null) batchable = gameObject.AddComponent<Batchable>();
                batchableList.Add(batchable);
            }
            List<Transform> children = new List<Transform>();
            Dreamteck.SceneUtility.GetChildrenRecursively(transform, ref children);
            for (int i = 1; i < children.Count; i++)
            {
                Batchable batchable = children[i].GetComponent<Batchable>();
                if (batchable != null) batchableList.Add(batchable);
            }
            batchables = batchableList.ToArray();
        }

        protected override IEnumerator BuildAsync()
        {
            if (!IsBatchingEnabled()) yield break;
            if (mode == Mode.Dynamic) GetBatchers();
            if (includeParent)
            {
                Batchable parentBatchable = GetComponent<Batchable>();
                if (parentBatchable == null) parentBatchable = gameObject.AddComponent<Batchable>();
                parentBatchable.UpdateImmediate();
                AddBatcherToQueue(parentBatchable);
            }
            Debug.Log(batchables.Length);
            for (int i = 0; i < batchables.Length; i++)
            {
                if (batchables[i] == null) continue;
                Debug.Log(batchables[i].name);
                if (!batchables[i].gameObject.activeInHierarchy) continue;
                AddBatcherToQueue(batchables[i]);
            }
            combinedMeshes = new Mesh[combineQueues.Count];
            for (int i = 0; i < combineQueues.Count; i++)
            {
                combinedMeshes[i] = combineQueues[i].Combine("Combined " + i);
                yield return null;
            }
            combineQueues.Clear();
        }

        private void AddBatcherToQueue(Batchable batchable)
        {
            Debug.Log("adding batcher to queue");
            if (batchable.GetMesh() == null)
            {
                return;
            }

            for (int i = 0; i < combineQueues.Count; i++)
            {
                if (combineQueues[i].Add(batchable))
                {
                    return;
                }
            }

            combineQueues.Add(new CombineChildQueue(trs, batchable.GetMaterials()));
            combineQueues[combineQueues.Count - 1].Add(batchable);
        }

        private void Reset()
        {
            priority = 99;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < combinedMeshes.Length; i++) Destroy(combinedMeshes[i]);
        }


#if UNITY_EDITOR
        public override void OnPack()
        {
            base.OnPack();
            if (mode == Mode.Cached)
            {
                GetBatchers();
                for (int i = 0; i < batchables.Length; i++) batchables[i].Pack();
            }
            else
            {
                List<Transform> children = new List<Transform>();
                SceneUtility.GetChildrenRecursively(transform, ref children);
                for (int i = 0; i < children.Count; i++)
                {
                    if (i == 0 && !includeParent) continue;
                    Batchable batchable = children[i].GetComponent<Batchable>();
                    if (batchable != null) batchable.Pack();
                }
                batchables = new Batchable[0];
            }
        }

        public override void OnUnpack()
        {
            base.OnUnpack();
            if (batchables.Length > 0)
            {
                for (int i = 0; i < batchables.Length; i++)
                {
                    if (batchables[i] != null)
                    {
                        batchables[i].Unpack();
                    }
                }
            }
            else
            {
                List<Transform> children = new List<Transform>();
                SceneUtility.GetChildrenRecursively(transform, ref children);
                for (int i = 0; i < children.Count; i++)
                {
                    if (i == 0 && !includeParent) continue;
                    Batchable batchable = children[i].GetComponent<Batchable>();
                    if (batchable != null) batchable.Unpack();
                }
            }
        }

#endif

    }
}
