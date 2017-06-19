using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    [System.Serializable]
    public class VLSLayer
    {
        [SerializeField]
        public string name = "Layer 1";

        [SerializeField]
        public LayerMask layerMask = 0;
               
        [HideInInspector]
        public RenderTexture renderTexture = null;
    }
}