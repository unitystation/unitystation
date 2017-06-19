using UnityEngine;
using UnityEditor;
using System.Collections;

namespace PicoGames.VLS2D
{
    [CustomEditor(typeof(VLSDirectional))]
    public class VLSDirectionalEditor : VLSLightEditor
    {
        //private SerializedProperty edges;

        protected override void OnEnable()
        {
            //edges = serializedObject.FindProperty("edgeCount");

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            {
                //EditorGUILayout.PropertyField(edges);
                //EditorGUILayout.PropertyField(coneAngle);
                //EditorGUILayout.PropertyField(penetration);
            }
            if (serializedObject.ApplyModifiedProperties())
            {
                (serializedObject.targetObject as VLSRadial).RecalculateVerts();
                (serializedObject.targetObject as VLSRadial).SetDirty();
            }

            base.OnInspectorGUI();
        }

        protected virtual void OnSceneGUI()
        {
            Transform t = (serializedObject.targetObject as VLSDirectional).transform;

            Handles.color = (serializedObject.targetObject as VLSDirectional).Color;
            Handles.ArrowCap(0, (t.up * t.localScale.y * 0.5f) + t.position, Quaternion.FromToRotation(t.forward, -t.up), HandleUtility.GetHandleSize(t.position) * 0.5f);
        }
    }
}