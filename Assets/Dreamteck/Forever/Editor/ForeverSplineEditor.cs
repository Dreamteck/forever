namespace Dreamteck.Forever.Editor
{
    using Dreamteck.Splines;
    using Dreamteck.Splines.Editor;
    using UnityEditor;
    using UnityEngine;

    public class ForeverSplineEditor : SplineEditor
    {
        protected override string editorName { get { return _customName; } }
        private string _customName = "ForeverSplineEditor";

        public ForeverSplineEditor(Matrix4x4 transformMatrix, SerializedObject parentObject, string customName, string customSplinePropertyName) : base(transformMatrix, parentObject, customSplinePropertyName)
        {
            _customName = customName;
        }

        public ForeverSplineEditor (Matrix4x4 transformMatrix, SerializedObject parentObject, string customName) : base(transformMatrix, parentObject)
        {
            _customName = customName;
        }

        public override void BeforeSceneGUI(SceneView current)
        {
            SetupModule(mainModule);
            for (int i = 0; i < moduleCount; i++)
            {
                SetupModule(GetModule(i));
            }
            base.BeforeSceneGUI(current);
        }

        private void SetupModule(PointModule module)
        {
            module.duplicationDirection = Spline.Direction.Forward;
            module.highlightColor = ForeverPrefs.highlightColor;
            module.showPointNumbers = false;
        }
    }
}
