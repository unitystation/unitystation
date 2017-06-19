using UnityEngine;
using System.Collections;

namespace PicoGames.VLS2D
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class VLSBasicShader : MonoBehaviour
    {
        public Color ambientColor = new Color(0.3f, 0.3f, 0.35f, 0.8f);

        private GameObject overlayPlane;
        private GameObject GetPlane()
        {
            if(overlayPlane == null)
                CreatePlane();
            
            return overlayPlane;
        }

        private MeshRenderer overlayRenderer;
        private MeshRenderer GetOverlayRenderer()
        {
            if(overlayRenderer == null)
                overlayRenderer = GetPlane().GetComponent<MeshRenderer>();

            return overlayRenderer;
        }

        private static Material maskMaterial;
        private static Material GetMaskMaterial()
        {
            if(maskMaterial == null)
            {
                maskMaterial = new Material(Shader.Find("VLS2D/DepthMask"));
                maskMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return maskMaterial;
        }
        
        void OnEnable()
        {
            CreatePlane();
        }

        private void CreatePlane()
        {
            if (overlayPlane == null)
            {
                Camera thisCam = GetComponent<Camera>();

                overlayPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                //overlayPlane.hideFlags = HideFlags.DontSave;

                overlayPlane.transform.parent = transform;
                overlayPlane.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + thisCam.nearClipPlane * 1.01f);

                Vector3 upperLeft = thisCam.ViewportToWorldPoint(new Vector3(0, 0, thisCam.farClipPlane));
                Vector3 lowerRight = thisCam.ViewportToWorldPoint(new Vector3(1, 1, thisCam.farClipPlane));

                overlayPlane.transform.localScale = new Vector3(lowerRight.x - upperLeft.x, lowerRight.y - upperLeft.y, 0);

                GetMaskMaterial().SetColor("_Color", ambientColor);

                overlayRenderer = overlayPlane.GetComponent<MeshRenderer>();
                overlayRenderer.material = GetMaskMaterial();
            }
        }

        private void ResizePlane()
        {

        }

        void Update()
        {
            GetOverlayRenderer().material.renderQueue = 3020;
            GetOverlayRenderer().material.SetColor("_Color", ambientColor);
        }
    }
}