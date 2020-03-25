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
	private SpriteHandlerController spriteHandlerController;
	private ItemsSprites newSprites;
	private Pickupable pick;
	void Awake()
	{
		EnsureInit();
	}
	private void Update()
	{

		ChangeAngleofSprite();
	}
	private void ChangeAngleofSprite()
	{
		Vector3 moveDirection = objectToTrack.AssumedWorldPosServer() - gameObject.AssumedWorldPosServer();
		float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
		AngleUpdate(angle);
	}
	private void AngleUpdate(float angle)
	{
		switch (angle)
		{
			case 0f:
				//spriteHandler.SetSprite(spriteHandler.Sprites[0], 2);
				ChangeSprite(2);
				return;
			case 180.0f:
				//spriteHandler.SetSprite(spriteHandler.Sprites[0], 3);
				ChangeSprite(3);
				return;
			case -90.0f:
				//spriteHandler.SetSprite(spriteHandler.Sprites[0],0);
				ChangeSprite(0);
				return;
			case 90.0f:
				//spriteHandler.SetSprite(spriteHandler.Sprites[0], 1);
				ChangeSprite(1);
				return;
			default:
				break;
		}
		if(angle < 0.0f && angle > -90.0f)
		{
			//spriteHandler.SetSprite(spriteHandler.Sprites[0], 4);
			ChangeSprite(4);
			return;
		}
		if (angle > 0.0f && angle < 90.0f)
		{
			//spriteHandler.SetSprite(spriteHandler.Sprites[0], 6);
			ChangeSprite(6);
			return;
		}
		if (angle > 90.0f && angle < 180.0f)
		{
			//spriteHandler.SetSprite(spriteHandler.Sprites[0], 7);
			ChangeSprite(7);
			return;
		}
		if (angle < -90.0f)
		{
			//spriteHandler.SetSprite(spriteHandler.Sprites[0], 5);
			ChangeSprite(5);
			return;
		}

	}
	private void ChangeSprite(int variant)
	{
		spriteHandler.ChangeSpriteVariant(variant);
		newSprites.InventoryIcon = spriteHandler.Sprites[0];
		spriteHandlerController.SetSprites(newSprites);
	}
	private void EnsureInit()
	{
		spriteHandlerController = GetComponent<SpriteHandlerController>();
		spriteHandler = rendererSprite.GetComponent<SpriteHandler>();
		objectToTrack = FindObjectOfType<NukeDiskScript>().gameObject;
		StartCoroutine(Animation());
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		

		Vector3 moveDirection = objectToTrack.AssumedWorldPosServer() - gameObject.AssumedWorldPosServer();
		float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
		rendererSprite.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

		Chat.AddExamineMsgFromServer(interaction.Performer,"Nuke disk:" + objectToTrack.AssumedWorldPosServer().ToString());
		Chat.AddExamineMsgFromServer(interaction.Performer,"You:" + gameObject.AssumedWorldPosServer().ToString());

		Chat.AddExamineMsgFromServer(interaction.Performer,"Direction" +  moveDirection.ToString());
		Chat.AddExamineMsgFromServer(interaction.Performer,"Angle" + angle.ToString());
		Chat.AddExamineMsgFromServer(interaction.Performer, "Rotation" + Quaternion.AngleAxis(angle, Vector3.forward).ToString());
	}

	private IEnumerator Animation()
	{

		while (true)
		{
			ChangeAngleofSprite();
			yield return WaitFor.Seconds(1.0f);
		}
	}


}
