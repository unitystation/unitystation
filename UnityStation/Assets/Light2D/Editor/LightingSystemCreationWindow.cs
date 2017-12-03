using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Light2D
{
    public class LightingSystemCreationWindow : EditorWindow
    {
        private int _lightObstaclesLayer;
        private int _lightSourcesLayer;
        private int _ambientLightLayer;

        public static void CreateWindow()
        {
            var window = GetWindow<LightingSystemCreationWindow>("Lighting system creation window");
            window.position = new Rect(200, 200, 500, 140);
        }

        void OnGUI()
        {
            if (FindObjectOfType<LightingSystem>())
            {
                GUILayout.Label("WARNING: existing lighting system is found.\nIt is recommended to remove it first, before adding new one.", EditorStyles.boldLabel);
            }

            GUILayout.Label("Select layers you wish to use. You could modify them later in created object.");
            _lightObstaclesLayer = EditorGUILayout.LayerField("Light Obstacles", _lightObstaclesLayer);
            _lightSourcesLayer = EditorGUILayout.LayerField("Light Sources", _lightSourcesLayer);
            _ambientLightLayer = EditorGUILayout.LayerField("Ambient Light", _ambientLightLayer);

            if (GUILayout.Button("Create"))
            {
                var mainCamera = Camera.main;
                var lighingSystem = mainCamera.GetComponent<LightingSystem>() ?? mainCamera.gameObject.AddComponent<LightingSystem>();

                var prefab = Resources.Load<GameObject>("Lighting Camera");
                var lightingSystemObj = (GameObject)Instantiate(prefab);
                lightingSystemObj.name = lightingSystemObj.name.Replace("(Clone)", "");
                lightingSystemObj.transform.parent = mainCamera.transform;
                lightingSystemObj.transform.localPosition = Vector3.zero;
                lightingSystemObj.transform.localScale = Vector3.one;
                lightingSystemObj.transform.localRotation = Quaternion.identity;

                var config = lightingSystemObj.GetComponent<LightingSystemPrefabConfig>();

                lighingSystem.LightCamera = lightingSystemObj.GetComponent<Camera>();
                lighingSystem.AmbientLightComputeMaterial = config.AmbientLightComputeMaterial;
                lighingSystem.LightOverlayMaterial = config.LightOverlayMaterial;
                lighingSystem.AmbientLightBlurMaterial = lighingSystem.LightSourcesBlurMaterial = config.BlurMaterial;

                DestroyImmediate(config);

                lighingSystem.LightCamera.depth = mainCamera.depth - 1;

                lighingSystem.LightCamera.cullingMask = 1 << _lightSourcesLayer;

                lighingSystem.LightSourcesLayer = _lightSourcesLayer;
                lighingSystem.AmbientLightLayer = _ambientLightLayer;
                lighingSystem.LightObstaclesLayer = _lightObstaclesLayer;

                mainCamera.cullingMask &=
                    ~((1 << _lightSourcesLayer) | (1 << _ambientLightLayer) | (1 << _lightObstaclesLayer));

                Close();
            }
        }
    }
}
