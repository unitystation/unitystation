using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup
{
	/// <summary>
	/// Player health reporting.
	/// 
	/// This is the Server -> Client reporting for player health
	/// The server also calculates the overall health values in this component
	/// (To determine maxHealth for huds, ui overlays etc)
	/// </summary>
	public class PlayerHealthReporting : ManagedNetworkBehaviour
	{
		//TODO if client disconnects and reconnects then the clients UI needs to 
		//poll this component and request updated values from the server to set
		//the current state of the UI overlays and hud

		public PlayerHealth playerHealth;

		//server only caches
		private int maxHealthServerCache;

		private void Start()
		{
			if (isServer) {
				maxHealthServerCache = playerHealth.maxHealth;
				UpdateManager.Instance.regularUpdate.Add(this);
				RpcUpdateClientUI(100); //Set the UI for this player to 100 percent
			}
		}

		private void OnDestroy()
		{
			if (isServer)
				UpdateManager.Instance.regularUpdate.Remove(this);
		}

		//This only runs on the server, server will do the calculations and send
		//messages to the client when needed (or requested)
		public override void UpdateMe(){
			ServerUpdateMaxHealth();
			base.UpdateMe();
		}
		private void ServerUpdateMaxHealth()
		{
			//Update when there is other damage methods like brute etc
			//atm there is only blood lose

			//Blood calcs:
			//TODO revist this when adding new methods of dmg
			float bloodLoseCalc = (float)playerHealth.maxHealth;
			if (playerHealth.BloodLevel >= 560) {
				//Do not adjust max health
			} else {
				bloodLoseCalc = ((float)playerHealth.BloodLevel / 560f) * 100f;
			}
			//TODO update this with new dmg methods when they are added
			playerHealth.maxHealth = (int)bloodLoseCalc;

			if(playerHealth.maxHealth != maxHealthServerCache){
				maxHealthServerCache = playerHealth.maxHealth;
				RpcUpdateClientUI(maxHealthServerCache);
			}
		}

		//TODO convert these RPC's to a net messages so it only sends data to the 
		//client that needs it
		/// <summary>
		/// Server will send Rpc if a value changes. Only the localplayer will
		/// carry on the action to the UI. 
		/// </summary>
		[ClientRpc]
		private void RpcUpdateClientUI(int maxHealth){
			if(isLocalPlayer){
				//Update the UI
				UI.UIManager.PlayerHealthUI.UpdateHealthUI(this, maxHealth);
			}
		}
	}
}
