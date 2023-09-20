using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using ScriptableObjects;
using Systems.Ai;
using Systems.Electricity;
using Systems.Interaction;
using Systems.MobAIs;
using Objects.Research;
using Objects.Wallmounts;


namespace Objects
{
	public class SecurityCamera : NetworkBehaviour, ICheckedInteractable<AiActivate>, ICheckedInteractable<HandApply>, IExaminable
	{
		//Accurate on client and server but wont be when custom securityCameraChannel are added
		//TODO sync channel and change its current dictionary position when sync'd on client
		private static Dictionary<string, List<SecurityCamera>> cameras = new Dictionary<string, List<SecurityCamera>>();
		public static Dictionary<string, List<SecurityCamera>> Cameras => cameras;

		[SerializeField]
		[Tooltip("The name of the camera, has // - SecCam// added on to the back automatically")]
		private string cameraName = "";
		public string CameraName => cameraName;

		[SerializeField]
		[SyncVar(hook = nameof(SyncChannel))]
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

		[SerializeField]
		[Tooltip("Whether this camera will send an alert if motion is detected near it (Players/Mobs)")]
		private bool motionSensingCamera;

		[SerializeField]
		[Tooltip("Sensing Range")]
		[UnityEngine.Range(0, 25)]
		private int motionSensingRange = 5;

		[NonSerialized]
		public UnityEvent<bool> OnStateChange = new UnityEvent<bool>();

		private Vector3 previousDetectPosition;

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
			}
			else
			{
				cameras[securityCameraChannel].Add(this);
			}

			cameraActive = !wiresCut;

			if (CustomNetworkManager.IsServer)
			{
				//Make sure new lights are set correctly for newly built cameras
				if (lightOn != GlobalLightStatus)
				{
					ToggleLight(GlobalLightStatus);
				}

				if (apcPoweredDevice != null)
				{
					apcPoweredDevice.OnStateChangeEvent += PowerStateChanged;
				}

				integrity.OnWillDestroyServer.AddListener(OnCameraDestruction);

				if (motionSensingCamera)
				{
					UpdateManager.Add(MotionSensingUpdate, 1f);
				}
			}
		}

		private void OnDisable()
		{
			cameras[securityCameraChannel].Remove(this);

			if (apcPoweredDevice != null)
			{
				apcPoweredDevice.OnStateChangeEvent -= PowerStateChanged;
			}

			integrity.OnWillDestroyServer.RemoveListener(OnCameraDestruction);

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, MotionSensingUpdate);
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
			if(PlayerManager.LocalPlayerObject.OrNull()?.GetComponent<AiPlayer>() == null) return;

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

		[Client]
		private void SyncChannel(string oldState, string newState)
		{
			SetNewChannel(oldState, newState);
		}

		[Server]
		public void ToggleLight(bool newState)
		{
			lightOn = newState;
		}

		public void SetUp(PlayerScript player)
		{
			if(player.PlayerInfo?.Connection == null) return;

			player.PlayerNetworkActions.TargetRpcOpenInput(gameObject, "Camera Channel", securityCameraChannel);
		}

		private void SetNewChannel(string oldState, string newState)
		{
			if (cameras.TryGetValue(oldState, out var list))
			{
				list.Remove(this);
			}

			//Add new
			if (cameras.ContainsKey(newState) == false)
			{
				cameras.Add(newState, new List<SecurityCamera> {this});
			}
			else
			{
				cameras[newState].Add(this);
			}
		}

		#region Player Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//Ai core has this camera script so dont try to cut cameras on it
			if (interaction.TargetObject.GetComponent<AiVessel>() != null) return false;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wirecutter)) return true;

			if (Validations.HasUsedActiveWelder(interaction)) return true;

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

			if (Validations.HasUsedActiveWelder(interaction))
			{
				TryWeld(interaction);
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

		private void TryWeld(HandApply interaction)
		{
			if (panelOpen == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Open the cameras back panel first");
				return;
			}

			if (wiresCut == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Cut the cameras wires first");
				return;
			}

			if (Validations.HasUsedActiveWelder(interaction))
			{
				//Unweld from wall
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
					"You start unwelding the camera assembly from the wall...",
					$"{interaction.Performer.ExpensiveName()} starts unwelding the camera assembly from the wall...",
					"You unweld the camera assembly onto the wall.",
					$"{interaction.Performer.ExpensiveName()} unwelds the camera assembly from the wall.",
					() =>
					{
						var result = Spawn.ServerPrefab(CommonPrefabs.Instance.CameraAssembly,
							registerObject.WorldPositionServer,
							transform.parent);

						if (result.Successful)
						{
							result.GameObject.GetComponent<Rotatable>().FaceDirection(GetComponent<Rotatable>().CurrentDirection);
							result.GameObject.GetComponent<CameraAssembly>().SetState(CameraAssembly.CameraAssemblyState.Unwelded);
						}

						_ = Despawn.ServerSingle(gameObject);
					});
			}
		}

		#endregion

		#region Power

		private void PowerStateChanged(PowerState Old, PowerState newState)
		{
			//If now off turn off
			if (newState == PowerState.Off)
			{
				ServerSetCameraState(false);
				return;
			}

			//If was off turn on if wires not cut
			if (newState == PowerState.On && wiresCut == false)
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
				if(player.Script.PlayerType != PlayerTypes.Ai) continue;

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

		#region Motion Sensing

		private void MotionSensingUpdate()
		{
			var cameraPos = registerObject.WorldPositionServer;
			var mobsFound = Physics2D.OverlapCircleAll(cameraPos.To2Int(),
				motionSensingRange, LayerMask.GetMask("Players", "NPC"));

			if (mobsFound.Length == 0)
			{
				//No targets
				return;
			}

			//Order mobs by distance, sqrMag distance cheaper to calculate
			var orderedMobs = mobsFound.OrderBy(
				x => (cameraPos - x.transform.position).sqrMagnitude).ToList();

			foreach (var mob in orderedMobs)
			{
				Vector3 worldPos;

				//Testing for player
				if (mob.TryGetComponent<PlayerScript>(out var script))
				{
					//Only target normal players and alive players can trigger sensor
					if(script.IsNormal == false || script.IsDeadOrGhost) continue;

					worldPos = script.WorldPos;
				}
				//Test for mobs
				else if (mob.TryGetComponent<MobAI>(out var mobAi))
				{
					//Only alive mobs can trigger sensor
					if(mobAi.IsDead) continue;

					worldPos = mobAi.ObjectPhysics.transform.position;
				}
				else
				{
					//Must be allowed mob or player so dont target them
					continue;
				}

				var linecast = MatrixManager.Linecast(cameraPos,
					LayerTypeSelection.Walls, LayerMask.GetMask("Door Closed", "Walls"), worldPos);

				//Check to see if we hit a wall or closed door
				if(linecast.ItHit) continue;

				//Don't spam the detect if the player hasn't moved
				if(previousDetectPosition == worldPos) continue;
				previousDetectPosition = worldPos;

				SendAlert(orderedMobs, cameraPos);

				//Only need to detect once
				break;
			}
		}

		private void SendAlert(List<Collider2D> colliders, Vector3 cameraPos)
		{
			foreach (var player in PlayerList.Instance.GetAllPlayers())
			{
				if(player.Script.PlayerType != PlayerTypes.Ai) continue;

				Chat.AddExamineMsgFromServer(player, $"ALERT: {gameObject.name} motion sensor activated");
			}

			//Send message to nearby players seeing the camera detect them
			foreach (var mob in colliders)
			{
				if(mob.TryGetComponent<PlayerScript>(out var script) == false) continue;

				//Only target normal players and alive players
				if(script.PlayerType != PlayerTypes.Normal || script.IsDeadOrGhost) continue;

				var linecast = MatrixManager.Linecast(cameraPos,
					LayerTypeSelection.Walls, LayerMask.GetMask("Door Closed", "Walls"), script.WorldPos);

				//Check to see if we hit a wall or closed door
				if(linecast.ItHit) continue;

				Chat.AddExamineMsgFromServer(script.gameObject, "The camera light flashes red");
			}
		}

		#endregion

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			if (GetComponent<AiVessel>() != null) return "";

			return $"The cameras back panel is {(panelOpen ? "open" : "closed")} and the camera is {(cameraActive ? "active" : "deactivated")}";
		}
	}
}
