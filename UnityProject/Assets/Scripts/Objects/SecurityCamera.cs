using System;
using System.Collections.Generic;
using Systems.Ai;
using Systems.Electricity;
using Core.Input_System.InteractionV2.Interactions;
using Mirror;
using Objects.Research;
using UnityEngine;
using UnityEngine.Events;

namespace Objects
{
	public class SecurityCamera : NetworkBehaviour, ICheckedInteractable<AiActivate>, ICheckedInteractable<HandApply>, IExaminable
	{
		private static Dictionary<string, List<SecurityCamera>> cameras = new Dictionary<string, List<SecurityCamera>>();
		public static Dictionary<string, List<SecurityCamera>> Cameras => cameras;

		[SerializeField]
		[Tooltip("The name of the camera, has // - SecCam// added on to the back automatically")]
		private string cameraName = "";
		public string CameraName => cameraName;

		[SerializeField]
		private string securityCameraChannel = "Station";
		public string SecurityCameraChannel => securityCameraChannel;

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

		private RegisterObject registerObject;
		public RegisterObject RegisterObject => registerObject;

		private APCPoweredDevice apcPoweredDevice;
		private Integrity integrity;

		[NonSerialized]
		public UnityEvent<bool> OnStateChange = new UnityEvent<bool>();

		private void Awake()
		{
			apcPoweredDevice = GetComponent<APCPoweredDevice>();
			registerObject = GetComponent<RegisterObject>();
			integrity = GetComponent<Integrity>();
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
				integrity.OnWillDestroyServer.AddListener(OnCameraDestruction);
			}
		}

		private void OnDisable()
		{
			cameras[securityCameraChannel].Remove(this);

			apcPoweredDevice.OrNull()?.OnStateChangeEvent.RemoveListener(PowerStateChanged);
			integrity.OnWillDestroyServer.RemoveListener(OnCameraDestruction);
		}

		#region Ai Camera Switching Interaction

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
			if (interaction.Performer.TryGetComponent<AiPlayer>(out var aiPlayer) == false) return;

			if(aiPlayer.OpenNetworks.Contains(securityCameraChannel) == false) return;

			if (cameraActive == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"Cannot move to {gameObject.ExpensiveName()}, as it is deactivated");
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

			//Ai core has this camera script so dont try to cut cameras on it
			if (interaction.TargetObject.GetComponent<AiVessel>() != null) return false;

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
				ServerSetCameraState(false);
			}
			//Switch to wire state otherwise
			else
			{
				ServerSetCameraState(wiresCut == false);
			}

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
				ServerSetCameraState(false);
				return;
			}

			//If was off turn on if wires not cut
			if (oldAndNewStates.Item1 == PowerState.Off && wiresCut == false)
			{
				ServerSetCameraState(true);
			}
		}

		#endregion

		[Server]
		//Called when camera is deactivated or reactivated, e.g loss of power or wires cut/repaired
		//Or when camera is destroyed
		private void ServerSetCameraState(bool newState)
		{
			cameraActive = newState;
			spriteHandler.OrNull()?.ChangeSprite(cameraActive ? 1 : 0);
			OnStateChange.Invoke(newState);

			//On state change, resync number of active cameras
			SyncNumberOfCameras();
		}

		[Server]
		public static void SyncNumberOfCameras()
		{
			foreach (var player in PlayerList.Instance.GetAllPlayers())
			{
				if(player.Script.PlayerState != PlayerScript.PlayerStates.Ai) continue;

				if (player.Script.TryGetComponent<AiPlayer>(out var aiPlayer) == false) continue;

				var count = 0;
				foreach (var cameraGroup in cameras)
				{
					//Only check camera groups Ai can see
					if(aiPlayer.OpenNetworks.Contains(cameraGroup.Key) == false) continue;

					foreach (var camera in cameraGroup.Value)
					{
						//Only active cameras
						if(camera.cameraActive == false) continue;

						count++;
					}
				}

				aiPlayer.ServerSetNumberOfCameras(count);
			}
		}

		private void OnCameraDestruction(DestructionInfo info)
		{
			ServerSetCameraState(false);
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			if (GetComponent<AiVessel>() != null) return "";

			return $"The cameras back panel is {(panelOpen ? "open" : "closed")} and the camera is {(cameraActive ? "active" : "deactivated")}";
		}
	}
}
