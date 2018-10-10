using UnityEngine;

interface ITextureRenderer
{
	PixelPerfectRT Render(Camera iCameraToMatch, PixelPerfectRTParameter iPPRTParameter, RenderSettings iRenderSettings);
}