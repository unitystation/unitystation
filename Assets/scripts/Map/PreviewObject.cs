#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MapEditor {

    [ExecuteInEditMode]
    public class PreviewObject: MonoBehaviour {
        private static PreviewObject instance;
        public static PreviewObject Instance {
            get {
                if(!instance) {
                    GameObject instanceTemp = GameObject.FindGameObjectWithTag("MapEditor");
                    if(instanceTemp != null) {
                        instance = instanceTemp.GetComponentInChildren<PreviewObject>(true);
                    } else {
                        instance = null;
                    }
                }

                return instance;
            }
        }

        public static bool ShowPreview { get; set; }
        private static SceneView currentSceneView;

        private static GameObject prefab;
        public static GameObject Prefab {
            get {
                return prefab;
            }
            set {
                if(prefab != value) {
                    prefab = value;

                    if(Instance)
                        Instance.UpdatePrefab();

                    if(prefab && currentSceneView) {
                        currentSceneView.Focus();
                    }
                }
            }
        }

        public Material previewMaterial;

        private SpriteRotate spriteRotate;

        public static void Update(SceneView sceneView) {
            SetActive(ShowPreview);
            currentSceneView = sceneView;
            if(Instance != null) {
                Instance.FollowMouse(Event.current);
                Instance.RemoveFromSelection();
            }
        }

        public static GameObject CreateGameObject() {
            var gameObject = (GameObject) PrefabUtility.InstantiatePrefab(Prefab);

            var spriteRotate = gameObject.GetComponentInChildren<SpriteRotate>();
            if(spriteRotate)
                spriteRotate.RotateIndex = Instance.spriteRotate.RotateIndex;

            return gameObject;
        }

        public static void RotateForwards() {
            if(Instance && Instance.spriteRotate)
                Instance.spriteRotate.RotateForwards();
        }

        public static void RotateBackwards() {
            if(Instance && Instance.spriteRotate)
                Instance.spriteRotate.RotateBackwards();
        }

        private void FollowMouse(Event e) {
            Ray r = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));

            int x = Mathf.RoundToInt(r.origin.x);
            int y = Mathf.RoundToInt(r.origin.y);

            transform.position = new Vector3(x, y, 0);
        }

        private void RemoveFromSelection() {
            if(Selection.Contains(gameObject)) {
                Selection.objects = Array.FindAll(Selection.objects, o => (o != gameObject));
            }
            foreach(Transform child in transform) {
                if(Selection.Contains(child.gameObject)) {
                    Selection.objects = Array.FindAll(Selection.objects, o => (o != child.gameObject));
                }
            }
        }

        public static void SetActive(bool active) {
            if(Instance && Instance.gameObject.activeInHierarchy != active) {
                Instance.gameObject.SetActive(active);

                if(active) {
                    if(prefab && currentSceneView) {
                        currentSceneView.Focus();
                    }
                } else {
                    Prefab = null;
                }
            }
        }

        private void UpdatePrefab() {
            for(int i = transform.childCount - 1; i >= 0; i--) {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            if(prefab) {
                foreach(Transform child in prefab.transform) {
                    var c = Instantiate(child.gameObject);

                    foreach(var script in c.GetComponentsInChildren<MonoBehaviour>()) {
                        script.enabled = false;
                    }

                    c.transform.parent = transform;
                    c.transform.localPosition = c.transform.position;
                }

                foreach(var renderer in GetComponentsInChildren<SpriteRenderer>()) {
                    renderer.sharedMaterial = previewMaterial;
                    renderer.sortingLayerName = "Preview";
                }

                spriteRotate = GetComponentInChildren<SpriteRotate>();

                if(spriteRotate)
                    spriteRotate.enabled = true;
            }
        }
    }
}
#endif