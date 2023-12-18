using System.Collections.Generic;
using UnityEngine;

namespace Core.Pathfinding
{
    /// <summary>
    /// Line path represents a path of line segments.
    /// </summary>
    [System.Serializable]
    public class LinePath
    {
        public Vector3[] nodes;

        public float MaxDist { get; private set; }

        [System.NonSerialized]
        public float[] distances;

        /* Indexer declaration. */
        public Vector3 this[int i]
        {
            get
            {
                return nodes[i];
            }

            set
            {
                nodes[i] = value;
            }
        }

        public int Length
        {
            get
            {
                return nodes.Length;
            }
        }

        public Vector3 EndNode
        {
            get
            {
                return nodes[nodes.Length - 1];
            }
        }

        public LinePath(Vector3[] nodes)
        {
            this.nodes = nodes;

            CalcDistances();
        }

        public LinePath(List<Vector3> nodes)
        {
            this.nodes = nodes.ToArray();

            CalcDistances();
        }

        /// <summary>
        /// Loops through the path's nodes and determines how far each node in
        /// the path is from the starting node.
        /// </summary>
        public void CalcDistances()
        {
            distances = new float[nodes.Length];
            distances[0] = 0;

            for (var i = 0; i < nodes.Length - 1; i++)
            {
                distances[i + 1] = distances[i] + Vector3.Distance(nodes[i], nodes[i + 1]);
            }

            MaxDist = distances[distances.Length - 1];
        }

        /// <summary>
        /// Draws the path in the scene view
        /// </summary>
        public void Draw()
        {
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                Debug.DrawLine(nodes[i], nodes[i + 1], Color.cyan, 0.0f, false);
            }
        }

        /// <summary>
        /// Gets the param for the closest point on the path given a position.
        /// </summary>
        public float GetParam(Vector3 position)
        {
            /* Find the first point in the closest line segment to the path */
            float closestDist = DistToSegment(position, nodes[0], nodes[1]);
            int closestSegment = 0;

            for (int i = 1; i < nodes.Length - 1; i++)
            {
                float dist = DistToSegment(position, nodes[i], nodes[i + 1]);

                if (dist <= closestDist)
                {
                    closestDist = dist;
                    closestSegment = i;
                }
            }

            float param = distances[closestSegment] + GetParamForSegment(position, nodes[closestSegment], nodes[closestSegment + 1]);

            return param;
        }

        /// <summary>
        /// Given a param it gets the position on the path.
        /// </summary>
        public Vector3 GetPosition(float param, bool pathLoop = false)
        {
            /* Make sure the param is not past the beginning or end of the path */
            if (param < 0)
            {
                param = (pathLoop) ? param + MaxDist : 0;
            }
            else if (param > MaxDist)
            {
                param = (pathLoop) ? param - MaxDist : MaxDist;
            }

            /* Find the first node that is farther than given param */
            int i = 0;
            for (; i < distances.Length; i++)
            {
                if (distances[i] > param)
                {
                    break;
                }
            }

            /* Convert it to the first node of the line segment that the param is in */
            if (i > distances.Length - 2)
            {
                i = distances.Length - 2;
            }
            else
            {
                i -= 1;
            }

            /* Get how far along the line segment the param is */
            float t = (param - distances[i]) / Vector3.Distance(nodes[i], nodes[i + 1]);

            /* Get the position of the param */
            return Vector3.Lerp(nodes[i], nodes[i + 1], t);
        }

        /// <summary>
        /// Gives the distance of a point to a line segment.
        /// </summary>
        /// <param name="p">p is the point</param>
        /// <param name="v">v is one point of the line segment</param>
        /// <param name="w">w is the other point of the line segment</param>
        static float DistToSegment(Vector3 p, Vector3 v, Vector3 w)
        {
            Vector3 vw = w - v;

            float l2 = Vector3.Dot(vw, vw);

            if (Mathf.Approximately(l2, 0))
            {
                return Vector3.Distance(p, v);
            }

            float t = Vector3.Dot(p - v, vw) / l2;

            if (t < 0)
            {
                return Vector3.Distance(p, v);
            }

            if (t > 1)
            {
                return Vector3.Distance(p, w);
            }

            Vector3 closestPoint = Vector3.Lerp(v, w, t);

            return Vector3.Distance(p, closestPoint);
        }

        /// <summary>
        /// Finds the param for the closest point on the segment vw given the point p.
        /// </summary>
        static float GetParamForSegment(Vector3 p, Vector3 v, Vector3 w)
        {
            Vector3 vw = w - v;

            float l2 = Vector3.Dot(vw, vw);

            if (Mathf.Approximately(l2, 0))
            {
                return 0;
            }

            float t = Vector3.Dot(p - v, vw) / l2;

            if (t < 0)
            {
                t = 0;
            }
            else if (t > 1)
            {
                t = 1;
            }

            return t * Mathf.Sqrt(l2);
        }

        public void RemoveNode(int i)
        {
            Vector3[] newNodes = new Vector3[nodes.Length - 1];

            int newNodesIndex = 0;
            for (int j = 0; j < newNodes.Length; j++)
            {
                if (j != i)
                {
                    newNodes[newNodesIndex] = nodes[j];
                    newNodesIndex++;
                }
            }

            nodes = newNodes;

            CalcDistances();
        }
    }
}