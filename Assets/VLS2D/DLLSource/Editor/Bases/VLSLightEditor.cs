using UnityEngine;
using UnityEditor;
using System.Collections;

// using statements to get layer names
using System;
using UnityEditorInternal;
using System.Reflection;

namespace PicoGames.VLS2D
{
    [CustomEditor(typeof(VLSLight))]
    public class VLSLightEditor : VLSBehaviourEditor
    {
        private SerializedProperty isStatic;
        private SerializedProperty color;
        private SerializedProperty material;
        private SerializedProperty shadowLayer;

        private SerializedProperty sortingLayerID;
        private SerializedProperty sortingOrder;

        private static string[] layerNames;
        private static int[] layerIDs;

        protected override void OnEnable()
        {
            color = serializedObject.FindProperty("color");
            material = serializedObject.FindProperty("material");
            shadowLayer = serializedObject.FindProperty("shadowLayer");
            isStatic = serializedObject.FindProperty("isStatic");

            sortingLayerID = serializedObject.FindProperty("sortingLayerID");
            sortingOrder = serializedObject.FindProperty("sortingOrder");

            layerNames = GetSortingLayerNames();
            layerIDs = GetSortingLayerUniqueIDs();

            EditorUtility.SetSelectedWireframeHidden((serializedObject.targetObject as VLSLight).GetComponent<MeshRenderer>(), true);

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            {
                EditorGUILayout.PropertyField(isStatic);
                EditorGUILayout.PropertyField(color);
                EditorGUILayout.PropertyField(material);
                EditorGUILayout.PropertyField(shadowLayer);

                EditorGUILayout.Space();

                sortingLayerID.intValue = EditorGUILayout.IntPopup("Sorting Layer", sortingLayerID.intValue, layerNames, layerIDs);
                EditorGUILayout.PropertyField(sortingOrder, new GUIContent("Order in Layer", ""));

                EditorGUILayout.Space();
            }
            if (serializedObject.ApplyModifiedProperties())
                (serializedObject.targetObject as VLSLight).SetDirty();

            //base.OnInspectorGUI();
        }

        public static string[] GetSortingLayerNames()
        {
            Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
            return (string[])sortingLayersProperty.GetValue(null, new object[0]);
        }

        public static int[] GetSortingLayerUniqueIDs()
        {
            Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayerUniqueIDsProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
            return (int[])sortingLayerUniqueIDsProperty.GetValue(null, new object[0]);
        }
    }
}