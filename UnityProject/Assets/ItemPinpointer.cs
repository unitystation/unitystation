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
	public float timeElapsedSprite = 0;
	public float timeElapsedIcon = 0;
	public float timeWait = 1;

	private bool isOn = false;

	public float maxMagnitude = 50;
	public float mediumMagnitude = 20;
	public float closeMagnitude = 10;


	[SyncVar(hook = nameof(SyncDiskPos))]
	private Vector3 distance;

	private void OnEnable()
	{
			EnsureInit();	
	}
	private void Update()
	{
		timeElapsedIcon += Time.deltaTime;
		if (timeElapsedIcon > 0.2f)
		{
			if (isOn)
			{
				pick.RefreshUISlotImage();
				timeElapsedIcon = 0;
			}
		}
	}
	private void ChangeAngleofSprite(Vector3 moveDirection)
	{
		
		float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
		CheckDistance(moveDirection,angle);
	}
	private void CheckDistance(Vector3 moveDirection, float angle)
	{
		if(moveDirection.magnitude > maxMagnitude)
		{
			ChangeSprite(0);
			spriteHandler.ChangeSprite(4);
			
		}
		else if (moveDirection.magnitude > mediumMagnitude)
		{
			spriteHandler.ChangeSprite(0);
			AngleUpdate(angle);
		}
		else if (moveDirection.magnitude > closeMagnitude)
		{
			spriteHandler.ChangeSprite(1);
			AngleUpdate(angle);
		}
		else if (moveDirection == Vector3.zero)
		{
			ChangeSprite(0);
			spriteHandler.ChangeSprite(3);
			
		}
		else if (moveDirection.magnitude < closeMagnitude)
		{
			spriteHandler.ChangeSprite(2);
			AngleUpdate(angle);
		}
		
	}
	private void AngleUpdate(float angle)
	{
		switch (angle)
		{
			case 0f:
				ChangeSprite(2);
				return;
			case 180.0f:
				ChangeSprite(3);
				return;
			case -90.0f:
				ChangeSprite(0);
				return;
			case 90.0f:
				ChangeSprite(1);
				return;
			default:
				break;
		}
		if(angle < 0.0f && angle > -90.0f)
		{
			ChangeSprite(4);
			return;
		}
		if (angle > 0.0f && angle < 90.0f)
		{
			ChangeSprite(6);
			return;
		}
		if (angle > 90.0f && angle < 180.0f)
		{
			ChangeSprite(7);
			return;
		}
		if (angle < -90.0f)
		{
			ChangeSprite(5);
			return;
		}

	}
	private void ChangeSprite(int variant)
	{
		spriteHandler.ChangeSpriteVariant(variant);
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
		if(isOn)
		{
			ChangeSprite(0);
			spriteHandler.ChangeSprite(8);
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			isOn = !isOn;
		}
		else
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			isOn = !isOn;
		}
		
	}

	private void SyncDiskPos(Vector3 oldPositon, Vector3 newPosition)
	{
		distance = newPosition;
		ChangeAngleofSprite(distance);
	}

	[Server]
	private void ServerChangeLightState(Vector3 newPosition)
	{
		distance = newPosition;
	}
	protected virtual void UpdateMe()
	{
		timeElapsedSprite += Time.deltaTime;
		if (timeElapsedSprite > timeWait)
		{
			Vector3 moveDirection = objectToTrack.AssumedWorldPosServer() - gameObject.AssumedWorldPosServer();
			ServerChangeLightState(moveDirection);
			timeElapsedSprite = 0;
		}
	}
}
