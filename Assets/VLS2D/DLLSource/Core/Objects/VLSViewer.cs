using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PicoGames.VLS2D
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class VLSViewer : MonoBehaviour
    {
        //public static bool isQuiting = false;
        
        [SerializeField]
        private static VLSViewer instance;
        public static VLSViewer Instance 
        {
            get 
            {
                if (instance == null)
                {
                    instance = GameObject.FindObjectOfType<VLSViewer>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("VLS Viewer");
                        instance = go.AddComponent<VLSViewer>();
                    }
                }

                return instance; 
            }
        }

        public int boundTolerance = 5;

        public static List<VLSLight> VisibleLights = new List<VLSLight>();
        public static List<VLSObstructor> VisibleObstructions = new List<VLSObstructor>();

        public static Camera[] cameraList = new Camera[0];
        public static Rect[] cameraBoundList;
        public static Vector2[] cameraPrevPositions;

        public static List<VLSLight> AllLightsInScene = new List<VLSLight>();
        public static List<VLSObstructor> AllObstructionsInScene = new List<VLSObstructor>();

        public static Rect GetCameraBounds(Camera _camera, float _tolerance = 5)
        {
            Rect _rct = new Rect(0, 0, 0, 0);

            Vector3 upperLeft = _camera.ViewportToWorldPoint(new Vector3(0, 0, _camera.farClipPlane));
            Vector3 lowerRight = _camera.ViewportToWorldPoint(new Vector3(1, 1, _camera.farClipPlane));
            _rct.Set(upperLeft.x - _tolerance, upperLeft.y - _tolerance, lowerRight.x - upperLeft.x + _tolerance * 2, lowerRight.y - upperLeft.y + _tolerance * 2);

            return _rct;
        }

        public static void AddObstructor(VLSObstructor _obstructor)
        {
            AllObstructionsInScene.Add(_obstructor);

            if (IsInView(_obstructor.bounds))
                VisibleObstructions.Add(_obstructor);
        }
        public static void RemoveObstructor(VLSObstructor _obstructor)
        {
            AllObstructionsInScene.Remove(_obstructor);

            if (VisibleObstructions.Contains(_obstructor))
                VisibleObstructions.Remove(_obstructor);
        }

        public static void AddVLSLight(VLSLight _light)
        {
            AllLightsInScene.Add(_light);

            if (IsInView(_light.bounds))
                VisibleLights.Add(_light);
        }
        public static void RemoveVLSLight(VLSLight _light)
        {
            AllLightsInScene.Remove(_light);

            if (VisibleLights.Contains(_light))
                VisibleLights.Remove(_light);
        }

        public static bool IsInView(Rect _bound)
        {
            if (!Application.isPlaying)
                return true;

            if(cameraList.Length == 0)
                VLSViewer.CheckForNewCameras();

            for (int i = 0; i < cameraBoundList.Length; i++)
            {
                if (cameraBoundList[i].Overlaps(_bound))
                    return true;
            }
            
            return false;
        }

        public static void UpdateAll()
        {
            CheckForNewCameras();

            for (int i = 0; i < cameraList.Length; i++)
            {
                if (cameraList[i] == null)
                    continue;

                if (Vector2.SqrMagnitude((Vector2)cameraList[i].transform.position - cameraPrevPositions[i]) < (Instance.boundTolerance * Instance.boundTolerance))
                    continue;

                UpdateView(cameraList[i], ref cameraBoundList[i], ref cameraPrevPositions[i]);
                cameraList[i].transform.hasChanged = false;
            }
        }
        
        private static void CheckForNewCameras()
        {
            if (cameraList.Length != Camera.allCameras.Length)
            {
                cameraList = new Camera[Camera.allCameras.Length];
                cameraBoundList = new Rect[cameraList.Length];
                cameraPrevPositions = new Vector2[cameraList.Length];

                for (int i = 0; i < cameraList.Length; i++)
                {
                    cameraList[i] = Camera.allCameras[i];
                    cameraBoundList[i] = new Rect();
                    cameraPrevPositions[i] = new Vector2();
                    UpdateView(cameraList[i], ref cameraBoundList[i], ref cameraPrevPositions[i]);
                }
            }
        }

        private static void UpdateView(Camera _camera, ref Rect _cameraBounds, ref Vector2 _pPos)
        {
            _pPos = _camera.transform.position;
            _cameraBounds = GetCameraBounds(_camera, (Instance == null) ? 5 : Instance.boundTolerance);

            VisibleObstructions.Clear();
            for (int o = 0; o < AllObstructionsInScene.Count; o++)
            {
                if (IsInView(AllObstructionsInScene[o].bounds))
                {
                    AllObstructionsInScene[o].Active(true);
                    VisibleObstructions.Add(AllObstructionsInScene[o]);
                }
                else
                {
                    AllObstructionsInScene[o].Active(false);
                }
            }

            VisibleLights.Clear();
            for (int l = 0; l < AllLightsInScene.Count; l++)
            {
                if (IsInView(AllLightsInScene[l].bounds))
                {
                    AllLightsInScene[l].Active(true);
                    VisibleLights.Add(AllLightsInScene[l]);
                }
                else
                {
                    AllLightsInScene[l].Active(false);
                }
            }
        }

        public static void Exists()
        {
            if (instance == null)
            {
                instance = GameObject.FindObjectOfType<VLSViewer>();
                if (instance == null)
                {
                    GameObject go = new GameObject("VLS Viewer");
                    instance = go.AddComponent<VLSViewer>();
                }

                VisibleLights.Clear();
                VisibleObstructions.Clear();
            }
        }
        
        //void OnGUI()
        //{
        //    GUILayout.Label("Lights In Scene: " + AllLightsInScene.Count + " - Active [" + VisibleLights.Count + "]");
        //    GUILayout.Label("Obsts In Scene:  " + AllObstructionsInScene.Count + " - Active [" + VisibleObstructions.Count + "]");
        //}

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.8f, 0.3f, 0.3f, 1f);
            for (int i = 0; i < cameraList.Length; i++)
            {
                Gizmos.DrawWireCube(cameraBoundList[i].center, cameraBoundList[i].size);
            }
        }

        void Awake()
        {
            instance = this;
        }

        void OnEnable()
        {
            VLSGlobals.LoadEditorPrefs();
        }

        void Update()
        {
            CheckForNewCameras();
            for (int i = 0; i < cameraList.Length; i++)
            {
                if (cameraList[i] == null)
                    continue;

                if (cameraList[i].transform.hasChanged)
                {
                    if (Vector2.SqrMagnitude((Vector2)cameraList[i].transform.position - cameraPrevPositions[i]) < (boundTolerance * boundTolerance))
                        continue;

                    UpdateView(cameraList[i], ref cameraBoundList[i], ref cameraPrevPositions[i]);
                    cameraList[i].transform.hasChanged = false;
                }
            }
        }

        void LateUpdate()
        {
            for (int l = 0; l < VisibleLights.Count; l++)
                VisibleLights[l].UpdateLight();
        }
    }
}