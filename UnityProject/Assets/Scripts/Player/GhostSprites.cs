﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Handles displaying the ghost sprites.
/// </summary>
[RequireComponent(typeof(Rotatable))]
public class GhostSprites : MonoBehaviour
{
	//sprite renderer showing the ghost
	private SpriteHandler SpriteHandler;

	public List<SpriteDataSO> GhostSpritesSOs = new List<SpriteDataSO>();

	public List<SpriteDataSO> AdminGhostSpriteSOs = new List<SpriteDataSO>();

	private Rotatable rotatable;

	protected void Awake()
	{
		rotatable = GetComponent<Rotatable>();
		SpriteHandler = GetComponentInChildren<SpriteHandler>();
	}

	private void OnEnable()
	{
		rotatable.OnRotationChange.AddListener(OnDirectionChange);
	}

	private void OnDisable()
	{
		rotatable.OnRotationChange.RemoveListener(OnDirectionChange);
	}

	public void SetGhostSprite(bool isAdmin)
	{
		if (isAdmin)
		{
			SpriteHandler.SetSpriteSO(AdminGhostSpriteSOs.PickRandom());
		}
		else
		{
			SpriteHandler.SetSpriteSO(GhostSpritesSOs.PickRandom());
		}
	}

	private void OnDirectionChange(OrientationEnum direction)
	{
		if (OrientationEnum.Down_By180 == direction)
		{
			SpriteHandler.SetSpriteVariant(0, networked:false);
		}
		else if (OrientationEnum.Up_By0 == direction)
		{
			SpriteHandler.SetSpriteVariant(1, networked:false);
		}
		else if (OrientationEnum.Right_By270 == direction)
		{
			SpriteHandler.SetSpriteVariant(2, networked:false);
		}
		else
		{
			SpriteHandler.SetSpriteVariant(3, networked:false);
		}
	}
}
