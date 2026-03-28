using System.Collections;
using Mirror;
using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.ObjectConnection;
using US13.Core.Sprite_Handler;
using US13.Core.Utils;
using US13.Managers;
using US13.Managers.UpdateManager;
using US13.Messages.Server.SoundMessages;
using US13.Objects.Construction.FloorDecals;
using US13.Objects.Gateway;
using US13.Player;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Behaviours.Objects;
using Util;
using Event = US13.Managers.Event;

namespace US13.Objects.Machines
{

	public class QuantumPad : NetworkBehaviour, IServerSpawn, ICheckedInteractable<HandApply>,IMultitoolMasterable, IMultitoolSlaveable
	{
		public QuantumPad connectedPad;

		/// <summary>
		/// Detects players/objects on itself every 1 second.
		/// </summary>
		public bool passiveDetect;

		/// <summary>
		/// Where should this pad drop you on the next pad?
		/// </summary>
		public PadDirection padDirection = PadDirection.OnTop;

		/// <summary>
		/// If you dont want the link to be changed.
		/// </summary>
		public bool disallowLinkChange;

		public string messageOnTravelToThis;

		private RegisterTile registerTile;
		[SerializeField] private float maintRoomChanceModifier = 0.1f; //Squarestation quantum pads are less likely to teleport to maintrooms due to their nessasity.

		private Matrix Matrix => registerTile.Matrix;

		private Vector3 travelCoord;

		private SpriteHandler spriteHandler;

		private bool doingAnimation;

		/// <summary>
		/// Temp until shuttle landings possible
		/// </summary>
		public bool IsLavaLandBase1;

		/// <summary>
		/// Temp until shuttle landings possible
		/// </summary>
		public bool IsLavaLandBase1Connector;
		private bool firstEnteredTriggered;

		/// <summary>
		/// Temp until shuttle landings possible
		/// </summary>
		public bool IsLavaLandBase2;

		/// <summary>
		/// Temp until shuttle landings possible
		/// </summary>
		public bool IsLavaLandBase2Connector;

		[field: SerializeField] public bool CanRelink { get; set; } = false;
		[field: SerializeField] public bool IgnoreMaxDistanceMapper { get; set; } = true;
		[field: SerializeField] public int MaxDistance { get; set; } = 60;
		[field: SerializeField] public float CheckForThingsToTeleportTime { get; set; } = 1.5f;

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.QuantumPad;

		GameObject IMultitoolLinkable.gameObject => gameObject;

		IMultitoolMasterable IMultitoolSlaveable.Master => connectedPad;

		[field: SerializeField] public bool RequireLink { get; set; } = true;

		bool IMultitoolSlaveable.TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			SetMaster(master);
			return true;
		}

		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			SetMaster(master);
		}

		private void SetMaster(IMultitoolMasterable master)
		{
			connectedPad = (QuantumPad) master;
		}


		[Server]
		private void ServerSync(bool newVar)
		{
			doingAnimation = newVar;
		}

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		private void Start()
		{
			//temp stuff

			if (IsLavaLandBase1)
			{
				LavaLandManager.LavaLandBase1 = this;
			}

			if (IsLavaLandBase2)
			{
				LavaLandManager.LavaLandBase2 = this;
			}

			if (IsLavaLandBase1Connector)
			{
				LavaLandManager.LavaLandBase1Connector = this;
			}

			if (IsLavaLandBase2Connector)
			{
				LavaLandManager.LavaLandBase2Connector = this;
			}

			spriteHandler.OrNull()?.SetCatalogueIndexSprite(0);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (!passiveDetect) return;

			UpdateManager.Add(ServerDetectObjectsOnTile, CheckForThingsToTeleportTime);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerDetectObjectsOnTile);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.IsTarget(gameObject, interaction)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			ServerDetectObjectsOnTile();
		}

		private void ServerDetectObjectsOnTile()
		{
			if (connectedPad == null) return;

			if (!doingAnimation && !passiveDetect)
			{
				ServerSync(true);

				StartCoroutine(ServerAnimation());
			}

			EnsureTravelCordsAreCorrect();

			var registerTileLocation = registerTile.LocalPositionServer;
			var somethingTeleported = false;

			//Use the transport object code from StationGateway
			//detect players positioned on the portal bit of the gateway
			foreach (var reg in Matrix.Get<CommonComponents>(registerTileLocation, isServer))
			{
				//Don't teleport self lol
				if (reg.gameObject == gameObject) continue;
				if (reg.UniversalObjectPhysics == null || reg.UniversalObjectPhysics.Intangible || reg.UniversalObjectPhysics.CanMove == false) continue;
				if (reg.TrySafeGetComponent<GhostMove>(out var ghost))
				{
					ghost.CMDSetServerPosition(travelCoord);
				}
				if (reg.TrySafeGetComponent<FloorDecal>(out var decal))
				{
					_ = Despawn.ServerSingle(decal.gameObject);
					continue;
				}

				HandleTeleportation(reg);
				HandleAudioOnTeleport();
				HandleLavalandFirstEnterEvent(); //(Max): Bad. This should be handled on the matrix itself; not via quantum pads.

				somethingTeleported = true;
				break;
			}

			if (doingAnimation == false && passiveDetect && somethingTeleported)
			{
				ServerSync(true);
				StartCoroutine(ServerAnimation());
				connectedPad.ServerSync(true);
				StartCoroutine(connectedPad.ServerAnimation());
			}
		}

		private void EnsureTravelCordsAreCorrect()
		{
			travelCoord = connectedPad.registerTile.WorldPositionServer;

			switch (padDirection)
			{
				case PadDirection.OnTop:
					break;
				case PadDirection.Up:
					travelCoord += Vector3.up;
					break;
				case PadDirection.Down:
					travelCoord += Vector3.down;
					break;
				case PadDirection.Left:
					travelCoord += Vector3.left;
					break;
				case PadDirection.Right:
					travelCoord += Vector3.right;
					break;
			}

			if (passiveDetect && padDirection == PadDirection.OnTop)
			{
				travelCoord += Vector3.up;
			}
		}

		public IEnumerator ServerAnimation()
		{
			spriteHandler.SetCatalogueIndexSprite(1);
			yield return WaitFor.Seconds(1f);
			spriteHandler.SetCatalogueIndexSprite(0);
			ServerSync(false);
		}

		private void HandleAudioOnTeleport()
		{
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.StealthOff, connectedPad.registerTile.LocalPosition, new AudioSourceParameters(maxDistance: 4f, spatialBlend:2));
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.StealthOff, registerTile.LocalPosition, new AudioSourceParameters(maxDistance: 4f, spatialBlend:2));
		}

		private void HandleTeleportation(CommonComponents reg)
		{
			if (reg.gameObject.TryGetComponent(out IQuantumReaction reaction))
			{
				reaction.OnTeleportStart();
				TransportUtility.TransportObjectAndPulled(reg.UniversalObjectPhysics, travelCoord);
				reaction.OnTeleportEnd();
			}
			else
			{
				TransportUtility.TransportObjectAndPulled(reg.UniversalObjectPhysics, travelCoord);
			}

			if (connectedPad && connectedPad.messageOnTravelToThis != "") Chat.AddExamineMsgFromServer(reg.gameObject, connectedPad.messageOnTravelToThis);
		}

		public void HandleTeleportationForTargetGameObject(GameObject target)
		{
			if (target == null || connectedPad == null) return;
			EnsureTravelCordsAreCorrect();
			var common = target.GetComponent<CommonComponents>();
			HandleTeleportation(common);
		}

		private void HandleLavalandFirstEnterEvent()
		{
			if (IsLavaLandBase1Connector == false || firstEnteredTriggered) return;
			EventManager.Broadcast(Event.LavalandFirstEntered);
			firstEnteredTriggered = true;
		}

		public enum PadDirection
		{
			OnTop,
			Up,
			Down,
			Left,
			Right
		}
	}
}
