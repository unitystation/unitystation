using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public class TextureDataRequest
{
	private bool iRequestLoaded;
	private int height;
	private int width;
	NativeArray<Color32> colors;

	public void DeallocateOnClose()
	{
		colors.Dispose();
	}

	public void StoreData(AsyncGPUReadbackRequest iRequest)
	{
		if (colors.Length > 0)
		{
			colors.Dispose();
		}
		iRequestLoaded = true;
		colors = new NativeArray<Color32>(iRequest.GetData<Color32>(), Allocator.Persistent);
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
		oColor32 = colors[_index];
		return true;
	}
}