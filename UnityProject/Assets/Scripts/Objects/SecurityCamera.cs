using System;
using System.Collections.Generic;
using Systems.Ai;
using UnityEngine;

namespace Objects
{
	public class SecurityCamera : MonoBehaviour, ICheckedInteractable<AiActivate>
	{
		private static Dictionary<string, List<SecurityCamera>> cameras = new Dictionary<string, List<SecurityCamera>>();
		public static Dictionary<string, List<SecurityCamera>> Cameras => cameras;

		private static List<string> openNetworks = new List<string>()
		{
			"Station"
		};
		public static List<string> OpenNetworks => openNetworks;

		[SerializeField]
		private string securityCameraChannel = "Station";

		[SerializeField]
		private GameObject aiSprite = null;

		private void OnEnable()
		{
			if (cameras.ContainsKey(securityCameraChannel) == false)
			{
				cameras.Add(securityCameraChannel, new List<SecurityCamera>{this});
				return;
			}

			cameras[securityCameraChannel].Add(this);
		}

		private void OnDisable()
		{
			cameras[securityCameraChannel].Remove(this);
		}

		#region Ai Camera Switching

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			//Only if you click normally do you switch cameras
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick) return false;

			if (interaction.Performer == null) return false;

			if(interaction.Performer.TryGetComponent<AiPlayer>(out var aiPlayer) == false) return false;

			//We should always have a camera location, either core or camera
			if (aiPlayer.CameraLocation == null) return false;

			//Distance check to make sure its in range, this wont be called for "saved" cameras
			if (Vector2.Distance(aiPlayer.CameraLocation.position, gameObject.transform.position) > aiPlayer.InteractionDistance) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			//TODO check camera network is valid??
			if(interaction.Performer.TryGetComponent<AiPlayer>(out var aiPlayer) == false) return;

			//Switch the players camera to this object
			aiPlayer.ServerSetCameraLocation(gameObject);
		}

		#endregion

		public void ToggleAiSprite(bool newState)
		{
			aiSprite.SetActive(newState);
		}
	}
}
