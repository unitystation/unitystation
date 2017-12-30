using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace PlayGroup
{
	/// <summary>
	///     Player health reporting.
	///     This is the Server -> Client reporting for player health
	///     The server also calculates the overall health values in this component
	///     (To determine maxHealth for huds, ui overlays etc)
	/// </summary>
	public class PlayerHealthReporting : ManagedNetworkBehaviour
	{
		private int bloodLevelCache;
		private float BloodPercentage = 100f;

		//server only caches
		private int healthServerCache;
		//TODO if client disconnects and reconnects then the clients UI needs to 
		//poll this component and request updated values from the server to set
		//the current state of the UI overlays and hud

		public PlayerHealth playerHealth;
		private PlayerMove playerMove;

		protected override void OnEnable()
		{
			//Do not call base method for this OnEnable.
		}

		public override void OnStartServer()
		{
			if (isServer)
			{
				healthServerCache = playerHealth.Health;
				bloodLevelCache = playerHealth.BloodLevel;
				playerMove = GetComponent<PlayerMove>();
				UpdateManager.Instance.Add(this);
				StartCoroutine(WaitForLoad());
			}
			base.OnStartServer();
		}

		private IEnumerator WaitForLoad()
		{
			yield return new WaitForSeconds(1f); //1000ms wait for lag

			UpdateClientUI(100); //Set the UI for this player to 100 percent
		}

		private void OnDestroy()
		{
			if (isServer)
			{
				UpdateManager.Instance.Remove(this);
			}
		}

		//This only runs on the server, server will do the calculations and send
		//messages to the client when needed (or requested)
		public override void UpdateMe()
		{
			ServerMonitorHealth();
			base.UpdateMe();
		}

		private void ServerMonitorHealth()
		{
			//Add other damage methods here like burning, 
			//suffication, etc

			//If already dead then do not check the status of the body anymore
			if (playerMove.isGhost)
			{
				return;
			}

			//Blood calcs:
			if (bloodLevelCache != playerHealth.BloodLevel)
			{
				bloodLevelCache = playerHealth.BloodLevel;
				if (playerHealth.BloodLevel >= 560)
				{
					//Full blood (or more)
					BloodPercentage = 100f;
				}
				else
				{
					BloodPercentage = playerHealth.BloodLevel / 560f * 100f;
				}
			}

			//If blood level falls below health level, then set the health level
			//manually and update the clients UI
			if (BloodPercentage < playerHealth.Health)
			{
				healthServerCache = (int) BloodPercentage;
				playerHealth.ServerOnlySetHealth(healthServerCache);
				UpdateClientUI(healthServerCache);
			}

			if (playerHealth.Health != healthServerCache)
			{
				healthServerCache = playerHealth.Health;
				UpdateClientUI(healthServerCache);
			}
		}

		//Sends msg to the owner of this player to update their UI
		[Server]
		private void UpdateClientUI(int newHealth)
		{
			UpdateUIMessage.Send(gameObject, newHealth);
		}
	}
}