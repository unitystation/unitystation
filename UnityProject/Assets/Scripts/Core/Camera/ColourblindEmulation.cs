using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum ColourBlindMode
{
	None =  0,
	Tritan = 1 << 0,
	Protan = 1 << 1,
	Deuntan = 1 << 2,
}

public class ColourblindEmulation : MonoBehaviour
{
	public Material material;

	[SerializeField] [NaughtyAttributes.EnumFlags] private ColourBlindMode CurrentColourMode;

	public void SetColourMode(ColourBlindMode InColourMode)
	{
		if (InColourMode == ColourBlindMode.None)
		{
			this.enabled = false;
		}
		else
		{
			this.enabled = true;
			CurrentColourMode = InColourMode;
		}
	}


	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		material.SetInt("_DEUNTAN",  CurrentColourMode.HasFlag(ColourBlindMode.Deuntan) ? 1 : 0);
		material.SetInt("_TRITAN",  CurrentColourMode.HasFlag(ColourBlindMode.Tritan) ? 1 : 0);
		material.SetInt("_PROTAN",  CurrentColourMode.HasFlag(ColourBlindMode.Protan) ? 1 : 0);

		Graphics.Blit(source, destination, material);
	}
}
