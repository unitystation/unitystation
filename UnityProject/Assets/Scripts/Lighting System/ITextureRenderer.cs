using UnityEngine;

interface ITextureRenderer
{
	void ResetRenderingTextures(OperationParameters iParameters);

	PixelPerfectRTP Render(Camera iCameraToMatch, PixelPerfectRTParameter iPPRTParameter);
}