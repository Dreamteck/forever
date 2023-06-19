namespace Dreamteck.Forever
{
    using UnityEngine;
    using System.Collections.Generic;

    [AddComponentMenu("Dreamteck/Forever/Builders/Active Random Children")]
    public class ActiveRandomChildren : Builder
    {
        [Range(0f, 1f)]
        public float minPercent = 0f;
        [Range(0f, 1f)]
        public float maxPercent = 1f;
        private float percent = 0f;


        protected override void Awake()
        {
            base.Awake();
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(false);
            }
        }

#if UNITY_EDITOR
        public override void OnUnpack()
        {
            foreach (Transform child in transform) child.gameObject.SetActive(true);
        }
#endif

        protected override void Build()
        {
            base.Build();
            Transform trs = transform;
            percent = Mathf.Lerp(minPercent, maxPercent, Random(0f, 1f));
            if (trs.childCount == 0)  return;
            List<int> available = new List<int>();
            for (int i = 0; i < trs.childCount; i++) available.Add(i);
            int activeCount = Mathf.RoundToInt(trs.childCount * percent);
            for (int i = 0; i < activeCount; i++)
            {
                int rand = Random(0, available.Count);
                trs.GetChild(available[rand]).gameObject.SetActive(true);
                available.RemoveAt(rand);
            }
        }
    }
}
