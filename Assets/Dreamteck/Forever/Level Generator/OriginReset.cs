using UnityEngine;

namespace Dreamteck.Forever
{ 
    [AddComponentMenu("Dreamteck/Forever/Origin Reset")]
    public class OriginReset : MonoBehaviour
    {
        public Transform cameraTransform;
        public bool x = true;
        public bool y = true;
        public bool z = true;
        public delegate void FloatingOriginHandler(Vector3 delta);
        public static event FloatingOriginHandler onOriginOffset;
        Transform _transform;

        public float originResetDistance = 999f;

        private void Awake()
        {
            _transform = transform;
        }

        private void Start()
        {
            UpdateTransform();
        }

        private void OnEnable()
        {
            if(GetComponent<LevelGenerator>() == null)
            {
                Debug.LogError("The Floating origin is not attached to the Level Generator object but instead is attached to " + name + " - disabling.");
                enabled = false;
                return;
            }
        }

        void LateUpdate()
        {
            UpdateTransform();

            bool outOfBounds =  (x && Mathf.Abs(cameraTransform.position.x) > originResetDistance) ||
                                (y && Mathf.Abs(cameraTransform.position.y) > originResetDistance) ||
                                (z && Mathf.Abs(cameraTransform.position.z) > originResetDistance);

            if (outOfBounds)
            {
                if (!LevelGenerator.instance.isBusy)
                {
                    Vector3 delta = new Vector3(x ? cameraTransform.position.x : 0,
                                                y ? cameraTransform.position.y : 0,
                                                z ? cameraTransform.position.z : 0);

                    foreach (Transform child in _transform)
                    {
                        child.Translate(-delta, Space.World);
                    }

                    if (onOriginOffset != null) {
                        onOriginOffset(delta);
                    }
                }
            }
        }


        private void UpdateTransform()
        {
            if (cameraTransform == null)
            {
                Camera cam = Camera.main;
                if (cam != null) cameraTransform = cam.transform;
                else
                {
                    cam = Camera.current;
                    if (cam != null) cameraTransform = cam.transform;
                }
            }
        }

    }
}
