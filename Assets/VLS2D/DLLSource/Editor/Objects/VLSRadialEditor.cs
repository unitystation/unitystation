using UnityEngine;
using UnityEditor;
using System.Collections;

namespace PicoGames.VLS2D
{
    [CustomEditor(typeof(VLSRadial))]
    public class VLSRadialEditor : VLSLightEditor
    {
        private SerializedProperty edgeCount;
        private SerializedProperty coneAngle;
        //private SerializedProperty penetration;

        private Vector3 position = new Vector3();
        private Vector3 localScale = new Vector3();
        private Quaternion rotation = new Quaternion();

        protected override void OnEnable()
        {
            edgeCount = serializedObject.FindProperty("edgeCount");
            coneAngle = serializedObject.FindProperty("coneAngle");
            //penetration = serializedObject.FindProperty("penetration");

            position = (serializedObject.targetObject as VLSRadial).transform.position;
            rotation = (serializedObject.targetObject as VLSRadial).transform.rotation;
            localScale = (serializedObject.targetObject as VLSRadial).transform.localScale;

            Tools.hidden = true;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            Tools.hidden = false;
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            {
                EditorGUILayout.PropertyField(edgeCount);
                EditorGUILayout.PropertyField(coneAngle);
                //EditorGUILayout.PropertyField(penetration);
            }
            if (serializedObject.ApplyModifiedProperties())
            {
                (serializedObject.targetObject as VLSRadial).RecalculateVerts();
                (serializedObject.targetObject as VLSRadial).SetDirty();
            }
        }

        protected virtual void OnSceneGUI()
        {
            UpdateTransformHandles();
        }

        protected virtual void UpdateTransformHandles()
        {
            Transform transform = (serializedObject.targetObject as VLSLight).transform;
            Quaternion toolRotation = (Tools.pivotRotation == PivotRotation.Local) ? transform.rotation : Quaternion.identity;

            EditorGUI.BeginChangeCheck();
            {
                switch (Tools.current)
                {
                    case Tool.Move:
                        position = Handles.PositionHandle(position, toolRotation);
                        break;
                    case Tool.Rotate:
                        rotation = Handles.RotationHandle(rotation, transform.position);
                        break;
                    case Tool.Scale:
                        localScale = Handles.ScaleHandle(localScale, transform.position, toolRotation, HandleUtility.GetHandleSize(transform.position));
                        break;
                    default:
                        position = Handles.PositionHandle(transform.position, toolRotation);
                        break;
                }
            }
            if(EditorGUI.EndChangeCheck())
            {
                transform.position = position;
                transform.localScale = localScale;
                transform.rotation = rotation;
            }
        }
    }
}