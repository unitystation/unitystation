using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    public class VLSRayHit
    {
        public VLSObstructor obstructor = null;
        public Vector3 point = Vector3.zero;
        public Vector2 direction = Vector3.zero;
        public float sqrDist = Mathf.Infinity;
    }
}