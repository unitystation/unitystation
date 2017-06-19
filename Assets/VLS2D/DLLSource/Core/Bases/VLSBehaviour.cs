using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PicoGames.VLS2D
{
    [ExecuteInEditMode]
    public abstract class VLSBehaviour : MonoBehaviour
    {
        private static int Id = 0;
        public static bool SHOW_NORMALS = false;

        [SerializeField, HideInInspector]
        public Rect bounds = new Rect(0, 0, 1, 1);
        [SerializeField, HideInInspector]
        public List<VLSEdge> edges = new List<VLSEdge>();

        [SerializeField, HideInInspector]
        protected bool isActive = true;
        [SerializeField, HideInInspector]
        protected List<Vector3> localVertices = new List<Vector3>();
        [SerializeField, HideInInspector]
        protected bool isDirty = false;
        [SerializeField, HideInInspector]
        private bool verticesDirty = true;
        private int id = 0;

        public int ID { get { return id; } }
        public int VertexCount { get { return localVertices.Count; } }
        public bool IsActive { get { return isActive; } }

        #region Debug Methods
        protected virtual void DebugShape(Color _color, bool _force = false)
        {
            if (!_force && !VLSDebug.IsModeActive(VLSDebugMode.Geometry))
                return;

            Gizmos.color = (isActive) ? _color : Color.gray;
            for (int i = 0; i < edges.Count; i++)
                Gizmos.DrawLine(edges[i].PointA.position, edges[i].PointB.position);            
        }

        protected virtual void DebugNormals(Color _color)
        {
            if (!VLSDebug.IsModeActive(VLSDebugMode.Geometry))
                return;

#if UNITY_EDITOR
            Gizmos.color = _color;
            for (int i = 0; i < edges.Count; i++)
                Gizmos.DrawRay((edges[i].PointA.position + edges[i].PointB.position) * 0.5f, edges[i].Normal * UnityEditor.HandleUtility.GetHandleSize(transform.position) * 0.5f);
#endif
        }

        protected virtual void DebugBounds(Color _color)
        {
            if (!VLSDebug.IsModeActive(VLSDebugMode.Bounds))
                return;

            Gizmos.color = (isActive) ? _color : Color.gray;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
        #endregion

        public virtual void Active(bool _isActive)
        {
            isActive = _isActive;
        }

        public virtual void SetDirty()
        {
            isDirty = true;
        }
        public virtual void SetVerticesDirty()
        {
            verticesDirty = true;
            transform.hasChanged = true;
        }

        public void LocalVertex(int _index, Vector3 _position)
        {
            if(_index >= localVertices.Count)
            {
                localVertices.Add(_position);
                verticesDirty = true;
                return;
            }

            localVertices[_index] = _position;
            verticesDirty = true;
        }
        public Vector3 LocalVertex(int _index)
        {
            _index = (_index < localVertices.Count) ? _index : localVertices.Count - 1;
            return localVertices[_index];
        }
        public void InsertLocalVertex(int _index, Vector2 _position)
        {
            localVertices.Insert(_index, _position);
            verticesDirty = true;
        }
        public void RemoveLocalVertex(int _index)
        {
            localVertices.RemoveAt(_index);
            verticesDirty = true;
        }

        public void ClearLocalVertices()
        {
            localVertices.Clear();
            verticesDirty = true;
        }
        public void GenerateDefaultVertices()
        {
            ClearLocalVertices();
            
            LocalVertex(500, new Vector3(0.5f, 0.5f, 0));
            LocalVertex(500, new Vector3(-0.5f, 0.5f, 0));
            LocalVertex(500, new Vector3(-0.5f, -0.5f, 0));
            LocalVertex(500, new Vector3(0.5f, -0.5f, 0));
            
            isDirty = true;
        }
        public void ReverseNormals()
        {
            localVertices.Reverse();
            verticesDirty = true;
        }

        protected virtual void Awake()
        {
            id = Id++;
            VLSViewer.Exists();
        }

        protected virtual void Update()
        {
            if (verticesDirty || transform.hasChanged)
            {
                Active(VLSViewer.IsInView(bounds));
                isDirty = true;

                GlobalizeVertices();
                RefreshRectBounds();
            }

            verticesDirty = false;
        }

        protected virtual void LateUpdate()
        {
            transform.hasChanged = false;
        }
                
        private void GlobalizeVertices()
        {
            if (edges.Count != localVertices.Count)
            {
                edges.Clear();
                for (int i = 0; i < localVertices.Count; i++)
                    edges.Add(new VLSEdge(this, transform.TransformPoint(localVertices[i]), transform.TransformPoint(localVertices[(i + 1) % localVertices.Count])));
            }
            else
            {
                for (int i = 0; i < localVertices.Count; i++)
                {
                    edges[i].PointA.position = transform.TransformPoint(localVertices[i]);
                    edges[i].PointB.position = transform.TransformPoint(localVertices[(i + 1) % localVertices.Count]);
                    edges[i].SetDirty();
                }
            }
        }

        private void RefreshRectBounds()
        {
            Vector2 min = Vector2.one * Mathf.Infinity;
            Vector2 max = Vector2.one * Mathf.NegativeInfinity;
            for (int i = 0; i < edges.Count; i++)
            {
                min = Vector2.Min(min, edges[i].PointA.position);
                min = Vector2.Min(min, edges[i].PointB.position);

                max = Vector2.Max(max, edges[i].PointA.position);
                max = Vector2.Max(max, edges[i].PointB.position);
            }

            bounds.Set(min.x, min.y, max.x - min.x, max.y - min.y);
            bounds.center = (bounds.min + bounds.max) * 0.5f;
        }

        // Hacky check to see if verts are going counter-clockwise. Might not work in all cases?
        public bool VertsAreCounterClockwise()
        {
            float tendency = 0;
            for (int i = 0; i < localVertices.Count; i++)
                tendency += (localVertices[(i + 1) % localVertices.Count].x - localVertices[i].x) * (localVertices[(i + 1) % localVertices.Count].y + localVertices[i].y); //(x2-x1)(y2+y1)

            return (tendency < 0);
        }
    }
}