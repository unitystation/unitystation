using UnityEngine;

interface ITextureRenderer
{
	PixelPerfectRTP Render(Camera iCameraToMatch, PixelPerfectRTParameter iPPRTParameter);
}