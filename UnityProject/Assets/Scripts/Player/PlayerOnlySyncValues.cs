using System;
using CameraEffects;
using Core.Utils;
using Items;
using Items.Tool;
using Mirror;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Player
{
	//how TO use
	//so, First of all you need 2 fields
	//the default state e.g when you ghost what it should be set to
	//and the active synchronised state
	//you got these two down now you build the hook
	//private void SyncXrayState(bool NotSetValueServer, bool newState)
	//so you Use SyncXrayState  to set the value, Noting that NotSetValueServer = False, every time you call it
	//and then inside of this statement
	//
	//if ((NotSetValueServer == false && isServer))
	//{
	//	SyncXRay = newState;
	//}
	//This is required so local client doesn't get Buggered by values getting reset when they Ghost
	//And then you do this check
	//if (ClientForThisBody)
	//This goes around your code that applies the value such as fov mask = x
	//this is to allow is local player = false in cases where it needs to be reset to default
	//You also must use the past in value
	// Such as NightVisionGoggles.NightVisionData newState
	// because the synchronised value is the non-reset one
	//Also use the Sync Your thing function with false and value


	/// <summary>
	/// Class which contains sync vars which are only sent to the client controlling this player
	/// </summary>
	public class PlayerOnlySyncValues : NetworkBehaviour
	{
		#region SyncVars

		//HiddenHands
		[SyncVar(hook = nameof(SyncHiddenHands))]
		private HiddenHandValue hiddenHandSelection;

		//Antag
		[SyncVar(hook = nameof(SyncAntagState))]
		private bool isAntag;
		public bool IsAntag => isAntag;
		#endregion

		#region OtherVariables

		public bool ClientForThisBody => OverrideLocalPlayer || hasAuthority;

		public bool OverrideLocalPlayer = false;

		#endregion

		#region Server

		[Server]
		public void ServerSetHiddenHands(HiddenHandValue newState)
        {
			hiddenHandSelection = newState;
        }

		[Server]
		public void ServerSetAntag(bool newState)
		{
			isAntag = newState;
			GetComponent<PlayerScript>().ActivateAntagAction(newState);
		}

		#endregion

		#region Client

		[Client]
		private void SyncHiddenHands(HiddenHandValue oldState, HiddenHandValue newState)
        {
			hiddenHandSelection = newState;
			HandsController.Instance.HideHands(hiddenHandSelection);
        }

		[Client]
		private void SyncAntagState(bool oldState, bool newState)
		{
			isAntag = newState;
		}
		#endregion

	}
}
