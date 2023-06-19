namespace Dreamteck.Forever
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public abstract class ForeverRandomizer : ScriptableObject
    {
        public bool isRecording { get { return _record; } }

        protected double[] _recordedValues = new double[0];
        private int _lastLevelIndex = -1;
        private bool _record = false;
        private int _playbackIndex = 0;
        private int _bufferIncrement = -1;
        private int _bufferIndex = -1;
        private Dictionary<ForeverLevel, int> _cues = new Dictionary<ForeverLevel, int>();

        public void Initialize(int prewarm = -1)
        {
            OnInitialize();
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnRecordingStarted() { }
        protected virtual void OnRecordingStopped() { }
        protected virtual void OnRecordingResumed() { }
        protected virtual void OnRecordingCleared() { }

        protected abstract double Next01();


        /// <summary>
        /// Generates a float between 0 and 1
        /// </summary>
        /// <returns></returns>
        public double Next()
        {
            if (_playbackIndex <= _bufferIndex)
            {
                return (float)_recordedValues[_playbackIndex++];
            }
            double value = Next01();
            if (_record)
            {
                RecordValue(value);
            }
            _playbackIndex++;
            return (float)value;
        }

        /// <summary>
        /// Same as <see cref="Next"/> but returns a float
        /// </summary>
        /// <returns></returns>
        public float NextF()
        {
            return (float)Next();
        }

        /// <summary>
        /// Generates an int between <paramref name="min"/> and <paramref name="max"/>
        /// </summary>
        /// <returns></returns>
        public int Next(int min, int max)
        {
            return Mathf.RoundToInt(Next((float)min, (float)(max - 1)));
        }

        /// <summary>
        /// Generates an float between <paramref name="min"/> and <paramref name="max"/>
        /// </summary>
        /// <returns></returns>
        public float Next(float min, float max)
        {
            return Mathf.Lerp(min, max, (float)Next());
        }

        /// <summary>
        /// Tells the randomizer to record all generated values for playback purposes
        /// </summary>
        /// <param name="bufferIncrement">Defines the increment of the recorded value buffer</param>
        public void StartRecording(int bufferIncrement = 10000)
        {
            ClearRecordedValues();
            _record = true;
            _bufferIncrement = bufferIncrement;
            ExtendBuffer();
            OnRecordingStarted();
        }

        /// <summary>
        /// Stops the randomizer from recording
        /// </summary>
        public void StopRecording()
        {
            _record = false;
            OnRecordingStopped();
        }

        public void ResumeRecording()
        {
            _record = true;
            OnRecordingResumed();
        }

        /// <summary>
        /// Clears the recorded values and disables recording
        /// </summary>
        public void ClearRecordedValues()
        {
            _recordedValues = new double[0];
            _cues.Clear();
            _playbackIndex = 0;
            _bufferIncrement = -1;
            _bufferIndex = -1;
            _lastLevelIndex = -1;
            OnRecordingCleared();
        }

        /// <summary>
        /// Rolls back the recorded values to a cue corresponding to the provided level
        /// </summary>
        public void RewindValues(ForeverLevel level)
        {
            if (_cues.ContainsKey(level)) {
                if (_cues[level] > _bufferIndex)
                {
                    Debug.LogError("Cannot rewind " + name + " because it is not recording");
                    return;
                }
                _playbackIndex = _cues[level];
            } else
            {
                Debug.LogError("Could not find rewind cue for level " + level.name + " in randomizer " + name + " cue levels " + _cues.Count);
            }
        }
        private void ExtendBuffer()
        {
            double[] newValues = new double[_recordedValues.Length + _bufferIncrement];
            _recordedValues.CopyTo(newValues, 0);
            _recordedValues = newValues;
        }

        private void RecordValue(double value)
        {
            _bufferIndex++;
            if (_bufferIndex >= _recordedValues.Length)
            {
                ExtendBuffer();
            }
            if (LevelGenerator.instance.currentLevelIndex != _lastLevelIndex)
            {
                _lastLevelIndex = LevelGenerator.instance.currentLevelIndex;
                var level = LevelGenerator.instance.currentLevel;
                if (_cues.ContainsKey(level))
                {
                    _cues[level] = _bufferIndex;
                }
                else
                {
                    _cues.Add(level, _bufferIndex);
                }
            }
            _recordedValues[_bufferIndex] = value;
        }
    }
}