using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    [AddComponentMenu("VLS2D (2D Lights)/Lights/Radial Light (Custom Shape)"), ExecuteInEditMode]
    public class VLSRadialCS : VLSLight
    {
        protected override void OnDrawGizmosSelected()
        {
            if (SHOW_NORMALS)
                DebugNormals(new Color(0.8f, 0.6f, 0.3f, 1f));

            base.OnDrawGizmosSelected();
        }

        protected override void OnEnable()
        {
            if (localVertices.Count == 0)
                GenerateDefaultVertices();

            base.OnEnable();

            if (VertsAreCounterClockwise())
                ReverseNormals();
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