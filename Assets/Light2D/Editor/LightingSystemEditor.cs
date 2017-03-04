using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Light2D
{
    [CustomEditor(typeof(LightingSystem))]
    public class LightingSystemEditor : Editor
    {
        private SerializedProperty _lightPixelSize;
        private SerializedProperty _lightCameraSizeAdd;
        private SerializedProperty _lightCameraFovAdd;
        private SerializedProperty _enableAmbientLight;
        private SerializedProperty _blurLightSources;
        private SerializedProperty _blurAmbientLight ;
        private SerializedProperty  _hdr ;
        private SerializedProperty _lightObstaclesAntialiasing;
        private SerializedProperty _ambientLightComputeMaterial;
        private SerializedProperty _lightOverlayMaterial;
        private SerializedProperty _lightSourcesBlurMaterial;
        private SerializedProperty _ambientLightBlurMaterial;
        private SerializedProperty _lightCamera;
        private SerializedProperty _lightSourcesLayer;
        private SerializedProperty _ambientLightLayer;
        private SerializedProperty _lightObstaclesLayer;
        private SerializedProperty _lightObstaclesDistance;
        private SerializedProperty _lightTexturesFilterMode;
        private SerializedProperty _enableNormalMapping;
        private SerializedProperty _affectOnlyThisCamera;
#if LIGHT2D_2DTK
        private float _old2dtkCamSize;
        private DateTime _sizeChangeTime;
#endif

        void OnEnable()
        {
            _lightPixelSize = serializedObject.FindProperty("LightPixelSize");
            _lightCameraSizeAdd = serializedObject.FindProperty("LightCameraSizeAdd");
            _lightCameraFovAdd = serializedObject.FindProperty("LightCameraFovAdd");
            _enableAmbientLight = serializedObject.FindProperty("EnableAmbientLight");
            _blurLightSources = serializedObject.FindProperty("BlurLightSources");
            _blurAmbientLight = serializedObject.FindProperty("BlurAmbientLight");
            _hdr = serializedObject.FindProperty("HDR");
            _lightObstaclesAntialiasing = serializedObject.FindProperty("LightObstaclesAntialiasing");
            _ambientLightComputeMaterial = serializedObject.FindProperty("AmbientLightComputeMaterial");
            _lightOverlayMaterial = serializedObject.FindProperty("LightOverlayMaterial");
            _lightSourcesBlurMaterial = serializedObject.FindProperty("LightSourcesBlurMaterial");
            _ambientLightBlurMaterial = serializedObject.FindProperty("AmbientLightBlurMaterial");
            _lightCamera = serializedObject.FindProperty("LightCamera");
            _lightSourcesLayer = serializedObject.FindProperty("LightSourcesLayer");
            _ambientLightLayer = serializedObject.FindProperty("AmbientLightLayer");
            _lightObstaclesLayer = serializedObject.FindProperty("LightObstaclesLayer");
            _lightObstaclesDistance = serializedObject.FindProperty("LightObstaclesDistance");
            _lightTexturesFilterMode = serializedObject.FindProperty("LightTexturesFilterMode");
            _enableNormalMapping = serializedObject.FindProperty("EnableNormalMapping");
            _affectOnlyThisCamera = serializedObject.FindProperty("AffectOnlyThisCamera");
        }

        public override void OnInspectorGUI()
        {
            // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
            serializedObject.Update();

            if (Application.isPlaying)
                GUI.enabled = false;

            var lightingSystem = (LightingSystem)target;
            var cam = lightingSystem.GetComponent<Camera>();
            bool isMobileTarget = EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ||
                                EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android;

            if (cam == null)
            {
                EditorGUILayout.LabelField("WARNING: No attached camera found.");
            }

            EditorGUILayout.PropertyField(_lightPixelSize, new GUIContent("Light Pixel Size"));

            bool sizeChanged = false;
#if LIGHT2D_2DTK
            var tk2dCamera = lightingSystem.GetComponent<tk2dCamera>();
            var tk2dCamSize = tk2dCamera == null
                ? (cam == null ? 0 : cam.orthographicSize)
                : tk2dCamera.ScreenExtents.yMax;
            var currSizeChanged = !Mathf.Approximately(tk2dCamSize, _old2dtkCamSize);
            _old2dtkCamSize = tk2dCamSize;
            if (currSizeChanged) _sizeChangeTime = DateTime.Now;
            sizeChanged = (DateTime.Now - _sizeChangeTime).TotalSeconds < 0.2f;
#endif
            if (cam != null)
            {
                float size;
                if (cam.orthographic)
                {
#if LIGHT2D_2DTK
                    float zoom = (tk2dCamera == null ? 1 : tk2dCamera.ZoomFactor);
                    size = (cam.orthographicSize*zoom + _lightCameraSizeAdd.floatValue) * 2f;
#else
                    size = (cam.orthographicSize + _lightCameraSizeAdd.floatValue)*2f;
#endif
                }
                else
                {
                    var halfFov = (cam.fieldOfView + _lightCameraFovAdd.floatValue)*Mathf.Deg2Rad/2f;
                    size = Mathf.Tan(halfFov)*_lightObstaclesDistance.floatValue*2;
                }
                if (!Application.isPlaying)
                {

                    int lightTextureHeight = Mathf.RoundToInt(size/_lightPixelSize.floatValue);
                    var oldSize = lightTextureHeight;
                    lightTextureHeight = EditorGUILayout.IntField("Light Texture Height", lightTextureHeight);
                    if (lightTextureHeight%2 != 0)
                        lightTextureHeight++;
                    if (lightTextureHeight < 16)
                    {
                        if (lightTextureHeight < 8)
                            lightTextureHeight = 8;
                        EditorGUILayout.LabelField("WARNING: Light Texture Height is too small.");
                        EditorGUILayout.LabelField(" 50-180 is recommended.");
                    }
                    if (lightTextureHeight > (isMobileTarget ? 200 : 400))
                    {
                        if (lightTextureHeight > 1024)
                            lightTextureHeight = 1024;
                        EditorGUILayout.LabelField("WARNING: Light Texture Height is too big.");
                        EditorGUILayout.LabelField(" 50-180 is recommended.");
                    }
                    if (oldSize != lightTextureHeight && !sizeChanged)
                    {
                        _lightPixelSize.floatValue = size/lightTextureHeight;
                    }
                }
            }

            if (cam == null || cam.orthographic)
            {
                EditorGUILayout.PropertyField(_lightCameraSizeAdd, new GUIContent("Light Camera Size Add"));
            }
            else
            {
                EditorGUILayout.PropertyField(_lightCameraFovAdd, new GUIContent("Light Camera Fov Add"));
                EditorGUILayout.PropertyField(_lightObstaclesDistance, new GUIContent("Camera To Light Obstacles Distance"));
            }

            EditorGUILayout.PropertyField(_hdr, new GUIContent("64 Bit Color"));
            EditorGUILayout.PropertyField(_lightObstaclesAntialiasing, new GUIContent("Light Obstacles Antialiasing"));
            EditorGUILayout.PropertyField(_enableNormalMapping, new GUIContent("Normal Mapping"));
            if (_enableNormalMapping.boolValue && isMobileTarget)
            {
                EditorGUILayout.LabelField("WARNING: Normal mapping is not supported on mobiles.");
            }
            //EditorGUILayout.PropertyField(_affectOnlyThisCamera, new GUIContent("Affect Only This Camera"));
            _lightTexturesFilterMode.enumValueIndex = (int)(FilterMode)EditorGUILayout.EnumPopup(
                "Texture Filtering", (FilterMode)_lightTexturesFilterMode.enumValueIndex);

            EditorGUILayout.PropertyField(_blurLightSources, new GUIContent("Blur Light Sources"));

            bool normalGuiEnableState = GUI.enabled;
            if (!_blurLightSources.boolValue)
                GUI.enabled = false;
            EditorGUILayout.PropertyField(_lightSourcesBlurMaterial, new GUIContent("   Light Sources Blur Material"));
            GUI.enabled = normalGuiEnableState;

            EditorGUILayout.PropertyField(_enableAmbientLight, new GUIContent("Enable Ambient Light"));
            if (!_enableAmbientLight.boolValue)
                GUI.enabled = false;
            EditorGUILayout.PropertyField(_blurAmbientLight, new GUIContent("   Blur Ambient Light"));
            var oldEnabled = GUI.enabled;
            if (!_blurAmbientLight.boolValue)
                GUI.enabled = false;
            EditorGUILayout.PropertyField(_ambientLightBlurMaterial, new GUIContent("   Ambient Light Blur Material"));
            GUI.enabled = oldEnabled;
            EditorGUILayout.PropertyField(_ambientLightComputeMaterial, new GUIContent("   Ambient Light Compute Material"));
            GUI.enabled = normalGuiEnableState;

            EditorGUILayout.PropertyField(_lightOverlayMaterial, new GUIContent("Light Overlay Material"));
            EditorGUILayout.PropertyField(_lightCamera, new GUIContent("Lighting Camera"));
            _lightSourcesLayer.intValue = EditorGUILayout.LayerField(new GUIContent("Light Sources Layer"), _lightSourcesLayer.intValue);
            _lightObstaclesLayer.intValue = EditorGUILayout.LayerField(new GUIContent("Light Obstacles Layer"), _lightObstaclesLayer.intValue);
            _ambientLightLayer.intValue = EditorGUILayout.LayerField(new GUIContent("Ambient Light Layer"), _ambientLightLayer.intValue);

            // Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
            serializedObject.ApplyModifiedProperties();
        }
    }
}
