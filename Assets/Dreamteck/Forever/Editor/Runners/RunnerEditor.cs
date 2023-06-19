namespace Dreamteck.Forever.Editor
{
    using UnityEngine;
    using UnityEditor;

    [CustomEditor(typeof(Runner), true)]
    public class RunnerEditor : Editor
    {
        MotionEditor motionEditor;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            Runner runner = (Runner)target;
            Undo.RecordObject(runner, runner.name + " - Edit Values");
            if(motionEditor == null) motionEditor = new MotionEditor(runner.motion);
            motionEditor.Draw();
            runner.startMode = (Runner.StartMode)EditorGUILayout.EnumPopup("Start Mode", runner.startMode);
            if(runner.startMode == Runner.StartMode.Percent) runner.startPercent = EditorGUILayout.Slider("Start Percent", (float)runner.startPercent, 0f, 1f);
            else if(runner.startMode == Runner.StartMode.Distance) runner.startDistance = EditorGUILayout.FloatField("Start Distance", runner.startDistance);
            if (runner.startDistance < 0f) runner.startDistance = 0f;
        }

        internal class MotionEditor
        {
            private Runner.MotionModule motion;
            bool open = false;

            internal MotionEditor(Runner.MotionModule input)
            {
                motion = input;
            }

            internal void Draw()
            {
                open = EditorGUILayout.Foldout(open, "Motion");
                if (open) DrawLogic();
            }

            private void DrawLogic()
            {
                EditorGUI.indentLevel = 1;
                motion.enabled = EditorGUILayout.Toggle("Apply Motion", motion.enabled);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Position", GUILayout.Width(EditorGUIUtility.labelWidth));
                motion.applyPositionX = EditorGUILayout.Toggle(motion.applyPositionX, GUILayout.Width(30));
                GUILayout.Label("X", GUILayout.Width(20));
                motion.applyPositionY = EditorGUILayout.Toggle(motion.applyPositionY, GUILayout.Width(30));
                GUILayout.Label("Y", GUILayout.Width(20));
                motion.applyPositionZ = EditorGUILayout.Toggle(motion.applyPositionZ, GUILayout.Width(30));
                GUILayout.Label("Z", GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();
                if (motion.applyPosition)
                {
                    EditorGUI.indentLevel = 2;
                    motion.offset = EditorGUILayout.Vector2Field("Offset", motion.offset);
                    motion.useSplineSizes = EditorGUILayout.Toggle("Multiply By Path Size", motion.useSplineSizes);
                }
                EditorGUI.indentLevel = 1;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Rotation", GUILayout.Width(EditorGUIUtility.labelWidth));
                motion.applyRotationX = EditorGUILayout.Toggle(motion.applyRotationX, GUILayout.Width(30));
                GUILayout.Label("X", GUILayout.Width(20));
                motion.applyRotationY = EditorGUILayout.Toggle(motion.applyRotationY, GUILayout.Width(30));
                GUILayout.Label("Y", GUILayout.Width(20));
                motion.applyRotationZ = EditorGUILayout.Toggle(motion.applyRotationZ, GUILayout.Width(30));
                GUILayout.Label("Z", GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                if (motion.applyRotation)
                {
                    EditorGUI.indentLevel = 2;
                    motion.rotationOffset = EditorGUILayout.Vector3Field("Offset", motion.rotationOffset);
                }
                EditorGUI.indentLevel = 1;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Scale", GUILayout.Width(EditorGUIUtility.labelWidth));
                motion.applyScaleX = EditorGUILayout.Toggle(motion.applyScaleX, GUILayout.Width(30));
                GUILayout.Label("X", GUILayout.Width(20));
                motion.applyScaleY = EditorGUILayout.Toggle(motion.applyScaleY, GUILayout.Width(30));
                GUILayout.Label("Y", GUILayout.Width(20));
                motion.applyScaleZ = EditorGUILayout.Toggle(motion.applyScaleZ, GUILayout.Width(30));
                GUILayout.Label("Z", GUILayout.Width(20));
                EditorGUILayout.EndHorizontal();

                if (motion.applyScale)
                {
                    EditorGUI.indentLevel = 2;
                    motion.baseScale = EditorGUILayout.Vector3Field("Base scale", motion.baseScale);
                }
                EditorGUI.indentLevel = 0;
            }
        }
    }
}
