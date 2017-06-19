using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    [System.Serializable]
    public class VLSBlurSettings
    {
        [SerializeField]
        public bool enabled = false;

        [SerializeField, Range(1, 4)]
        public int iterations = 3;

        [SerializeField, Range(0, 4)]
        public float spread = 0.5f;
    }
}