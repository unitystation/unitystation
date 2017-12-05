using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lighting
{
    public class LightingSourceManager : MonoBehaviour
    {

        public Dictionary<Vector2, LightSource> lights = new Dictionary<Vector2, LightSource>();
        private LightingRoom lightingRoomParent;

        void Awake()
        {
            lightingRoomParent = GetComponentInParent<LightingRoom>();
        }

        void Start()
        {
            LoadAllLights();
        }

        void LoadAllLights()
        {
            foreach (Transform child in transform)
            {
                LightSource source = child.gameObject.GetComponent<LightSource>();
                if (source != null)
                {
                    lights.Add(child.transform.position, source);
                }
                else
                {
                    Debug.LogError("No LightSource component found!");
                }

            }
        }

        public void UpdateRoomBrightness(LightSource theSource)
        {

        }
    }
}
