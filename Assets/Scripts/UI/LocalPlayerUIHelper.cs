using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayGroup
{
	/// <summary>
	/// Monitors the health status of the localplayer and updates the UI
	/// </summary>
	public class LocalPlayerUIHelper : ManagedNetworkBehaviour
	{
		private PlayerHealth playerHealth;

		private void Start()
		{
			playerHealth = GetComponent<PlayerHealth>();
			UpdateManager.Instance.regularUpdate.Add(this);
		}

		public override void UpdateMe()
		{
			if (isLocalPlayer) {
				//If blood goes below safe level and overlay crit is on normal
				//then show the shroud around the edges of the screen
				if (playerHealth.BloodLevel < (int)BloodVolume.SAFE && UI.UIManager.OverlayCrits.currentState
				   == UI.OverlayState.normal) {
					UI.UIManager.OverlayCrits.SetState(UI.OverlayState.injured);
				}

				if (playerHealth.ConsciousState == ConsciousState.UNCONSCIOUS && UI.UIManager.OverlayCrits.currentState
				    != UI.OverlayState.unconscious && playerHealth.BloodLevel > (int)BloodVolume.SURVIVE) {
					UI.UIManager.OverlayCrits.SetState(UI.OverlayState.unconscious);
				}
			}
			base.UpdateMe();
		}
	}
}
