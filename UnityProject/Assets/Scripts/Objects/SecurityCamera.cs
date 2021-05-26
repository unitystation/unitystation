using System;
using System.Collections.Generic;
using Systems.Ai;
using Mirror;
using UnityEngine;

namespace Objects
{
	public class SecurityCamera : NetworkBehaviour, ICheckedInteractable<AiActivate>, ICheckedInteractable<HandApply>
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

		[SerializeField]
		private GameObject cameraLight = null;

		[SerializeField]
		private SpriteHandler spriteHandler = null;

		[SyncVar(hook = nameof(SyncStatus))]
		private bool cameraActive = true;
		public bool CameraActive => cameraActive;

		[SyncVar(hook = nameof(SyncLight))]
		private bool lightOn;
		public bool LightOn => lightOn;

		//Valid serverside only
		public static bool GlobalLightStatus;

		private bool panelOpen;
		private bool wiresCut;

		private void OnEnable()
		{
			if (cameras.ContainsKey(securityCameraChannel) == false)
			{
				cameras.Add(securityCameraChannel, new List<SecurityCamera>{this});
				return;
			}

			cameras[securityCameraChannel].Add(this);

			//Make sure new lights are set correctly for newly built cameras
			if (CustomNetworkManager.IsServer && lightOn != GlobalLightStatus)
			{
				ToggleLight(GlobalLightStatus);
			}
		}

		private void OnDisable()
		{
			cameras[securityCameraChannel].Remove(this);

			cameraActive = false;
		}

		#region Ai Camera Switching

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			//Only if you click normally do you switch cameras
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick) return false;

			//Validate distance check and target checks
			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

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
			aiSprite.OrNull()?.SetActive(newState);
		}

		[Client]
		private void SyncLight(bool oldState, bool newState)
		{
			//Only allow light on if camera is active
			if(newState && cameraActive == false) return;

			cameraLight.SetActive(newState);
		}

		[Client]
		private void SyncStatus(bool oldState, bool newState)
		{
			if(PlayerManager.LocalPlayer.GetComponent<AiPlayer>() == null) return;

			ToggleAiSprite(newState);

			//If inactive turn light off
			if (newState == false)
			{
				cameraLight.SetActive(false);
			}
			else if (lightOn)
			{
				//Turn light back on if its supposed to be on
				cameraLight.SetActive(true);
			}
		}

		[Server]
		public void ToggleLight(bool newState)
		{
			lightOn = newState;
		}

		#region Player Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.HandApply(interaction, side) == false) return false;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
			{
				TryScrewdriver(interaction);
				return;
			}

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter))
			{
				TryCut(interaction);
				return;
			}
		}

		private void TryScrewdriver(HandApply interaction)
		{
			Chat.AddActionMsgToChat(interaction.Performer, $"You {(panelOpen ? "close" : "open")} the cameras back panel",
				$"{interaction.Performer.ExpensiveName()} {(panelOpen ? "closes" : "opens")} the cameras back panel");
			panelOpen = !panelOpen;
		}

		private void TryCut(HandApply interaction)
		{
			if (panelOpen == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Open the cameras back panel first");
				return;
			}

			wiresCut = !wiresCut;
			cameraActive = wiresCut;
			spriteHandler.OrNull()?.ChangeSpriteVariant(wiresCut ? 0 : 1);

			Chat.AddActionMsgToChat(interaction.Performer, $"You {(wiresCut ? "cut" : "repair")} the cameras wiring",
				$"{interaction.Performer.ExpensiveName()} {(panelOpen ? "cuts" : "repairs")} the cameras wiring");
		}

		#endregion
	}
}
