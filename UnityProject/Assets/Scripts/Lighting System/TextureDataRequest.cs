using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public struct TextureDataRequest
{
	private bool mRequestLoaded;
	private AsyncGPUReadbackRequest mRequest;

	public TextureDataRequest(AsyncGPUReadbackRequest iRequest)
	{
		// Store request to track if it was disposed.
		mRequest = iRequest;
		mRequestLoaded = true;
	}

	private NativeArray<Color32> textureBuffer => mRequest.GetData<Color32>();

	private int height => mRequest.height;

	private int width => mRequest.width;

	public bool TryGetPixelNormalized(float iNormalizedX, float iNormalizedY, out Color32 oColor32)
	{
		var _width = (int)(iNormalizedX * width);
		var _height = (int)(iNormalizedY * height);

		return TryGetPixel(_width, _height, out oColor32);
	}

	public bool TryGetPixel(int iX, int iY, out Color32 oColor32)
	{
		oColor32 = default(Color32);

		if (mRequestLoaded == false ||
		    mRequest.done == false ||
		    mRequest.hasError == true ||
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