using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Lighting
{
    public class LightingRoom : MonoBehaviour
    {
        public LightingTileManager tileManager { get; set; }
        public LightingSourceManager sourceManager { get; set; }
        /// <summary>
        /// x = left
        /// y = top
        /// z = right
        /// w = down
        /// </summary>
        /// <value>The bounds of the LightTile Group in the room.</value>
        public Vector4 bounds
        {
            get
            {
                if (tileManager != null)
                {
                    return tileManager.bounds;
                }
                else
                {
                    return Vector4.zero;
                }

            }
        }

        void Awake()
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.name == "Tiles")
                {
                    child.gameObject.SetActive(true);
                    tileManager = child.gameObject.GetComponent<LightingTileManager>();
                }
            }
            sourceManager = GetComponentInChildren<LightingSourceManager>();
        }

        void Start()
        {
            Invoke("PrintBounds", 1f);
        }

        void PrintBounds()
        {
            Debug.Log("LIGHTING: Bounds calc for " + gameObject.name + ": " + bounds);
        }

        public void LightSwitchOff()
        {
            foreach (KeyValuePair<Vector2, LightSource> light in sourceManager.lights)
            {
                light.Value.Trigger(false);

            }
        }

        public void LightSwitchOn()
        {
            foreach (KeyValuePair<Vector2, LightSource> light in sourceManager.lights)
            {
                light.Value.Trigger(true);
            }
        }
    }
}
