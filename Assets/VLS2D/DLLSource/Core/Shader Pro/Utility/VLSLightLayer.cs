using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    [System.Serializable]
    public class VLSLightLayer : VLSLayer
    {
        [SerializeField]
        public VLSBlurSettings blur = new VLSBlurSettings();
        [SerializeField]
        public VLSOverlaySettings overlay = new VLSOverlaySettings();
    }
}