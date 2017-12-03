using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Light2D.Examples
{
    [CustomEditor(typeof (HingeAutoRotator))]
    [CanEditMultipleObjects]
    public class HingeAutoRotatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (targets.Length == 1)
            {
                var myScript = (HingeAutoRotator) target;
                var connBody = myScript.Joint == null ? null : myScript.Joint.connectedBody;

                GUI.enabled = false;
                EditorGUILayout.ObjectField("Connected body", connBody, typeof (Rigidbody2D), true);
                GUI.enabled = true;
            }

            DrawDefaultInspector();
        }
    }
}