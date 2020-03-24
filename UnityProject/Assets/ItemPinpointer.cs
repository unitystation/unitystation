using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;
using Mirror;
using UnityEngine.Events;

public class ItemPinpointer : NetworkBehaviour, IInteractable<HandActivate>
{
	public GameObject rendererSprite;
	private GameObject objectToTrack;
	void Awake()
	{
		EnsureInit();
	}
	private void Update()
	{
		Vector3 moveDirection = objectToTrack.AssumedWorldPosServer() - gameObject.AssumedWorldPosServer();
		if (moveDirection != Vector3.zero)
		{
			float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
			rendererSprite.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
		}
	}
	private void EnsureInit()
	{
		objectToTrack = FindObjectOfType<NukeDiskScript>().gameObject;
		StartCoroutine(Animation());
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		Chat.AddExamineMsgFromServer(interaction.Performer,"Nuke disk:" + objectToTrack.AssumedWorldPosServer().ToString());
		Chat.AddExamineMsgFromServer(interaction.Performer,"You:" + gameObject.AssumedWorldPosServer().ToString());
	}

	private IEnumerator Animation()
	{

		while (true)
		{
			Vector3 moveDirection = gameObject.AssumedWorldPosServer() - objectToTrack.AssumedWorldPosServer();
			if (moveDirection != Vector3.zero)
			{
				float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
				rendererSprite.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
			}
			yield return WaitFor.Seconds(1.0f);
		}
	}


}
