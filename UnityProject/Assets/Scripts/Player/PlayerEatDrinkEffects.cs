using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using CameraEffects;

public class PlayerEatDrinkEffects : NetworkBehaviour
{
	[SyncVar(hook = nameof(ClientSide))]
	private int alcoholValue;

	private Camera camera;

	private void Awake()
	{
		camera = Camera.main;
	}

	private void ClientSide(int oldValue, int newValue)
	{
		if (camera == null) return;
		camera.GetComponent<CameraEffectControlScript>().drunkCameraTime += newValue;
	}

	[Server]
	public void SyncServer(int newValue)
	{
		alcoholValue = newValue;
	}
}
