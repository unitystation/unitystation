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
		private Vector3 nightVisionVisibility;
		[SyncVar]
		private float visibilityAnimationSpeed = 1.5f;
		//

		#endregion

		#region OtherVariables

		//NightVision
		private Camera mainCamera;
		//

		#endregion

		#region LifeCycle

		private void Awake()
		{
			mainCamera = Camera.main;
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

		#endregion

		#region Client

		[Client]
		private void SyncNightVision(bool oldState, bool newState)
		{
			nightVisionState = newState;
			mainCamera.GetComponent<CameraEffectControlScript>().AdjustPlayerVisibility(nightVisionVisibility, nightVisionState ? visibilityAnimationSpeed : 0.1f);
			mainCamera.GetComponent<CameraEffectControlScript>().ToggleNightVisionEffectState(nightVisionState);
		}

		#endregion
	}
}
