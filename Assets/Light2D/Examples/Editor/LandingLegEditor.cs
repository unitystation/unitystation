using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Light2D.Examples
{
    [CustomEditor(typeof (LandingLeg))]
    [CanEditMultipleObjects]
    public class LandingLegEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (targets.Length == 1)
            {
                var myScript = (LandingLeg) target;
                var joint = myScript.AutoRotator == null ? null : myScript.AutoRotator.Joint;
                var connBody = joint == null ? null : joint.connectedBody;

                GUI.enabled = false;
                EditorGUILayout.ObjectField("Connected body", connBody, typeof (Rigidbody2D), true);
                GUI.enabled = true;
            }

            DrawDefaultInspector();
        }
    }
}