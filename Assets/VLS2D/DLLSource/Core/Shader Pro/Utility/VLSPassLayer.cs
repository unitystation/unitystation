using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    [System.Serializable]
    public class VLSPassLayer : VLSLayer
    {
        [SerializeField]
        public VLSBlurSettings blur = new VLSBlurSettings();

        [SerializeField]
        public bool lightsEnabled = true;

        [SerializeField]
        public int lightLayerMask = 0;
        
        [SerializeField]
        public float lightIntensity = 1f;

        [SerializeField]
        public bool useSceneAmbientColor;

        [SerializeField]
        public Color ambientColor = new Color(0.2f, 0.2f, 0.22f, 1f);

        //[SerializeField]
        //public CameraClearFlags clearFlag;
        //[SerializeField]
        //public Color backgroundColor;
    }
}