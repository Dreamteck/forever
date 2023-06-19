namespace Dreamteck.Forever
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class SegmentDefinition : System.IDisposable
    {
        public GameObject prefab
        {
            get
            {
                return _prefab;
            }
            set
            {
                if (value == null) return;
                if (value.GetComponent<LevelSegment>() == null) return;
                _prefab = value;
            }
        }
        public bool pooling
        {
            get {
                return _pool;
            }
        }

        [System.NonSerialized]
        public SegmentSequence nestedSequence = null;
        public bool nested = false;
        public float randomPickChance = 1f;

        [SerializeField] private GameObject _prefab;
        [SerializeField] private GameObject[] _segmentPool = new GameObject[0];
        [SerializeField] private bool _pool = false;
        [SerializeField] private Transform _poolParent;

        private int _poolIndex = 0;

        public SegmentDefinition()
        {
            nestedSequence = null;
            nested = false;
        }

        public SegmentDefinition(string nestedName)
        {
            nestedSequence = new SegmentSequence();
            nestedSequence.name = nestedName;
            nested = true;
        }

        public SegmentDefinition(GameObject input)
        {
            prefab = input;
            nested = false;
        }

        public bool IsChildOfPool(GameObject segment)
        {
            return ArrayUtility.Contains(_segmentPool, segment);
        }

        public LevelSegment Instantiate()
        {
            GameObject go = null;
            if (_pool && _poolIndex < _segmentPool.Length)
            {
                go = _segmentPool[_poolIndex++];
                go.SetActive(true);
            }

            if(go == null)
            {
                go = Object.Instantiate(_prefab);
            }
            
            LevelSegment seg = go.GetComponent<LevelSegment>();
            seg.Initialize(this);
            return seg;
        }

        public bool ReturnToPool(GameObject segment)
        {
            if (IsChildOfPool(segment))
            {
                segment.SetActive(false);
                segment.transform.parent = _poolParent;
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            if (_segmentPool != null)
            {
                for (int i = 0; i < _segmentPool.Length; i++)
                {
                    _segmentPool[i] = null;
                }
            }
            _segmentPool = null;
            _prefab = null;
            _poolParent = null;
            if (nestedSequence != null)
            {
                nestedSequence.Dispose();
                nestedSequence = null;
            }
        }

        public SegmentDefinition Duplicate()
        {
            SegmentDefinition def = new SegmentDefinition();
            def._prefab = _prefab;
            def.randomPickChance = randomPickChance;
            if (def.nestedSequence != null) def.nestedSequence = nestedSequence.Duplicate();
            def.nested = nested;
            return def;
        }

#if UNITY_EDITOR
        public void EditorDeployPool(Transform poolParent, int instanceCount)
        {
            if (nested)
            {
                nestedSequence.EditorDeployPool(poolParent);
                return;
            }
            if(instanceCount <= 0)
            {
                return;
            }
            if(_prefab == null)
            {
                Debug.LogError("Cannot depoly pool - missing prefab");
                return;
            }
            EditorDismantlePool();
            _poolParent = poolParent;
            _segmentPool = new GameObject[instanceCount];
            for (int i = 0; i < instanceCount; i++)
            {
                _segmentPool[i] = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(_prefab);
                _segmentPool[i].transform.parent = poolParent;
                _segmentPool[i].SetActive(false);
            }
            _pool = true;
        }

        public void EditorDismantlePool()
        {
            for (int i = 0; i < _segmentPool.Length; i++)
            {
                if (_segmentPool[i] != null)
                {
                    Object.DestroyImmediate(_segmentPool[i]);
                }
            }
            _segmentPool = new GameObject[0];
            _pool = false;
        }
#endif
    }
}
