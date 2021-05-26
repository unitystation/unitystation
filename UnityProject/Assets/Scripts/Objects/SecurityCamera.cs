using System;
using System.Collections.Generic;
using Systems.Ai;
using Systems.Electricity;
using Mirror;
using UnityEngine;

namespace Objects
{
	public class SecurityCamera : NetworkBehaviour, ICheckedInteractable<AiActivate>, ICheckedInteractable<HandApply>, IExaminable
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
		public GameObject CameraLight => cameraLight;

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

		private APCPoweredDevice apcPoweredDevice;
		public APCPoweredDevice ApcPoweredDevice => apcPoweredDevice;

		private void Awake()
		{
			apcPoweredDevice = GetComponent<APCPoweredDevice>();
		}

		private void OnEnable()
		{
			if (cameras.ContainsKey(securityCameraChannel) == false)
			{
				cameras.Add(securityCameraChannel, new List<SecurityCamera> {this});
				return;
			}

			cameras[securityCameraChannel].Add(this);

			cameraActive = !wiresCut;


			if (CustomNetworkManager.IsServer)
			{
				//Make sure new lights are set correctly for newly built cameras
				if (lightOn != GlobalLightStatus)
				{
					ToggleLight(GlobalLightStatus);
				}

				apcPoweredDevice.OrNull()?.OnStateChangeEvent.AddListener(PowerStateChanged);
			}
		}

		private void OnDisable()
		{
			cameras[securityCameraChannel].Remove(this);

			apcPoweredDevice.OrNull()?.OnStateChangeEvent.RemoveListener(PowerStateChanged);
		}

		#region Ai Camera Switching

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			//Only if you click normally do you switch cameras
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick) return false;

			//Validate distance check and target checks, dont linecast validate as we allow it through walls
			if (DefaultWillInteract.AiActivate(interaction, side, false) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			//TODO check camera network is valid??
			if(interaction.Performer.TryGetComponent<AiPlayer>(out var aiPlayer) == false) return;

			if (cameraActive == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Cannot move to camera, it is deactivated");
				return;
			}

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
			if(PlayerManager.LocalPlayer.OrNull()?.GetComponent<AiPlayer>() == null) return;

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

			//Always off if no power
			if (apcPoweredDevice.State == PowerState.Off)
			{
				cameraActive = false;
			}
			//Switch to wire state otherwise
			else if (wiresCut)
			{
				cameraActive = wiresCut;
			}

			spriteHandler.OrNull()?.ChangeSpriteVariant(wiresCut ? 0 : 1);

			Chat.AddActionMsgToChat(interaction.Performer, $"You {(wiresCut ? "cut" : "repair")} the cameras wiring",
				$"{interaction.Performer.ExpensiveName()} {(panelOpen ? "cuts" : "repairs")} the cameras wiring");
		}

		#endregion

		#region Power

		private void PowerStateChanged(Tuple<PowerState, PowerState> oldAndNewStates)
		{
			//If now off turn off
			if (oldAndNewStates.Item2 == PowerState.Off)
			{
				cameraActive = false;
				return;
			}

			//If was off turn on if wires not cut
			if (oldAndNewStates.Item1 == PowerState.Off && wiresCut == false)
			{
				cameraActive = true;
			}
		}

		#endregion

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return $"The cameras back panel is {(panelOpen ? "open" : "closed")} and the camera is {(cameraActive ? "active" : "deactivated")}";
		}
	}
}
