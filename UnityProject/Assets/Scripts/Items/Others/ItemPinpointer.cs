using System;
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
	private float timeElapsedSprite = 0;
	private float timeElapsedIcon = 0;
	public float timeWait = 1;

	private bool isOn = false;

	public float maxMagnitude = 80;
	public float mediumMagnitude = 40;
	public float closeMagnitude = 10;

	private int spriteSheetVariant;

	private int spriteVariant;

	private void Start()
	{
		var NukeDisks = FindObjectsOfType<NukeDiskScript>();

		foreach (var nukeDisk in NukeDisks)
		{
			if (nukeDisk == null) continue;

			if(!nukeDisk.secondaryNukeDisk)
			{
				objectToTrack =  nukeDisk.gameObject;
				break;
			}
		}
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		EnsureInit();
	}
	void OnDisable()
	{

		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);

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

	private void SyncSheetVariant(int newSheetVar)
	{
		spriteVariant = 0;
		spriteHandler.ChangeSpriteVariant(0);
		spriteSheetVariant = newSheetVar;
		spriteHandler.ChangeSprite(spriteSheetVariant);
		pick.RefreshUISlotImage();

	}

	private void SyncVariant(int newVar)
	{
		spriteVariant = newVar;
		spriteHandler.ChangeSpriteVariant(newVar);
		pick.RefreshUISlotImage();
	}
	private void EnsureInit()
	{
		pick = GetComponent<Pickupable>();
		spriteHandler = rendererSprite.GetComponent<SpriteHandler>();
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
			if(objectToTrack == null)
			{
				objectToTrack = FindObjectOfType<NukeDiskScript>().gameObject;
			}
			isOn = !isOn;
			ServerChangeSpriteVariant(0);
			ServerChangeSpriteSheetVariant(0);
			pick.RefreshUISlotImage();
	}

	[Server]
	private void ServerChangeSpriteSheetVariant(int newSheetVar)
	{
		spriteSheetVariant = newSheetVar;
		SyncSheetVariant(spriteSheetVariant);
	}

	[Server]
	private void ServerChangeSpriteVariant(int newVar)
	{
		spriteVariant = newVar;
	}
	protected virtual void UpdateMe()
	{
		if (isServer)
		{

			timeElapsedSprite += Time.deltaTime;
			if (timeElapsedSprite > timeWait)
			{
				if (isOn)
				{
					Vector3 moveDirection = objectToTrack.AssumedWorldPosServer() - gameObject.AssumedWorldPosServer();
					ChangeAngleofSprite(moveDirection);
				}
				timeElapsedSprite = 0;
			}
		}
		else
		{
			timeElapsedIcon += Time.deltaTime;
			if (timeElapsedIcon > 0.2f)
			{
				pick.RefreshUISlotImage();
				timeElapsedIcon = 0;
			}
		}
	}
}
