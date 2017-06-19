using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    [System.Serializable]
    public class VLSPoint : System.IComparable<VLSPoint>
    {
        [SerializeField]
        public Vector3 position = new Vector3(0, 0, 0);
        [SerializeField]
        public float angle = 0;

        public VLSPoint() { }

        public VLSPoint(Vector3 _position, float _angle)
        {
            this.position = _position;
            this.angle = _angle;
        }

        public int CompareTo(VLSPoint other)
        {
            if (this.angle > other.angle)
                return 1;
            else if (this.angle < other.angle)
                return -1;

            return 0;
        }
    }
}