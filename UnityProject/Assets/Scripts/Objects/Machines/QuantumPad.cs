using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using Gateway;
using Systems.Scenes;

namespace Objects.Science
{

	public class QuantumPad : NetworkBehaviour, IServerSpawn, ICheckedInteractable<HandApply>
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
				LavaLandManager.Instance.LavaLandBase1 = this;
			}

			if (IsLavaLandBase2)
			{
				LavaLandManager.Instance.LavaLandBase2 = this;
			}

			if (IsLavaLandBase1Connector)
			{
				LavaLandManager.Instance.LavaLandBase1Connector = this;
			}

			if (IsLavaLandBase2Connector)
			{
				LavaLandManager.Instance.LavaLandBase2Connector = this;
			}

			spriteHandler.ChangeSprite(0);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (!passiveDetect) return;

			UpdateManager.Add(ServerDetectObjectsOnTile, 1f);
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

			var message = connectedPad.messageOnTravelToThis;

			var registerTileLocation = registerTile.LocalPositionServer;

			var somethingTeleported = false;

			//Use the transport object code from StationGateway

			//detect players positioned on the portal bit of the gateway
			foreach (UniversalObjectPhysics player in Matrix.Get<UniversalObjectPhysics>(registerTileLocation, ObjectType.Player, true))
			{
				Chat.AddExamineMsgFromServer(player.gameObject, message);
				SoundManager.PlayNetworkedForPlayer(connectedPad.gameObject, CommonSounds.Instance.StealthOff);
				SoundManager.PlayNetworkedForPlayer(gameObject, CommonSounds.Instance.StealthOff);
				TransportUtility.TransportObjectAndPulled(player, travelCoord, true, maintRoomChanceModifier);
				somethingTeleported = true;

				if (IsLavaLandBase1Connector && firstEnteredTriggered == false)
				{
					//Trigger lavaland first entered event
					EventManager.Broadcast(Event.LavalandFirstEntered);
					firstEnteredTriggered = true;
				}
			}

			//detect objects and items
			foreach (var item in Matrix.Get<UniversalObjectPhysics>(registerTileLocation, ObjectType.Object, true)
									.Concat(Matrix.Get<UniversalObjectPhysics>(registerTileLocation, ObjectType.Item, true)))
			{
				//Don't teleport self lol
				if(item.gameObject == gameObject) continue;

				if (item.gameObject.TryGetComponent(out IQuantumReaction reaction))
				{
					reaction.OnTeleportStart();
					TransportUtility.TransportObjectAndPulled(item, travelCoord);
					reaction.OnTeleportEnd();
				}

				else
				{
					TransportUtility.TransportObjectAndPulled(item, travelCoord);
				}

				somethingTeleported = true;
			}

			if (!doingAnimation && passiveDetect && somethingTeleported)
			{
				ServerSync(true);

				StartCoroutine(ServerAnimation());
			}
		}

		public IEnumerator ServerAnimation()
		{
			spriteHandler.ChangeSprite(1);
			yield return WaitFor.Seconds(1f);
			spriteHandler.ChangeSprite(0);
			ServerSync(false);
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
