namespace Dreamteck.Forever
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;

    public class SegmentSequenceSettingsWindow : EditorWindow
    {
        SegmentSequence sequence;
        SegmentSequenceEditor editor;
        public SegmentSequenceEditor.EmptyHandler onWillChange;

        public void Init(SegmentSequence sequence, SegmentSequenceEditor editor)
        {
            this.sequence = sequence;
            this.editor = editor;
            minSize = maxSize = new Vector2(250f, 250f);
            titleContent = new GUIContent("Sequence Settings");
            Repaint();
        }

        private void OnGUI()
        {
            if(editor == null)
            {
                Close();
                return;
            }
            string name = sequence.name;
            name = EditorGUILayout.TextField("Name", name);
            if (name != sequence.name)
            {
                if (onWillChange != null) onWillChange();
                sequence.name = name;
            }


            if (!sequence.isCustom)
            {
                SegmentSequence.Type type = sequence.type;
                type = (SegmentSequence.Type)EditorGUILayout.EnumPopup("Shuffle Type", type);
                if (type != sequence.type)
                {
                    if (onWillChange != null) onWillChange();
                    sequence.type = type;
                }
                if (type == SegmentSequence.Type.RandomByChance)
                {
                    int spawnCount = sequence.spawnCount;
                    spawnCount = EditorGUILayout.IntField(new GUIContent("Spawn Count", "How many segments should this random sequence spawn? Zero will go on forever."), spawnCount);
                    if (spawnCount != sequence.spawnCount)
                    {
                        if (onWillChange != null) onWillChange();
                        sequence.spawnCount = spawnCount;
                    }
                    ForeverRandomizer randomizer = sequence.randomizer;
                    randomizer = (ForeverRandomizer)EditorGUILayout.ObjectField("Randomizer", randomizer, typeof(ForeverRandomizer), false);
                    if(randomizer != sequence.randomizer)
                    {
                        if (onWillChange != null) onWillChange();
                        sequence.randomizer = randomizer;
                    }
                    if(sequence.randomizer == null)
                    {
                        EditorGUILayout.HelpBox("A Randomizer must be assigned in order for the sequence to work", MessageType.Error);
                    }
                    bool preventRepeat = sequence.preventRepeat;
                    preventRepeat = EditorGUILayout.Toggle(new GUIContent("Prevent Repeating", "If true, the random algorithm will make sure not to spawn the same segment twice in a row."), preventRepeat);
                    if (preventRepeat != sequence.preventRepeat)
                    {
                        if (onWillChange != null) onWillChange();
                        sequence.preventRepeat = preventRepeat;
                    }
                }
                else if (sequence.type == SegmentSequence.Type.Custom)
                {
                    SegmentShuffle customShuffle = sequence.customShuffle;
                    customShuffle = (SegmentShuffle)EditorGUILayout.ObjectField("Shuffle", customShuffle, typeof(SegmentShuffle), false);
                    if (customShuffle != sequence.customShuffle)
                    {
                        if (onWillChange != null) onWillChange();
                        sequence.customShuffle = customShuffle;
                    }
                } else if (sequence.type == SegmentSequence.Type.Shuffled)
                {
                    int spawnCount = sequence.spawnCount;
                    spawnCount = EditorGUILayout.IntField(new GUIContent("Spawn Count", "How many segments should this random sequence spawn?"), spawnCount);
                    if (spawnCount != sequence.spawnCount)
                    {
                        if (onWillChange != null) onWillChange();
                        sequence.spawnCount = spawnCount;
                    }
                    ForeverRandomizer randomizer = sequence.randomizer;
                    randomizer = (ForeverRandomizer)EditorGUILayout.ObjectField("Randomizer", randomizer, typeof(ForeverRandomizer), false);
                    if (randomizer != sequence.randomizer)
                    {
                        if (onWillChange != null) onWillChange();
                        sequence.randomizer = randomizer;
                    }
                    if (sequence.randomizer == null)
                    {
                        EditorGUILayout.HelpBox("A Randomizer must be assigned in order for the sequence to work", MessageType.Error);
                    }
                }
            }
            EditorGUILayout.Space();
            LevelPathGenerator pathGenerator = sequence.customPathGenerator;
            pathGenerator = (LevelPathGenerator)EditorGUILayout.ObjectField("Custom Path Generator", pathGenerator, typeof(LevelPathGenerator), false);
            if (pathGenerator != sequence.customPathGenerator)
            {
                if (onWillChange != null) onWillChange();
                sequence.customPathGenerator = pathGenerator;
            }

            EditorGUILayout.Space();

            bool isCustom = sequence.isCustom;
            isCustom = EditorGUILayout.Toggle("Custom Sequence", isCustom);
            if (isCustom != sequence.isCustom)
            {
                if (onWillChange != null) onWillChange();
                sequence.isCustom = isCustom;
            }

            if (isCustom)
            {
                CustomSequence customSequence = sequence.customSequence;
                customSequence = (CustomSequence)EditorGUILayout.ObjectField("Sequence Asset", customSequence, typeof(CustomSequence), false);
                if (customSequence != sequence.customSequence)
                {
                    if (onWillChange != null) onWillChange();
                    sequence.customSequence = customSequence;
                }
                return;
            }

        }
    }
}
