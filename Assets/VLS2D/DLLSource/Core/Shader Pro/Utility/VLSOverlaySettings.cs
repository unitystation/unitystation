using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    [System.Serializable]
    public class VLSOverlaySettings
    {
        [SerializeField]
        public bool enabled = false;
        [SerializeField]
        public Texture2D texture = null;
        [SerializeField]
        public float xScrollSpeed = 0;
        [SerializeField]
        public float yScrollSpeed = 0;
        [SerializeField]
        public float scale = 1;
        [SerializeField]
        public float intensity = 1;
    }
}