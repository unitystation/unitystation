using UnityEngine;
using UnityEditor;
using System.Collections;

namespace PicoGames.VLS2D
{
    [CustomEditor(typeof(VLSProShader)), DisallowMultipleComponent]
    public class VLSProShaderEditor : Editor
    {
        public static string[] LightPassList = new string[0];

        private SerializedProperty useAsUtility;
        private SerializedProperty lightPasses;
        private SerializedProperty defaultLayer;
        private SerializedProperty layerPasses;

        private static int lightPassCount = 0;
        //private bool showDefault = false;
        //private bool showOverlay = false;
        
        void OnEnable()
        {
            useAsUtility = serializedObject.FindProperty("useAsUtility");
            lightPasses = serializedObject.FindProperty("lightPasses");
            defaultLayer = serializedObject.FindProperty("defaultLayer");
            layerPasses = serializedObject.FindProperty("layerPasses");

            //showDefault = EditorPrefs.GetBool("SHOW_DEFAULT", false);
            //showOverlay = EditorPrefs.GetBool("SHOW_OVERLAY", false);
        }

        void OnDisable()
        {
            //EditorPrefs.SetBool("SHOW_DEFAULT", showDefault);
            //EditorPrefs.SetBool("SHOW_OVERLAY", showOverlay);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            {
                //EditorGUILayout.PropertyField(useAsUtility);

                //if (useAsUtility.boolValue)
                //    EditorGUILayout.HelpBox("These settings will not render to the camera. You must pass a RenderTexture and a Light Layer name/index into the 'BlitLightsToTexture' function to render lights onto a texture.", MessageType.Warning);
                
                VLSLayerList.Show(lightPasses, "Light Passes");

                EditorGUILayout.Space();

                EditorGUILayout.PrefixLabel("Base Layer");
                EditorGUI.indentLevel++;
                {
                    EditorGUILayout.HelpBox("Layer and Background settings should be set using the attached camera's 'Background' and 'Culling Mask' settings.", MessageType.Info);

                    EditorGUILayout.PropertyField(defaultLayer.FindPropertyRelative("useSceneAmbientColor"), new GUIContent("Scene Ambience", ""));
                    if (!defaultLayer.FindPropertyRelative("useSceneAmbientColor").boolValue)
                    {
                        EditorGUILayout.PropertyField(defaultLayer.FindPropertyRelative("ambientColor"), new GUIContent("Ambient Color", ""));
                    }

                    EditorGUILayout.PropertyField(defaultLayer.FindPropertyRelative("blur"));
                    //EditorGUILayout.PropertyField(defaultLayer.FindPropertyRelative("overlay"));

                    GUILayout.Space(2);
                    EditorGUILayout.PropertyField(defaultLayer.FindPropertyRelative("lightsEnabled"), new GUIContent("Apply VLSLight", ""));
                    GUI.enabled = defaultLayer.FindPropertyRelative("lightsEnabled").boolValue;
                    {
                        EditorGUI.indentLevel++;
                        //VLSProShaderEditor.GenerateSelectionList(defaultLayer);

                        EditorGUI.BeginChangeCheck();
                        {
                            defaultLayer.FindPropertyRelative("lightLayerMask").intValue = EditorGUILayout.MaskField("Light Layers", defaultLayer.FindPropertyRelative("lightLayerMask").intValue, VLSProShaderEditor.LightPassList);
                        }
                        EditorGUI.EndChangeCheck();

                        EditorGUILayout.PropertyField(defaultLayer.FindPropertyRelative("lightIntensity"), new GUIContent("Light Intensity", ""));
                        EditorGUI.indentLevel--;
                    }
                    GUI.enabled = true;
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();

                VLSLayerList.Show(layerPasses, "Extra Layer Passes");
            }
            serializedObject.ApplyModifiedProperties();

            if(GUI.changed)
                VLSProShaderEditor.GenerateSelectionList(defaultLayer);
        }
                
        public static void GenerateSelectionList(SerializedProperty _property)
        {
            lightPassCount = _property.serializedObject.FindProperty("lightPasses").arraySize;

            if (LightPassList.Length != lightPassCount)
                LightPassList = new string[lightPassCount];

            for (int i = 0; i < LightPassList.Length; i++)
                LightPassList[i] = _property.serializedObject.FindProperty("lightPasses").GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
        }
    }
}