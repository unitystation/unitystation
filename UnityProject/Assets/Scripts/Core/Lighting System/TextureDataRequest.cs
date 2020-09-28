using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class TextureDataRequest : IDisposable
{
	private NativeArray<Color32> colors;
	private int height;
	private bool requestLoaded;
	private int width;

	public void StoreData(AsyncGPUReadbackRequest request)
	{
		if (colors.Length > 0)
		{
			colors.Dispose();
		}
		requestLoaded = true;
		colors = new NativeArray<Color32>(request.GetData<Color32>(), Allocator.Persistent);
		height = request.height;
		width = request.width;
	}

	public bool TryGetPixelNormalized(float normalizedX, float normalizedY, out Color32 color)
	{
		var w = (int) (normalizedX * width);
		var h = (int) (normalizedY * height);
		return TryGetPixel(w, h, out color);
	}

	public bool TryGetPixel(int x, int y, out Color32 color)
	{
		color = default;

		if (requestLoaded == false ||
		    x < 0 || x > width ||
		    y < 0 || y > height)
		{
			return false;
		}

		var index = y * width + x;
		color = colors[index];
		return true;
	}

	public void Dispose()
	{
		if (colors.Length > 0)
		{
			colors.Dispose();
		}
	}
}