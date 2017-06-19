using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    [System.Serializable]
    public class VLSEdge : System.IComparable<VLSEdge>
    {
        [SerializeField]
        private VLSBehaviour parent = null;
        [SerializeField]
        private VLSPoint pointA = new VLSPoint();
        [SerializeField]
        private VLSPoint pointB = new VLSPoint();
        [SerializeField]
        private Vector3 normal = new Vector3(0, 0);
        [SerializeField]
        private Vector3 direction = new Vector3(0, 0);
        [SerializeField]
        private bool flagNormalUpdate = false;
        [SerializeField]
        private bool isEnd = false;
        [SerializeField]
        private bool isStart = false;

        public VLSBehaviour Parent
        {
            get { return parent; }
        }

        public VLSPoint PointA
        {
            get { return pointA; }
            set { pointA = value; flagNormalUpdate = true; }
        }

        public VLSPoint PointB
        {
            get { return pointB; }
            set { pointB = value; flagNormalUpdate = true; }
        }

        public Vector3 Normal
        {
            get 
            {
                if (flagNormalUpdate)
                    CalculateNormal();

                return normal; 
            }
        }

        public Vector3 Direction
        {
            get
            {
                if (flagNormalUpdate)
                    CalculateNormal();

                return direction;
            }
        }

        public bool IsEnd
        {
            get { return isEnd; }
            set { isEnd = value; }
        }

        public bool IsStart
        {
            get { return isStart; }
            set { isStart = value; }
        }

        public void SetDirty()
        {
            flagNormalUpdate = true;
        }

        public VLSEdge(VLSBehaviour _parent, Vector3 _pointA, Vector3 _pointB)
        {
            pointA.position.Set(_pointA.x, _pointA.y, 0);
            pointB.position.Set(_pointB.x, _pointB.y, 0);

            CalculateNormal();
        }

        private void CalculateNormal()
        {
            Vector2 delta = (pointB.position - pointA.position).normalized;
            normal.Set(delta.y, -delta.x, 0);
            direction.Set(delta.x, delta.y, 0);
            flagNormalUpdate = false;
        }

        public int CompareTo(VLSEdge other)
        {
            if (this.pointA.angle > other.pointA.angle)
                return 1;
            else if (this.pointA.angle < other.pointA.angle)
                return -1;

            return 0;
        }
    }
}