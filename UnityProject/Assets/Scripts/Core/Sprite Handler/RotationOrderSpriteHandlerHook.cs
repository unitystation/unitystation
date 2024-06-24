using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class RotationOrderSpriteHandlerHook : MonoBehaviour
{
	public SpriteHandler SH;

	[SerializeField]
	[Tooltip("Should the Sprite order change with the rotation")]
	private bool SetOrder = false;

	[ShowIf(nameof(SetOrder))] public List<int> Orders = new List<int>(){0, 0, 0, 0};

	[SerializeField]
	[Tooltip("Should the SetLayer change with the rotation")]
	private bool SetLayer= false;

	[ShowIf(nameof(SetLayer))] public List<string > Layers = new List<string >(){"Rename me 1", "Rename me 2","Rename me 3", "Rename me 4"};

	public void Awake()
	{
		SH = GetComponent<SpriteHandler>();
		SH.OnVariantUpdated += UpdateLayerORSorting;

	}

	public void UpdateLayerORSorting()
	{
		if (SetOrder)
		{
			SH.SpriteRenderer.sortingOrder = Orders[SH.variantIndex];
		}

		if (SetLayer)
		{
			SH.SpriteRenderer.sortingLayerName = Layers[SH.variantIndex];
		}

	}
}
