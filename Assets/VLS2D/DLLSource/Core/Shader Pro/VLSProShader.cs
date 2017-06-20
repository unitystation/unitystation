/* Code Header
 * Code written by Jake Fletcher 
 * Pico Games 2015 - VLS2D 4.x
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PicoGames.VLS2D
{            
    [AddComponentMenu("VLS2D (2D Lights)/Camera/Pro Shader", -5), RequireComponent(typeof(Camera)), ExecuteInEditMode, DisallowMultipleComponent]
    public class VLSProShader : MonoBehaviour
    {
        #region Public Variables
        [SerializeField]
        public bool useAsUtility = false;
        
        [SerializeField]
        public VLSLightLayer[] lightPasses = new VLSLightLayer[] { new VLSLightLayer() };

        [SerializeField]
        public VLSPassLayer[] layerPasses = new VLSPassLayer[] { };

        [SerializeField]
        public VLSPassLayer defaultLayer = new VLSPassLayer();

        #endregion

        #region Private Variables
        private RenderTexture tempBuffer;
        private RenderTexture mainBuff;
        private GameObject RTCameraObject = null;
        private Camera RTCamera = null;
        private Camera mCamera = null;

        private int pixelWidth = 0;
        private int pixelHeight = 0;
        private bool isInitialized = false;
        #endregion

        #region Blur Material
        private static Material blurMaterial;
        private static Material GetBlurMaterial()
        {
            if (blurMaterial == null)
            {
                blurMaterial = new Material(Shader.Find("VLS2D/Blur"));
                blurMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return blurMaterial;
        }
        #endregion

        #region Composite Material
        private static Material compositeMaterial;
        private static Material GetCompositeMaterial()
        {
            if (compositeMaterial == null)
            {
                compositeMaterial = new Material(Shader.Find("VLS2D/Composite"));//compositeMatString);
                compositeMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return compositeMaterial;
        }
        #endregion
                
        #region Multiply Material
        private static Material multiplyMaterial;
        private static Material GetMultiplyMaterial()
        {
            if (multiplyMaterial == null)
            {
                multiplyMaterial = new Material(Shader.Find("VLS2D/Multiply"));
                multiplyMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return multiplyMaterial;
        }
        #endregion

        #region Overlay Material
        private static Material addOverlayMaterial;
        private static Material GetAddOverlayMaterial()
        {
            if (addOverlayMaterial == null)
            {
                addOverlayMaterial = new Material(Shader.Find("VLS2D/AddOverlay"));
                addOverlayMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return addOverlayMaterial;
        }
        #endregion

        #region Main Monobehaviour Functionality
        void OnDisable()
        {
            if (multiplyMaterial != null)
                DestroyImmediate(multiplyMaterial);
            if (addOverlayMaterial != null)
                DestroyImmediate(addOverlayMaterial);
            if (compositeMaterial != null)
                DestroyImmediate(compositeMaterial);
            if (blurMaterial != null)
                DestroyImmediate(blurMaterial);

            DestroyImmediate(RTCameraObject);
        }

        void OnEnable()
        {
            if (!SystemInfo.supportsImageEffects)
            {
                enabled = false;
                return;
            }

            mCamera = GetComponent<Camera>();
            mCamera.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 1f);

            defaultLayer.layerMask = mCamera.cullingMask;

            RTCameraObject = new GameObject("RTCamera", typeof(Camera));
            RTCameraObject.GetComponent<Camera>().enabled = false;
            RTCameraObject.hideFlags = HideFlags.HideAndDontSave;
        }

        void OnPreRender()
        {
            if (!enabled || !gameObject.activeSelf)
                return;

            if (RTCamera == null)
                RTCamera = RTCameraObject.GetComponent<Camera>();

            for (int i = 0; i < lightPasses.Length; i++)
            {
                if((mCamera.cullingMask & lightPasses[i].layerMask) != 0)
                    mCamera.cullingMask &= ~lightPasses[i].layerMask;
            }

            if(defaultLayer.layerMask != mCamera.cullingMask)
                defaultLayer.layerMask = mCamera.cullingMask;

            pixelWidth = (int)mCamera.pixelWidth;
            pixelHeight = (int)mCamera.pixelHeight;
            RTCamera.CopyFrom(mCamera);

            ReleasePassRenderTextures();
        }

        void OnRenderImage(RenderTexture _input, RenderTexture _output)
        {
            if (useAsUtility)
            {
                Graphics.Blit(_input, _output);
                return;
            }

            if (mCamera.targetTexture != null)
                RenderScene(_input, mCamera.targetTexture);
            else
                RenderScene(_input, _output);
        }

        void RenderScene(RenderTexture _source, RenderTexture _destination)
        {
            mainBuff = RenderTexture.GetTemporary(pixelWidth, pixelHeight);

            // Render Light Layers
            for (int i = 0; i < lightPasses.Length; i++)
                RenderLights(lightPasses[i]);

            for (int i = 0; i < layerPasses.Length; i++)
                RenderLayer(layerPasses[i]);

            BlitLightsToLayer(defaultLayer, _source, mainBuff);
            for (int i = 0; i < layerPasses.Length; i++)
                BlitLightsToLayer(layerPasses[i], layerPasses[i].renderTexture, mainBuff);

            Graphics.Blit(mainBuff, _destination);

            ReleasePassRenderTextures();
        }
        
        public void BlitLightPass(RenderTexture _texture, int _lightPass)
        {
            RenderLights(lightPasses[_lightPass]);            
            Graphics.Blit(lightPasses[_lightPass].renderTexture, _texture);
        }

        private Material tempCompMat = null;
        void BlitLightsToLayer(VLSPassLayer _layer, RenderTexture _input, RenderTexture _output)
        {
            tempCompMat = GetCompositeMaterial();// (_layer.overlay.enabled && _layer.overlay.texture != null) ? GetCompositeMaterialwithOverlay() : GetCompositeMaterial();

            tempBuffer = RenderTexture.GetTemporary(pixelWidth, pixelHeight);
            tempBuffer.Create();

            if (_layer.lightsEnabled || _layer.lightLayerMask == 0)
                for (int i = 0; i < lightPasses.Length; i++)
                    if (((int)Mathf.Pow(2, i) & _layer.lightLayerMask) != 0)
                        Graphics.Blit(lightPasses[i].renderTexture, tempBuffer, GetMultiplyMaterial());
            
            tempCompMat.SetTexture("_OverlayTex", tempBuffer);

            tempCompMat.SetFloat("_Intensity", _layer.lightIntensity);
            tempCompMat.SetColor("_AmbientColor", _layer.useSceneAmbientColor ? RenderSettings.ambientLight : _layer.ambientColor);

            if (_layer.blur.enabled)
                BlurRenderTexture(_input, _input, _layer.blur.iterations, _layer.blur.spread);
            
            Graphics.Blit(_input, _output, tempCompMat);

            ReleaseRenderTexture(ref tempBuffer);
        }
        #endregion
        
        #region Layer Renderers
        void RenderLights(VLSLightLayer _layer)
        {
            ReleaseRenderTexture(ref _layer.renderTexture);
            RenderTexture buffer = RenderTexture.GetTemporary(pixelWidth, pixelHeight);
            _layer.renderTexture = RenderTexture.GetTemporary(pixelWidth, pixelHeight);

            RTCamera.clearFlags = CameraClearFlags.Color;
            RTCamera.backgroundColor = Color.clear;

            RTCamera.cullingMask = _layer.layerMask;

            RTCamera.targetTexture = _layer.renderTexture;
            RTCamera.Render();

            if (_layer.blur.enabled && _layer.blur.iterations > 0)
            {
                BlurRenderTexture(_layer.renderTexture, buffer, _layer.blur.iterations, _layer.blur.spread);
            }
            else
            {
                Graphics.Blit(_layer.renderTexture, buffer);
            }

            if (_layer.overlay.enabled && _layer.overlay.texture != null)
            {
                GetAddOverlayMaterial().SetTexture("_OverlayTex", _layer.overlay.texture);
                GetAddOverlayMaterial().SetFloat("_Intensity", _layer.overlay.intensity);
                GetAddOverlayMaterial().SetFloat("_Scale", _layer.overlay.scale);
                GetAddOverlayMaterial().SetFloat("_XScrollSpeed", _layer.overlay.xScrollSpeed);
                GetAddOverlayMaterial().SetFloat("_YScrollSpeed", _layer.overlay.yScrollSpeed);
            }
            else
            {
                GetAddOverlayMaterial().SetTexture("_OverlayTex", null);
            }

            Graphics.Blit(buffer, _layer.renderTexture, GetAddOverlayMaterial());

            RenderTexture.ReleaseTemporary(buffer);
        }

        void RenderLayer(VLSPassLayer _layer)
        {
            ReleaseRenderTexture(ref _layer.renderTexture);
            _layer.renderTexture = RenderTexture.GetTemporary(pixelWidth, pixelHeight);

            for (int i = 0; i < lightPasses.Length; i++)
                RTCamera.cullingMask = (_layer.layerMask & ~lightPasses[i].layerMask);

            //defaultLayer.layerMask = RTCamera.cullingMask;

            RTCamera.clearFlags = CameraClearFlags.Color; //_layer.clearFlag;
            RTCamera.backgroundColor = Color.clear;// _layer.backgroundColor;

            RTCamera.targetTexture = _layer.renderTexture;
            RTCamera.Render();
        }
        #endregion

        #region Render Texture Helpers
        void ReleaseRenderTexture(ref RenderTexture _renderTexture)
        {
            if (_renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(_renderTexture);
                _renderTexture = null;
            }
        }

        void ReleasePassRenderTextures()
        {
            for (int i = 0; i < layerPasses.Length; i++)
                ReleaseRenderTexture(ref layerPasses[i].renderTexture);

            for (int i = 0; i < lightPasses.Length; i++)
                ReleaseRenderTexture(ref lightPasses[i].renderTexture);

            ReleaseRenderTexture(ref mainBuff);
            ReleaseRenderTexture(ref tempBuffer);
        }
        #endregion

        #region Blur Helpers
        private RenderTexture buffer, buffer2;
        private float off;
        private bool oddEven;
        private Vector2[] blitOffsets = new Vector2[4]
        {
            new Vector2(-1, -1),
            new Vector2(-1, 1),
            new Vector2(1, 1),
            new Vector2(1, -1)
        };

        void BlurRenderTexture(RenderTexture source, RenderTexture destination, int iterations, float spread)
        {
            buffer = RenderTexture.GetTemporary(source.width / 4, source.height / 4, 0);
            buffer2 = RenderTexture.GetTemporary(source.width / 4, source.height / 4, 0);

            // Copy source to the 4x4 smaller texture.
            DownSample4x(source, buffer);

            // Blur the small texture
            for (int i = 0; i < iterations; i++)
            {
                if ((i % 2) == 0)
                    FourTapCone(buffer, buffer2, i, spread);
                else
                    FourTapCone(buffer2, buffer, i, spread);
                oddEven = !oddEven;
            }
            if ((iterations % 2) == 0)
                Graphics.Blit(buffer, destination);
            else
                Graphics.Blit(buffer2, destination);

            RenderTexture.ReleaseTemporary(buffer);
            RenderTexture.ReleaseTemporary(buffer2);
        }

        // Performs one blur iteration.
        public void FourTapCone(RenderTexture source, RenderTexture dest, int iteration, float spread)
        {
            off = 0.5f + iteration * spread;
            Graphics.BlitMultiTap(source, dest, GetBlurMaterial(),
                    blitOffsets[0] * off,
                    blitOffsets[1] * off,
                    blitOffsets[2] * off,
                    blitOffsets[3] * off
            );
        }

        // Downsamples the texture to a quarter resolution.
        private void DownSample4x(RenderTexture source, RenderTexture dest)
        {
            off = 1.0f;
            Graphics.BlitMultiTap(source, dest, GetBlurMaterial(),
                    blitOffsets[0],
                    blitOffsets[1],
                    blitOffsets[2],
                    blitOffsets[3]
            );
        }
        #endregion
    }
}