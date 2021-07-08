using System;
using CameraEffects;
using Mirror;
using UnityEngine;

namespace Player
{
	/// <summary>
	/// Class which contains sync vars which are only sent to the client controlling this player
	/// </summary>
	public class PlayerOnlySyncValues : NetworkBehaviour
	{
		#region SyncVars

		//NightVision
		[SyncVar(hook = nameof(SyncNightVision))]
		private bool nightVisionState;
		[SyncVar]
		private Vector3 nightVisionVisibility = new Vector3(10.5f, 10.5f, 21);
		[SyncVar]
		private float visibilityAnimationSpeed = 1.5f;

		//Antag
		[SyncVar(hook = nameof(SyncAntagState))]
		private bool isAntag;
		public bool IsAntag => isAntag;

		#endregion

		#region OtherVariables

		//NightVision
		private CameraEffectControlScript cameraEffectControlScript;

		#endregion

		#region LifeCycle

		private void Awake()
		{
			cameraEffectControlScript = Camera.main.OrNull()?.GetComponent<CameraEffectControlScript>();
		}

		#endregion

		#region Server

		[Server]
		public void ServerSetNightVision(bool newState, Vector3 visibility, float speed)
		{
			nightVisionVisibility = visibility;
			visibilityAnimationSpeed = speed;
			nightVisionState = newState;
		}

		[Server]
		public void ServerSetAntag(bool newState)
		{
			isAntag = newState;
		}

		#endregion

		#region Client

		[Client]
		private void SyncNightVision(bool oldState, bool newState)
		{
			nightVisionState = newState;
			cameraEffectControlScript.AdjustPlayerVisibility(nightVisionVisibility, nightVisionState ? visibilityAnimationSpeed : 0.1f);
			cameraEffectControlScript.ToggleNightVisionEffectState(nightVisionState);
		}

		[Client]
		private void SyncAntagState(bool oldState, bool newState)
		{
			isAntag = newState;
			GetComponent<PlayerScript>().ActivateAntagAction(newState);
		}

		#endregion
	}
}
