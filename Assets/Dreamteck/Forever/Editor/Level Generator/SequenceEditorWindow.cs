namespace Dreamteck.Forever.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class SequenceEditWindow : EditorWindow
    {
        private Object _undoObject = null;
        private System.Action<SegmentSequence[]> _onApplySequences;
        public System.Action onClose;
        SegmentSequenceEditor sequenceEditor = null;
        public void Init(SegmentSequenceCollection collection, Object undoObject, System.Action<SegmentSequence[]> onApplySequences, string title = "Sequence Editor")
        {
            _undoObject = undoObject;
            _onApplySequences = onApplySequences;
            titleContent = new GUIContent(title);
            sequenceEditor = new SegmentSequenceEditor(_undoObject, collection, new Rect(100, 100, 600, 600));
            sequenceEditor.onWillChange += RecordUndo;
            sequenceEditor.onChanged += OnChanged;
            sequenceEditor.onApplySequences += OnApplySequences;
        }

        private void OnDestroy()
        {
            if (onClose != null)
            {
                onClose();
            }
        }

        void OnChanged()
        {
            Repaint();
        }

        void RecordUndo()
        {
            Undo.RecordObject(_undoObject, "Edit Segment Collection");
        }

        void OnApplySequences(SegmentSequence[] sequences)
        {
            if (_onApplySequences != null)
            {
                _onApplySequences(sequences);
            }
        }

        private void OnGUI()
        {
            sequenceEditor.viewRect = new Rect(5, 5, position.width, position.height);
            sequenceEditor.windowPosition = new Vector2(position.x, position.y);
            sequenceEditor.DrawEditor();
            //_undoObject.sequenceCollection.sequences = sequenceEditor.sequences;
        }
    }
}
