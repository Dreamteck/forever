namespace Dreamteck.Forever
{
    using UnityEngine;

    [CreateAssetMenu(menuName = "Forever/Randomizers/.NET Randomizer")]
    public class DotNETRandomizer : ForeverRandomizer
    {
        [SerializeField]
        private int _seed;

        [SerializeField]
        private bool _generateRandomSeed;

        /// <summary>
        /// The seed for the randomizer. Will override <see cref="generateRandomSeed"/> if set through a script
        /// </summary>
        public int seed
        {
            get { return _seed; }
            set {
                _seed = value;
                _bypassRandomSeed = true;
            }
        }

        /// <summary>
        /// If true, a random value will be picked for the seed upon initialization
        /// </summary>
        public bool generateRandomSeed
        {
            get { return _generateRandomSeed; }
            set { _generateRandomSeed = value; }
        }

        private System.Random _random = null;
        private bool _bypassRandomSeed = false;


        protected override void OnInitialize()
        {
            if (isRecording && _random != null) return;
            if (_generateRandomSeed && !_bypassRandomSeed)
            {
                _random = new System.Random(Random.Range(int.MinValue, int.MaxValue));
            } else
            {
                _random = new System.Random(_seed);
            }
            _bypassRandomSeed = false;
        }

        protected override double Next01()
        {
            return _random.NextDouble();
        }

        protected override void OnRecordingCleared()
        {
            _random = null;
        }
    }
}