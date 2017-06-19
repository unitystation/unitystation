using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    [AddComponentMenu("VLS2D (2D Lights)/Lights/Radial Light"), ExecuteInEditMode]
    public class VLSRadial : VLSLight
    {
        public int EdgeCount { get { return edgeCount; } set { edgeCount = value; SetDirty(); } }

        [SerializeField, HideInInspector]
        private int edgeCount = 16;
        [SerializeField, HideInInspector, Range(1, 360)]
        private int coneAngle = 360;


        protected override void OnEnable()
        {
            RecalculateVerts();
            base.OnEnable();
        }

        protected override void Reset()
        {
            RecalculateVerts();
        }

        public void RecalculateVerts()
        {
            isDirty = true;
            edgeCount = (int)Mathf.Clamp(edgeCount, 4, 64);

            float angleDivTwo = (360 - coneAngle) * 0.5f;
            float coneStart = angleDivTwo;
            float coneEnd = (360 - angleDivTwo);
            float spacing = (coneEnd - coneStart) / (float)edgeCount;

            ClearLocalVertices();
            for (int i = 0; i <= edgeCount; i++)
            {
                float rad = (coneStart + (i * spacing)) * Mathf.Deg2Rad;
                LocalVertex(5000, new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0));
            }

            if(coneAngle != 360)
                LocalVertex(5000, new Vector3(0, 0.05f * ((float)coneAngle / 360f), 0));
        }

        public override void UpdateVertices()
        {
            VLSUtility.GenerateRadialMesh(this, shadowLayer);
        }

        public override void UpdateUVs()
        {
            if (buffer.uvs.Length != buffer.VertexCount)
                buffer.uvs = new Vector2[buffer.VertexCount];

            for (int i = 0; i < buffer.VertexCount; i++)
            {
                uv.Set((buffer.vertices[i].position.x) / 2, (buffer.vertices[i].position.y) / 2);
                uv = transform.rotation * uv;
                buffer.uvs[i].Set(uv.x + 0.5f, uv.y + 0.5f);
            }
        }

        private Vector2 uv;
        private int index = 0;
        private int vIndex = 0;
        public override void UpdateTriangles()
        {
            if (buffer.triangles.Length != (buffer.VertexCount * 3))
                buffer.triangles = new int[buffer.VertexCount * 3];

            index = 0;
            for (int i = 0; i < (buffer.VertexCount - 1); i++)
            {
                buffer.triangles[index++] = (buffer.VertexCount - 1);

                vIndex = ((i + 1) % (buffer.VertexCount - 1));
                buffer.triangles[index++] = (vIndex == (buffer.VertexCount - 1) ? 1 : vIndex);

                buffer.triangles[index++] = i;
            }
        }
    }
}