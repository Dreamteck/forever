namespace Dreamteck.Forever.Editor
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;


    [CustomEditor(typeof(ActiveRandomChildren))]
    public class ActiveRandomChildrenEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            ActiveRandomChildren active = (ActiveRandomChildren)target;
            int childCount = active.transform.childCount;
            EditorGUILayout.LabelField("Total: " + childCount + " (Min " + Mathf.RoundToInt(childCount * active.minPercent) + ") - (Max " + Mathf.RoundToInt(childCount * active.maxPercent) + ")");
            if (GUILayout.Button("Preview density"))
            {
                List<GameObject> children = new List<GameObject>();
                foreach (Transform child in active.transform)
                {
                    child.gameObject.SetActive(false);
                    children.Add(child.gameObject);
                }
                float percent = Mathf.Lerp(active.minPercent, active.maxPercent, Random.Range(0f, 1f));
                int activeCount = Mathf.RoundToInt(childCount * percent);
                for (int i = 0; i < activeCount; i++)
                {
                    int rand = Random.Range(0, children.Count);
                    children[rand].SetActive(true);
                    children.RemoveAt(rand);
                }
            }

            if (GUILayout.Button("Activate All"))
            {
                foreach (Transform child in active.transform)
                {
                    child.gameObject.SetActive(true);
                }
            }

            if (GUILayout.Button("Deactivate All"))
            {
                foreach (Transform child in active.transform)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        float CeilFloat(float number, int digitsAfterPoint)
        {
            return Mathf.Ceil(number * (float)Mathf.Pow(10, digitsAfterPoint)) / (float)Mathf.Pow(10, digitsAfterPoint);
        }
    }
}
