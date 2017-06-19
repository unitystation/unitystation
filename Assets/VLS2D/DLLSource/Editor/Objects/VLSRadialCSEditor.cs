using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace PicoGames.VLS2D
{
    [CustomEditor(typeof(VLSRadialCS))]
    public class VLSRadialCSEditor : VLSLightEditor
    {
        private Vector3 position = new Vector3();
        private Vector3 localScale = new Vector3();
        private Quaternion rotation = new Quaternion();
                
        protected override void OnEnable()
        {
            position = (serializedObject.targetObject as VLSLight).transform.position;
            rotation = (serializedObject.targetObject as VLSLight).transform.rotation;
            localScale = (serializedObject.targetObject as VLSLight).transform.localScale;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            ShowEdgeInspector();
            base.OnInspectorGUI();
        }

        void OnSceneGUI()
        {
            DrawEdgeEditor();
            //UpdateTransformHandles();

            if (GUI.changed)
                EditorUtility.SetDirty(serializedObject.targetObject);
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
                        position = Handles.PositionHandle(transform.position, toolRotation);
                        break;
                    case Tool.Rotate:
                        rotation = Handles.RotationHandle(transform.rotation, transform.position);
                        break;
                    case Tool.Scale:
                        localScale = Handles.ScaleHandle(transform.localScale, transform.position, toolRotation, HandleUtility.GetHandleSize(transform.position));
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