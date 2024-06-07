using System;
using System.Collections;
using System.Collections.Generic;
using CameraEffects;
using UnityEngine;

public class Ghost : MonoBehaviour
{

	private PlayerScript PlayerScript;
	public void Awake()
	{
		PlayerScript = GetComponent<PlayerScript>();
		PlayerScript.OnBodyPossesedByPlayer.AddListener(PlayerEnterGhost);
	}

	public void PlayerEnterGhost()
	{
		Camera.main.GetComponent<CameraEffectControlScript>().Stop();
	}
}
