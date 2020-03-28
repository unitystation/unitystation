﻿using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using System.Linq;

public class ItemPinpointer : NetworkBehaviour, IInteractable<HandActivate>
{
	public GameObject rendererSprite;
	private GameObject objectToTrack;
	private SpriteHandler spriteHandler;

	private ItemsSprites newSprites = new ItemsSprites();
	private Pickupable pick;
	public float timeElapsedSprite = 0;
	public float timeElapsedIcon = 0;
	public float timeWait = 1;

	private bool isOn = false;

	public float maxMagnitude = 80;
	public float mediumMagnitude = 40;
	public float closeMagnitude = 10;

	[SyncVar(hook = nameof(SyncSheetVariant))]
	private int spriteSheetVariant;

	[SyncVar(hook = nameof(SyncVariant))]
	private int spriteVariant;

	private void OnEnable()
	{
		EnsureInit();
	}

	public override void OnStartServer()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	void OnDisable()
	{
		if (isServer)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}
	}

	private void ChangeAngleofSprite(Vector3 moveDirection)
	{
		
		float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
		if (CheckDistance(moveDirection))
		{
			AngleUpdate(angle);
		}
	}
	private bool CheckDistance(Vector3 moveDirection)
	{
		if (moveDirection.magnitude > mediumMagnitude)
		{
			ServerChangeSpriteSheetVariant(4);

		}
		else if (moveDirection.magnitude > closeMagnitude)
		{
			ServerChangeSpriteSheetVariant(1);
		}
		else if (moveDirection.magnitude < closeMagnitude)
		{
			if (moveDirection == Vector3.zero)
			{
				ServerChangeSpriteVariant(0);
				ServerChangeSpriteSheetVariant(3);
				return false;
			}
			ServerChangeSpriteSheetVariant(2);

		}
		return true;
	}
	private void AngleUpdate(float angle)
	{
		switch (angle)
		{
			case 0f:
				ServerChangeSpriteVariant(2);
				return;
			case 180.0f:
				ServerChangeSpriteVariant(3);
				return;
			case -90.0f:
				ServerChangeSpriteVariant(0);
				return;
			case 90.0f:
				ServerChangeSpriteVariant(1);
				return;
			default:
				break;
		}
		if(angle < 0.0f && angle > -90.0f)
		{
			ServerChangeSpriteVariant(4);
			return;
		}
		if (angle > 0.0f && angle < 90.0f)
		{
			ServerChangeSpriteVariant(6);
			return;
		}
		if (angle > 90.0f && angle < 180.0f)
		{
			ServerChangeSpriteVariant(7);
			return;
		}
		if (angle < -90.0f)
		{
			ServerChangeSpriteVariant(5);
			return;
		}

	}

	private void SyncSheetVariant(int oldSheetVar, int newSheetVar)
	{
		spriteSheetVariant = newSheetVar;
		spriteHandler.ChangeSprite(spriteSheetVariant);

	}

	private void SyncVariant(int oldVar, int newVar)
	{
		spriteVariant = newVar;
		spriteHandler.ChangeSpriteVariant(newVar);
		pick.RefreshUISlotImage();

	}
	private void EnsureInit()
	{
		pick = GetComponent<Pickupable>();
		spriteHandler = rendererSprite.GetComponent<SpriteHandler>();
		objectToTrack = FindObjectOfType<NukeDiskScript>().gameObject;
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
			if(objectToTrack == null)
			{
				objectToTrack = FindObjectOfType<NukeDiskScript>().gameObject;
			}
			ServerChangeSpriteVariant(0);
			ServerChangeSpriteSheetVariant(0);
			isOn = !isOn;		
	}

	[Server]
	private void ServerChangeSpriteSheetVariant(int newSheetVar)
	{
		spriteSheetVariant = newSheetVar;
	}

	[Server]
	private void ServerChangeSpriteVariant(int newVar)
	{
		spriteVariant = newVar;
	}
	protected virtual void UpdateMe()
	{
		timeElapsedSprite += Time.deltaTime;
		timeElapsedIcon += Time.deltaTime;
		if (timeElapsedSprite > timeWait)
		{
			if (isOn)
			{
				Vector3 moveDirection = objectToTrack.AssumedWorldPosServer() - gameObject.AssumedWorldPosServer();
				ChangeAngleofSprite(moveDirection);
			}
			timeElapsedSprite = 0;
		}
		if (isOn && timeElapsedIcon > 0.2f)
		{
				pick.RefreshUISlotImage();
				timeElapsedIcon = 0;
		}
	}
}
