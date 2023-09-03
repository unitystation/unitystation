using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System.Linq;
using UnityEditor;
using Gateway;
using Logs;
using Systems.Electricity;
using Managers;
using Strings;
using UI.Systems.Tooltips.HoverTooltips;

namespace Objects
{
	/// <summary>
	/// For Gateways inheritable class
	/// </summary>
	public class StationGateway : NetworkBehaviour, IAPCPowerable, IExaminable, IHoverTooltip
	{
		[SerializeField]
		private SpriteRenderer[] Sprites = null;
		//SpriteBaseBottom, SpriteBaseTop, SpriteBaseRightMiddle, SpriteBaseLeftMiddle, SpriteBaseRightBottom, SpriteBaseLeftBottom, SpriteBaseRightTop, SpriteBaseLeftTop, SpriteBaseCentre

		private PowerState CurrentState = PowerState.On;
		private float OnWatts = 1000;
		private float OffWatts = 0.1f;
		private APCPoweredDevice PoweredDevice;

		private int animationOffset = 0;

		[SerializeField]
		private Sprite[] Online = null;
		[SerializeField]
		private Sprite[] Offline = null;
		[SerializeField]
		private Sprite[] PowerOff = null;

		private WorldGateway selectedWorld;// The world from the list that was chosen

		/// <summary>
		/// Gets the coordinates of the teleport target where things will be teleported to.
		/// </summary>
		//If the selected world has an override, use it.
		public virtual Vector3 TeleportTargetCoord => selectedWorld.OverrideCoord != Vector3Int.zero
			? selectedWorld.OverrideCoord
			: selectedWorld.registerTile.WorldPosition;

		private bool HasPower = true; // Not used atm

		private bool IsConnected;

		protected bool SpawnedMobs = false;

		private int RandomCountBegining = 300; // Defaults to between 5 and 20 mins gate will open.
		private int RandomCountEnd = 1200;

		protected RegisterTile registerTile;

		private Matrix Matrix => registerTile.Matrix;

		public string WorldName = "The Station";

		protected Vector3Int Position;

		protected string Message;

		protected float timeElapsedServer = 0;
		protected float timeElapsedClient = 0;
		protected float timeElapsedServerSound = 0;
		public float DetectionTime = 1;

		public float SoundLength = 7f;
		public float AnimationSpeed = 0.25f;

		private float WaitTimeBeforeActivation { get; set; }

		[SyncVar(hook = nameof(SyncState))]
		private bool isOn = false;

		#region Lifecycle

		private void OnEnable()
		{
			PoweredDevice = GetComponent<APCPoweredDevice>();
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		public override void OnStartServer()
		{
			SetOffline();

			registerTile = GetComponent<RegisterTile>();
			Position = registerTile.WorldPosition;
			SubSceneManager.RegisterStationGateway(this);
			ServerChangeState(false);
			bool loadNormally = true;
			if (Application.isEditor)
			{
#if UNITY_EDITOR
				if (EditorPrefs.HasKey("prevEditorScene"))
				{
					if (!string.IsNullOrEmpty(EditorPrefs.GetString("prevEditorScene")))
					{
						if (SubSceneManager.Instance.awayWorldList.AwayWorlds.Contains(
							    EditorPrefs.GetString("prevEditorScene")))
						{
							loadNormally = false;
							// This will ensure that the gateway is ready in 30 seconds
							// if you are working on an awaysite in the editor
							WaitTimeBeforeActivation = 30f;
						}
					}
				}
#endif
			}

			if (loadNormally)
			{
				WaitTimeBeforeActivation = Random.Range(RandomCountBegining, RandomCountEnd);
			}

			Invoke(nameof(ConnectToWorld), WaitTimeBeforeActivation);
		}

		#endregion

		private void SyncState(bool oldVar, bool newVar)
		{
			isOn = newVar;
			// do your thing
			// all clients will be updated with this
		}

		[Server]
		public void ServerChangeState(bool newVar)
		{
			isOn = newVar;
		}

		protected virtual void UpdateMe()
		{
			if (isServer)
			{
				if (APCPoweredDevice.IsOn(CurrentState) == false) return;

				timeElapsedServer += Time.deltaTime;
				if (timeElapsedServer > DetectionTime && isOn)
				{
					DetectPlayer();
					timeElapsedServer = 0;
				}

				timeElapsedServerSound += Time.deltaTime;
				if (timeElapsedServerSound > SoundLength && isOn)
				{
					DetectPlayer();
					SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.MachineHum4, Position + Vector3Int.up);
					timeElapsedServerSound = 0;
				}
			}
			else
			{
				timeElapsedClient += Time.deltaTime;
				if (timeElapsedClient > AnimationSpeed)
				{
					if (isOn)
					{
						SetOnline();
					}
					else
					{
						SetOffline();
					}
					timeElapsedClient = 0;
				}
			}
		}

		[Server]
		private void ConnectToWorld()
		{
			var randomWorld = SubSceneManager.RequestRandomAwayWorldLink(this);

			if (randomWorld == null)
			{
				Loggy.Log("StationGateway failed to connect to an away world", Category.Machines);
				SetOffline();
				return;
			}

			selectedWorld = randomWorld;

			Message = "Teleporting to: " + selectedWorld.WorldName;

			if (selectedWorld.spawnMobsOnConnection)
			{
				selectedWorld.SetUp(this);
			}

			if (HasPower)
			{
				SetOnline();
				ServerChangeState(true);

				var text = "Alert! New Gateway connection formed.\n\n Connection established to: " + selectedWorld.WorldName;
				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Alert);
			}
		}

		[Server]
		public virtual void DetectPlayer()
		{
			// detect players positioned on the portal bit of the gateway
			var playersFound = Matrix.Get<MovementSynchronisation>(registerTile.LocalPositionServer + Vector3Int.up, ObjectType.Player, true);

			if (SpawnedMobs == false && selectedWorld != null && playersFound.Count() > 0)
			{
				selectedWorld.SetUp(this);
				Loggy.Log("Gateway Spawned Mobs", Category.Machines);
				if (selectedWorld.GetComponent<MobSpawnControlScript>() != null)
				{
					selectedWorld.GetComponent<MobSpawnControlScript>().SpawnMobs();
				}
				SpawnedMobs = true;
			}

			foreach (MovementSynchronisation player in playersFound)
			{
				var coord = new Vector2(Position.x, Position.y);
				Chat.AddLocalMsgToChat(Message, coord, gameObject, LanguageManager.Common);
				SoundManager.PlayNetworkedForPlayer(player.gameObject,CommonSounds.Instance.StealthOff); // very weird, sometimes does the sound other times not.
				TransportUtility.TransportObjectAndPulled(player, TeleportTargetCoord);
			}

			foreach (var item in Matrix.Get<UniversalObjectPhysics>(registerTile.LocalPositionServer + Vector3Int.up, ObjectType.Object, true)
				         .Concat(Matrix.Get<UniversalObjectPhysics>(registerTile.LocalPositionServer + Vector3Int.up, ObjectType.Item, true)))
			{
				TransportUtility.TransportObjectAndPulled(item, TeleportTargetCoord);
			}
		}

		public virtual void SetOnline()
		{
			SetSprites(Online);

			animationOffset += Sprites.Length;
			animationOffset %= Online.Length;
		}

		public virtual void SetOffline()
		{
			SetSprites(Offline);

			animationOffset += Sprites.Length;
			animationOffset %= Offline.Length;
		}

		public virtual void SetPowerOff()
		{
			animationOffset = 0;
			SetSprites(PowerOff);
		}

		private void SetSprites(Sprite[] spriteSet)
		{
			for (int i = 0; i < Sprites.Length; i++)
			{
				Sprites[i].sprite = spriteSet[i + animationOffset];
			}
		}

		#region IAPCPowerable

		public void PowerNetworkUpdate(float voltage) { }

		public void StateUpdate(PowerState state)
		{
			if (CurrentState == state) return;

			CurrentState = state;
			if (state == PowerState.Off)
			{
				PoweredDevice.Wattusage = OffWatts;
				SetPowerOff();
			}
			else
			{
				PoweredDevice.Wattusage = OnWatts;
				SetOnline();
			}
		}

		#endregion

		private string StateExamineMessage()
		{
			if (PoweredDevice.State == PowerState.Off)
			{
				return "The gateway stands lifeless, void of power.";
			}

			var text = selectedWorld == null
				? "A lone red LED blinks on the gateway, signaling a missing connection."
				: $"A green LED flashes on the gateway, hinting at a stable connection. Display says \"{selectedWorld.WorldName}\".";

			return text;
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return StateExamineMessage();
		}

		public string HoverTip()
		{
			return StateExamineMessage();
		}

		public Sprite CustomIcon() => Sprites[0].sprite;
		public string CustomTitle() => null;
		public List<Sprite> IconIndicators() => null;
		public List<TextColor> InteractionsStrings() => null;
	}
}
