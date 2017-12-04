using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace MapEditor
{
    [ExecuteInEditMode]
    public class PreviewObject : MonoBehaviour
    {
#if UNITY_EDITOR
        private static PreviewObject instance;
        public static PreviewObject Instance
        {
            get
            {
                if (!instance)
                {
                    GameObject instanceTemp = GameObject.FindGameObjectWithTag("MapEditor");
                    if (instanceTemp != null)
                    {
                        instance = instanceTemp.GetComponentInChildren<PreviewObject>(true);
                    }
                    else
                    {
                        instance = null;
                    }
                }

                return instance;
            }
        }

        public static bool ShowPreview { get; set; }
        private static SceneView currentSceneView;

        private static GameObject prefab;
        public static GameObject Prefab
        {
            get
            {
                return prefab;
            }
            set
            {
                if (prefab != value)
                {
                    prefab = value;

                    if (Instance)
                        Instance.UpdatePrefab();

                    if (prefab && currentSceneView)
                    {
                        currentSceneView.Focus();
                    }
                }
            }
        }

        public Material previewMaterial;

        private SpriteRotate[] spriteRotates;
        private BoxCollider2D boxCollider2D;

        public static void Update(SceneView sceneView)
        {
            SetActive(ShowPreview);
            currentSceneView = sceneView;
            if (Instance != null)
            {
                Instance.FollowMouse(Event.current);
                Instance.RemoveFromSelection();
            }
        }

        public static GameObject CreateGameObject()
        {
            var gameObject = (GameObject)PrefabUtility.InstantiatePrefab(Prefab);

            var spriteRotates = gameObject.GetComponentsInChildren<SpriteRotate>();
            for (int i = 0; i < spriteRotates.Length; i++)
            {
                spriteRotates[i].RotateIndex = Instance.spriteRotates[i].RotateIndex;
            }

            if (Instance.boxCollider2D)
            {
                var boxCollider2D = gameObject.GetComponentInChildren<BoxCollider2D>();
                boxCollider2D.size = Instance.boxCollider2D.size;
                boxCollider2D.offset = Instance.boxCollider2D.offset;
            }

            return gameObject;
        }

        public static void RotateForwards()
        {
            if (Instance && Instance.spriteRotates.Length > 0)
            {
                foreach (var spriteRotate in Instance.spriteRotates)
                {
                    spriteRotate.RotateForwards();
                }
            }

            Instance.UpdateCollider();
        }

        public static void RotateBackwards()
        {
            if (Instance && Instance.spriteRotates.Length > 0)
            {
                foreach (var spriteRotate in Instance.spriteRotates)
                {
                    spriteRotate.RotateBackwards();
                }
            }

            Instance.UpdateCollider();
        }

        private void UpdateCollider()
        {
            if (boxCollider2D)
            {
                var position = Instance.spriteRotates[0].transform.localPosition;
                position = new Vector3(Mathf.Round(position.x), Mathf.Round(position.y));
                boxCollider2D.offset = position;
                boxCollider2D.size = Vector2.one;
            }
        }

        private void FollowMouse(Event e)
        {
            Ray r = Camera.current.ScreenPointToRay(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));

            int x = Mathf.RoundToInt(r.origin.x);
            int y = Mathf.RoundToInt(r.origin.y);

            transform.position = new Vector3(x, y, 0);
        }

        private void RemoveFromSelection()
        {
            if (Selection.Contains(gameObject))
            {
                Selection.objects = Array.FindAll(Selection.objects, o => (o != gameObject));
            }
            RemoveSelectionChildren(transform);
        }

        private void RemoveSelectionChildren(Transform parent)
        {
            foreach (Transform child in parent)
            {
                RemoveSelectionChildren(child);

                if (Selection.Contains(child.gameObject))
                {
                    Selection.objects = Array.FindAll(Selection.objects, o => (o != child.gameObject));
                }
            }
        }

        public static void SetActive(bool active)
        {
            if (Instance && Instance.gameObject.activeInHierarchy != active)
            {
                Instance.gameObject.SetActive(active);

                if (active)
                {
                    if (prefab && currentSceneView)
                    {
                        currentSceneView.Focus();
                    }
                }
                else
                {
                    Prefab = null;
                }
            }
        }

        private void UpdatePrefab()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            if (prefab)
            {
                var p = Instantiate(prefab);
                foreach (var script in p.GetComponentsInChildren<MonoBehaviour>())
                {
                    script.enabled = false;
                }

                p.transform.parent = transform;
                p.transform.localPosition = Vector3.zero;

                foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
                {
                    renderer.sharedMaterial = previewMaterial;
                    renderer.sortingLayerName = "Preview";
                }

                spriteRotates = GetComponentsInChildren<SpriteRotate>();
                boxCollider2D = GetComponentInChildren<BoxCollider2D>();

                if (spriteRotates.Length > 0)
                {
                    foreach (var spriteRotate in spriteRotates)
                    {
                        spriteRotate.enabled = true;
                        spriteRotate.RotateIndex = 0;
                    }
                }
            }
        }
#endif
    }
}
