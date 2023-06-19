namespace Dreamteck.Forever
{
    using Dreamteck.Utilities;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [System.Serializable]
    public class SegmentSequence : System.IDisposable
    {
        public bool enabled = true;
        public string name = "";
        public bool isCustom = false;
        public CustomSequence customSequence = null;
        public SegmentDefinition[] segments = new SegmentDefinition[0];
        public LevelPathGenerator customPathGenerator;
        public int spawnCount = 1;
        public enum Type { Ordered, RandomByChance, Custom, Shuffled }
        public Type type = Type.Ordered;
        public ForeverRandomizer randomizer;
        public SegmentShuffle customShuffle = null;
        private int currentSegmentIndex = 0;
        public bool preventRepeat = false;
        private bool stopped = false;
        private SegmentDefinition _lastDefinition = null;
        private int _lastRandom = -1;

        public delegate SegmentDefinition SegmentShuffleHandler(SegmentSequence sequence, int index);

        private SegmentShuffleHandler _shuffle;

        private List<int> _shuffledSegments = new List<int>();

        public void Initialize()
        {
            if (isCustom)
            {
                customSequence.Initialize();
                return;
            }

            if (randomizer != null)
            {
                randomizer.Initialize();
            }
            _lastDefinition = null;
            currentSegmentIndex = 0;
            _shuffledSegments.Clear();
            stopped = false;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].nested && segments[i].nestedSequence != null) segments[i].nestedSequence.Initialize();
            }
            switch (type)
            {
                case Type.Ordered: _shuffle = GetOrderedSegment; break;
                case Type.RandomByChance: _shuffle = GetRandomSegment; break;
                case Type.Shuffled: _shuffle = GetShuffledSegment; break;
                case Type.Custom:
                    if (customShuffle != null)
                    {
                        customShuffle.Reset();
                        _shuffle = customShuffle.Get;
                    }
                    break;
            }
        }

        public void Stop()
        {
            if (isCustom)
            {
                customSequence.Stop();
                return;
            }
            if (type == Type.RandomByChance) currentSegmentIndex = spawnCount - 1;
            else currentSegmentIndex = segments.Length - 1;
            stopped = true;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].nested) segments[i].nestedSequence.Stop();
            }
        }

        public SegmentDefinition Next()
        {
            if(_lastDefinition != null && _lastDefinition.nested && !_lastDefinition.nestedSequence.IsDone())
            {
                return _lastDefinition.nestedSequence.Next();
            }

            if (isCustom)
            {
                return new SegmentDefinition(customSequence.Next());
            }
            if (segments.Length == 0)
            {
                return null;
            }

            _lastDefinition = _shuffle(this, currentSegmentIndex);
            currentSegmentIndex++;
            if (_lastDefinition.nested)
            {
                _lastDefinition.nestedSequence.Initialize();
                return _lastDefinition.nestedSequence.Next();
            } else
            {
                return _lastDefinition;
            }
        }

        public bool IsDone()
        {
            if (isCustom) return customSequence.IsDone();
            if (stopped) return true;
            if (segments.Length == 0) return true;
            if (_lastDefinition == null || !_lastDefinition.nested || _lastDefinition.nestedSequence.IsDone())
            {
                switch (type)
                {
                    case Type.Ordered: return currentSegmentIndex >= segments.Length;
                    case Type.RandomByChance: if (spawnCount == 0) return false; return currentSegmentIndex >= spawnCount;
                    case Type.Shuffled: if (spawnCount == 0)
                        {
                            return currentSegmentIndex >= segments.Length;
                        }
                        return currentSegmentIndex >= spawnCount;
                    case Type.Custom: if (customShuffle != null) return customShuffle.IsDone(); break;
                }
            }
            return false;
        }

        public SegmentSequence Duplicate()
        {
            SegmentSequence sequence = new SegmentSequence();
            sequence.enabled = enabled;
            sequence.name = name;
            sequence.preventRepeat = preventRepeat;
            sequence.isCustom = isCustom;
            sequence.customSequence = customSequence;
            sequence.spawnCount = spawnCount;
            sequence.type = type;
            sequence.customShuffle = customShuffle;
            sequence.segments = new SegmentDefinition[segments.Length];
            for (int i = 0; i < segments.Length; i++) sequence.segments[i] = segments[i].Duplicate();
            return sequence;
        }

        public void Dispose()
        {
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i].Dispose();
            }
        }

        private SegmentDefinition GetOrderedSegment(SegmentSequence sequence, int index)
        {
            int segmentIndex = index;
            if (segmentIndex < 0) segmentIndex = 0;
            if (segmentIndex >= sequence.segments.Length) segmentIndex = sequence.segments.Length - 1;
            return sequence.segments[segmentIndex];
        }

        private SegmentDefinition GetRandomSegment(SegmentSequence sequence, int index)
        {
            int segmentIndex = GetRandomSegmentByChance(sequence, sequence.preventRepeat ? _lastRandom : -1);
            _lastRandom = segmentIndex;
            return sequence.segments[segmentIndex];
        }

        private SegmentDefinition GetShuffledSegment(SegmentSequence sequence, int index)
        {
            if(_shuffledSegments.Count == 0)
            {
                for (int i = 0; i < sequence.segments.Length; i++)
                {
                    _shuffledSegments.Add(i);
                }
                ShuffleList(_shuffledSegments);
                if(_shuffledSegments[0] == _lastRandom)
                {
                    int temp = _shuffledSegments[0];
                    _shuffledSegments[0] = _shuffledSegments[_shuffledSegments.Count - 1];
                    _shuffledSegments[_shuffledSegments.Count - 1] = temp;
                }
                _lastRandom = _shuffledSegments[_shuffledSegments.Count - 1];
            }
            int idx = _shuffledSegments[0];
            _shuffledSegments.RemoveAt(0);
            return sequence.segments[idx];
        }

        private int GetRandomSegmentByChance(SegmentSequence sequence, int exclude = -1)
        {
            float totalChance = 0f;
            for (int i = 0; i < sequence.segments.Length; i++)
            {
                if (i == exclude) continue;
                totalChance += sequence.segments[i].randomPickChance;
            }
            float randomValue = randomizer.Next(0f, totalChance);
            float passed = 0f;
            for (int i = 0; i < sequence.segments.Length; i++)
            {
                if (i == exclude) continue;
                if (sequence.segments[i].randomPickChance <= 0f) continue;
                if (randomValue >= passed && randomValue <= passed + sequence.segments[i].randomPickChance) return i;
                passed += sequence.segments[i].randomPickChance;
            }
            return 0;
        }

        private void ShuffleList<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = randomizer.Next(0, n + 1);
                list.Swap(k, n);
            }
        }

#if UNITY_EDITOR
        public void EditorDeployPool(Transform poolParent)
        {
            if(poolParent == null)
            {
                Debug.Log("Cannot deploy pool for sequence " + name + " becaue the pool parent is null");
                return;
            }
            int count = 1;
            if(type == Type.RandomByChance)
            {
                if (preventRepeat)
                {
                    count = Mathf.CeilToInt((float)spawnCount / 2);
                }
                else
                {
                    count = spawnCount;
                }
                if(count == 0)
                {
                    count = 10; //Arbitrary number for endless sequences. We will change how this is handled in a future version
                }
            }
            if(type == Type.Shuffled)
            {
                count = Mathf.CeilToInt((float)spawnCount / segments.Length);
            }

            for (int i = 0; i < segments.Length; i++)
            {
                UnityEditor.EditorUtility.DisplayProgressBar("Deploying Pool", "Deploying segment " + (i + 1) + " / " + segments.Length, (float)i / (segments.Length - 1));
                segments[i].EditorDeployPool(poolParent, count);
            }
            UnityEditor.EditorUtility.ClearProgressBar();
        }

        public void EditorDismantlePool()
        {
            for (int i = 0; i < segments.Length; i++)
            {
                segments[i].EditorDismantlePool();
            }
        }
#endif
    }


    [System.Serializable]
    public class SegmentSequenceCollection : ISerializationCallbackReceiver, System.IDisposable
    {
        public SegmentSequence[] sequences = new SegmentSequence[0];
        [SerializeField]
        private List<SequenceSerialization> sequencePositions = new List<SequenceSerialization>();
        [SerializeField]
        private List<SegmentSequence> allSequences = new List<SegmentSequence>();

        [System.Serializable]
        internal class SequenceSerialization
        {
            [SerializeField]
            internal int parent = -1;
            [SerializeField]
            internal int segmentIndex = -1;

            internal SequenceSerialization(int p, int s)
            {
                parent = p;
                segmentIndex = s;
            }
        }

        public void OnBeforeSerialize()
        {
            if (Application.isPlaying) return;
            allSequences.Clear();
            sequencePositions.Clear();
            for (int i = 0; i < sequences.Length; i++)
            {
                UnpackSequence(sequences[i], -1, -1, ref allSequences, ref sequencePositions);
            }
        }

        public void OnAfterDeserialize()
        {
            if (sequencePositions.Count == 0) return;
            List<SegmentSequence> sequenceList = new List<SegmentSequence>();
            for (int i = 0; i < allSequences.Count; i++)
            {
                if (sequencePositions[i].parent < 0) sequenceList.Add(allSequences[i]);
                else
                {
                    allSequences[sequencePositions[i].parent].segments[sequencePositions[i].segmentIndex].nestedSequence = allSequences[i];
                }
            }
            sequences = sequenceList.ToArray();
        }

        void UnpackSequence(SegmentSequence sequence, int parent, int segmentIndex, ref List<SegmentSequence> flat, ref List<SequenceSerialization> parentIndices)
        {
            flat.Add(sequence);
            parentIndices.Add(new SequenceSerialization(parent, segmentIndex));
            int parentIndex = flat.Count - 1;
            for (int i = 0; i < sequence.segments.Length; i++)
            {
                if (sequence.segments[i].nestedSequence != null)
                {
                    UnpackSequence(sequence.segments[i].nestedSequence, parentIndex, i, ref flat, ref parentIndices);
                }
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < sequences.Length; i++)
            {
                sequences[i].Dispose();
            }

            for (int i = 0; i < allSequences.Count; i++)
            {
                allSequences[i].Dispose();
            }
        }
    }
}
