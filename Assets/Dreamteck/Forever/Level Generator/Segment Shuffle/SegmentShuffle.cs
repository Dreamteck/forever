
namespace Dreamteck.Forever
{
    using UnityEngine;

    public class SegmentShuffle : ScriptableObject
    {
        protected bool isDone = false;

        public bool IsDone()
        {
            return isDone;
        }

        public virtual void Reset()
        {
            isDone = false;
        }

        public virtual SegmentDefinition Get(SegmentSequence sequence, int index)
        {
            if (sequence.segments.Length == 0) return null;
            return sequence.segments[0];
        }

    }
}
