using System;
using System.Collections;
using System.Collections.Generic;
using Atmospherics;
using UnityEngine;
using Mirror;
using UnityEngine.Events;

public class ItemPinpointer : NetworkBehaviour, IInteractable<HandActivate>
{

	private GameObject objectToTrack;
	void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		objectToTrack = FindObjectOfType<NukeDiskScript>().gameObject;	
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		Chat.AddExamineMsgFromServer(interaction.Performer,"Nuke disk:" + objectToTrack.AssumedWorldPosServer().ToString());
		Chat.AddExamineMsgFromServer(interaction.Performer,"You:" + gameObject.AssumedWorldPosServer().ToString());
	}




}
