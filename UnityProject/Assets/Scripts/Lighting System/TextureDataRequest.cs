using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class TextureDataRequest
{
	private bool iRequestLoaded;
	private List<Color32> textureBuffer = new List<Color32>();
	private int height;
	private int width;

	public void StoreData(AsyncGPUReadbackRequest iRequest)
	{
		iRequestLoaded = true;
		textureBuffer.Clear();
		foreach(Color32 C in iRequest.GetData<Color32>()){
			textureBuffer.Add(C);
		}
		height = iRequest.height;
		width = iRequest.width;
	}

	public bool TryGetPixelNormalized(float iNormalizedX, float iNormalizedY, out Color32 oColor32)
	{
		var _width = (int)(iNormalizedX * width);
		var _height = (int)(iNormalizedY * height);
		return TryGetPixel(_width, _height, out oColor32);
	}

	public bool TryGetPixel(int iX, int iY, out Color32 oColor32)
	{
		oColor32 = default(Color32);

		if (iRequestLoaded == false ||
		    iX < 0 ||
		    iX > width ||
		    iY < 0 ||
		    iY > height)
			return false;

		int _index = (iY * width) + iX;
		oColor32 = textureBuffer[_index];

		return true;
	}
}