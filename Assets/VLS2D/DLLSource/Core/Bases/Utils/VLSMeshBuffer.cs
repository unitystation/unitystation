using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace PicoGames.VLS2D
{
    [System.Serializable]
    public class VLSMeshBuffer
    {
        [SerializeField, HideInInspector]
        private Mesh mesh;
        [SerializeField, HideInInspector]
        public List<VLSPoint> vertices = new List<VLSPoint>();

        [SerializeField, HideInInspector]
        public int[] triangles = new int[0];
        [SerializeField, HideInInspector]
        public Vector3[] normals = new Vector3[0];
        [SerializeField, HideInInspector]
        public Vector2[] uvs = new Vector2[0];
        [SerializeField, HideInInspector]
        public Color32[] colors = new Color32[0];

        [SerializeField, HideInInspector]
        private MeshFilter filter;

        private int vertexCount = 0;
        public int VertexCount { get { return vertexCount; } }

        public Mesh Mesh
        {
            get
            {
                if (mesh == null)
                {
                    mesh = new Mesh();
                    mesh.hideFlags = HideFlags.DontSave;
                }

                return mesh;
            }
        }
        
        public VLSMeshBuffer(MeshFilter _filter)
        {
            this.filter = _filter;
        }

        public void Clear()
        {
            for (int i = 0; i < vertices.Count; i++)
                vertices[i].angle = Mathf.Infinity;

            vertexCount = 0;
            Mesh.Clear();
        }

        public void AddPoint(Vector3 _position, float _angle)
        {
            _position.Set(_position.x, _position.y, 0);
            if (vertexCount >= vertices.Count)
            {
                vertices.Add(new VLSPoint(_position, _angle));
            }
            else
            {
                vertices[vertexCount].position = _position;
                vertices[vertexCount].angle = _angle;
            }
            vertexCount++;
        }

        public void InsertPoint(int _index, Vector2 _position, float _angle)
        {
            if (vertexCount >= vertices.Count)
            {
                vertices.Add(new VLSPoint(_position, _angle));
            }
            else
            {
                vertices[vertexCount].position = _position;
                vertices[vertexCount].angle = _angle;
            }
            vertexCount++;
        }

        private Vector3[] tmpVerts = new Vector3[0];
        public void Apply()
        {
            Mesh.Clear();
            Mesh.MarkDynamic();

            if (tmpVerts.Length != vertexCount)
                tmpVerts = new Vector3[vertexCount];

            for (int i = 0; i < vertexCount; i++)
                tmpVerts[i] = vertices[i].position;

            Mesh.vertices = tmpVerts;

            Mesh.triangles = triangles;
            Mesh.normals = normals;
            Mesh.uv = uvs;
            UpdateColors();

            Mesh.RecalculateBounds();

            filter.sharedMesh = Mesh;
        }

        public void UpdateColors()
        {
            Mesh.colors32 = colors;
            filter.sharedMesh = Mesh;
        }
    }
}