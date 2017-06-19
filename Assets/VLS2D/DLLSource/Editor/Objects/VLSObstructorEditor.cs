using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace PicoGames.VLS2D
{
    [CustomEditor(typeof(VLSObstructor))]
    public class VLSObstructorEditor : VLSBehaviourEditor
    {
        private SerializedProperty collider2DReference;
        private SerializedProperty collider3DReference;
        private SerializedProperty colliderReferenceType;
        private SerializedProperty circleResolution;
        private SerializedProperty polyColliderPathIndex;

        protected override void OnEnable()
        {
            collider2DReference = serializedObject.FindProperty("collider2DReference");
            collider3DReference = serializedObject.FindProperty("collider3DReference");
            colliderReferenceType = serializedObject.FindProperty("colliderReferenceType");
            circleResolution = serializedObject.FindProperty("circleResolution");
            polyColliderPathIndex = serializedObject.FindProperty("polyColliderPathIndex");

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            {
                EditorGUILayout.PropertyField(colliderReferenceType);

                if (colliderReferenceType.enumValueIndex != 0)
                {
                    if (colliderReferenceType.enumValueIndex == (int)ColliderReferenceType._2D)
                    {
                        EditorGUILayout.PropertyField(collider2DReference);

                        if (collider2DReference.objectReferenceValue is CircleCollider2D)
                            EditorGUILayout.PropertyField(circleResolution);

                        if (collider2DReference.objectReferenceValue is PolygonCollider2D)
                            EditorGUILayout.PropertyField(polyColliderPathIndex);
                    }

                    if (colliderReferenceType.enumValueIndex == (int)ColliderReferenceType._3D)
                    {
                        EditorGUILayout.PropertyField(collider3DReference);

                        if (collider3DReference.objectReferenceValue is SphereCollider)
                            EditorGUILayout.PropertyField(circleResolution);
                    }

                    if (GUILayout.Button("Update Verts From Collider"))
                    {
                        (serializedObject.targetObject as VLSObstructor).UpdateFromReferencedCollider();
                        EditorUtility.SetDirty(serializedObject.targetObject);
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();

            ShowEdgeInspector();
                        
            if (GUI.changed)
                EditorUtility.SetDirty(serializedObject.targetObject);
        }

        protected virtual void OnSceneGUI()
        {
            DrawEdgeEditor();

            if (GUI.changed)
                EditorUtility.SetDirty(serializedObject.targetObject);
        }
    }
}