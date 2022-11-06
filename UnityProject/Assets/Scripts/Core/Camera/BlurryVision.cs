using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlurryVision : MonoBehaviour
{
	public Material material;

	[SerializeField] private int _BlurryStrength = 20;

	public void SetBlurStrength(int InStrength)
	{
		if (InStrength <= 0)
		{
			_BlurryStrength = 1;
			this.enabled = false;
		}
		else
		{
			this.enabled = true;
			if (InStrength > 30)
			{
				InStrength = 30;
			}

			_BlurryStrength = InStrength;
		}
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		material.SetInt("_BlurryStrength", _BlurryStrength);
		Graphics.Blit(source, destination, material);
	}
}
