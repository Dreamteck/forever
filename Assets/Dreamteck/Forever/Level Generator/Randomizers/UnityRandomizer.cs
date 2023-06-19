namespace Dreamteck.Forever
{
    using UnityEngine;

    [CreateAssetMenu(menuName = "Forever/Randomizers/Unity Randomizer")]
    public class UnityRandomizer : ForeverRandomizer
    {
        protected override double Next01()
        {
            return Random.Range(0f, 1f);
        }
    }
}