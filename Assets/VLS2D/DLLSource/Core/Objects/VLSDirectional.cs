using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    public class VLSDirectional : VLSLight
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            edges.Reverse();
        }

        public override void UpdateVertices()
        {
            VLSUtility.GenerateDirectionalMesh(this, shadowLayer);
        }

        public override void UpdateUVs()
        {
            //throw new System.NotImplementedException();
        }

        public override void UpdateTriangles()
        {
            //throw new System.NotImplementedException();
        }
    }
}