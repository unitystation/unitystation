using System;
using System.Linq;
using HealthV2;
using UnityEngine;
namespace Objects
{
	/// <summary>
	/// Buckle a player in when they are dragged and dropped while on this object, then unbuckle
	/// them when the object is hand-applied to.
	/// </summary>
	public class BuckleInteract : MonoBehaviour, ICheckedInteractable<MouseDrop>, ICheckedInteractable<HandApply>,
		IServerDespawn
	{
		//may be null, catalogue index 0 is the occupied sprite
		[SerializeField]
		private SpriteHandler occupiedSpriteHandler;
		private Integrity integrity;

		///<summary>
		/// Event that get invoked when a player Buckles into an object that has this component
		///</summary>
		public event Action OnBuckleEvent;
		///<summary>
		/// Event that get invoked when a player Unbuckles into an object that has this component
		///</summary>
		public event Action OnUnbuckleEvent;

		/// <summary>
		/// Do the resist even if uncuffed, e.g alien nest
		/// </summary>
		[SerializeField]
		private bool doResistUncuffed = false;
		public bool DoResistUncuffed => doResistUncuffed;

		/// <summary>
		/// The time that a mob will spend trying to unbuckle himself from a chair when he is handcuffed.
		/// </summary>
		[SerializeField]
		private float resistTime = 60;
		public float ResistTime => resistTime;

		public bool forceLayingDown;

		[SerializeField]
		[Tooltip("Whether the object you can trying to buckle to is impassable, therefore should bypass the push check")]
		private bool allowImpassable;

		private RegisterTile registerTile;
		private UniversalObjectPhysics objectPhysics;

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();
		}

		private bool IsPushEnough(MouseDrop interaction, NetworkSide side, PlayerScript playerScript, out bool sameSquare, out Vector2Int dir)
		{
			Vector2Int playerWorldPos = interaction.DroppedObject.TileWorldPosition();
			Vector2Int targetWorldPos = interaction.TargetObject.TileWorldPosition();
			dir = targetWorldPos - playerWorldPos;
			sameSquare = true;

			if (Validations.ObjectsAtSameTile(interaction.DroppedObject, interaction.TargetObject) == false)
			{
				sameSquare = false;

				bool canPush = false;
				var playerPushPull = playerScript.ObjectPhysics;
				if (side == NetworkSide.Server)
				{
					canPush = playerPushPull.CanPush(dir);
				}
				else
				{
					canPush = playerPushPull.CanPush(dir);
				}

				if (canPush == false) return false;
			}
			return true;
		}

		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side,
					Validations.CheckState(x => x.CanBuckleOthers), AllowTelekinesis: false) == false) return false;

			if (Validations.HasComponent<MovementSynchronisation>(interaction.DroppedObject) == false) return false;

			var playerMove = interaction.DroppedObject.GetComponent<MovementSynchronisation>();
			var registerPlayer = playerMove.GetComponent<RegisterPlayer>();

			// Determine if a push into the tile would be necessary or insufficient.
			if (allowImpassable == false && IsPushEnough(interaction, side, registerPlayer.PlayerScript, out _, out _) == false) return false;

			//if there are any restrained players already here, we can't restrain another one here
			if (MatrixManager.GetAt<MovementSynchronisation>(interaction.TargetObject, side)
				.Any(pm => pm.IsBuckled)) return false;

			//can't buckle during movement
			var playerSync = interaction.DroppedObject.GetComponent<MovementSynchronisation>();
			if (playerSync.IsMoving) return false;

			//if the player to buckle is currently downed, we cannot buckle if there is another player on the tile
			//(because buckling a player causes the tile to become unpassable, thus a player could end up
			//occupying another player's space)
			//player to buckle is up, no need to check for other players on the tile
			if (registerPlayer.IsLayingDown == false) return true;

			//Player to buckle is down,
			//return false if there are any blocking players on this tile (because if we buckle this player
			//they would become blocking, and we can't have 2 blocking players on the same tile).
			return MatrixManager.GetAt<MovementSynchronisation>(interaction.TargetObject, side)
				.Any(pm => pm != playerMove && pm.GetComponent<RegisterPlayer>().IsBlocking) == false;
		}

		public void ServerPerformInteraction(MouseDrop drop)
		{
			var playerScript = drop.UsedObject.GetComponent<PlayerScript>();

			IsPushEnough(drop, NetworkSide.Server, playerScript, out bool sameSquare, out Vector2Int dir);

			if (sameSquare == false)
			{
				playerScript.ObjectPhysics.AppearAtWorldPositionServer(transform.position);
			}

			BucklePlayer(playerScript);
		}

		/// <summary>
		/// Don't use it without proper validation!
		/// </summary>
		public void BucklePlayer(PlayerScript playerScript)
		{
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Click01, gameObject.AssumedWorldPosServer(), sourceObj: gameObject);

			if (forceLayingDown && playerScript.RegisterPlayer.IsLayingDown == false) playerScript.RegisterPlayer.ServerSetIsStanding(false);		

			objectPhysics.BuckleTo(playerScript.playerMove);
			occupiedSpriteHandler.OrNull()?.ChangeSprite(0);
			OnBuckleEvent?.Invoke();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side, Validations.CheckState(x => x.CanBuckleOthers)) == false) return false;
			if (interaction.TargetObject != gameObject) return false;
			//can only do this empty handed
			if (interaction.HandObject != null) return false;

			//can only do this if there is a buckled player here
			return MatrixManager.GetAt<MovementSynchronisation>(interaction.TargetObject, side)
				.Any(pm => pm.IsBuckled);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Click01, interaction.TargetObject.AssumedWorldPosServer(), sourceObj: gameObject);

			UnbuckleAll();
		}

		/// <summary>
		/// Eject whoever is buckled to this
		/// </summary>
		public void UnbuckleAll()
		{
			if (CustomNetworkManager.IsServer == false) return;

			foreach (var playerMove in MatrixManager.GetAt<MovementSynchronisation>(gameObject, NetworkSide.Server))
			{
				if (playerMove.IsBuckled)
				{
					UnBuckle(playerMove.playerScript);
					return;
				}
			}
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			UnbuckleAll();
		}

		public void TryUnbuckle(PlayerScript playerScript)
		{
			if (playerScript.PlayerSync.IsCuffed || doResistUncuffed)
			{
				if (CanUnBuckleSelf(playerScript))
				{
					Chat.AddActionMsgToChat(
						playerScript.gameObject,
						$"You start trying to unbuckle yourself from the {gameObject.ExpensiveName()}! (this will take some time...)",
						$"{playerScript.visibleName} is trying to unbuckle themself from the {gameObject.ExpensiveName()}!"
					);

					StandardProgressAction.Create(
						new StandardProgressActionConfig(StandardProgressActionType.Unbuckle),
						() =>
						{
							UnBuckle(playerScript);
						}).ServerStartProgress(registerTile, resistTime, playerScript.gameObject);

				}
			}
			else
			{
				UnBuckle(playerScript);
			}
		}

		private void UnBuckle(PlayerScript playerScript)
		{
			playerScript.PlayerSync.Unbuckle();
			objectPhysics.Unbuckle();
			occupiedSpriteHandler.OrNull()?.PushClear();
			OnUnbuckleEvent?.Invoke();
		}

		private bool CanUnBuckleSelf(PlayerScript playerScript)
		{
			PlayerHealthV2 playerHealth = playerScript.playerHealth;

			if (playerHealth == null) return false;

			if (playerHealth.ConsciousState == ConsciousState.DEAD) return false;

			if (playerHealth.ConsciousState == ConsciousState.UNCONSCIOUS) return false;

			if (playerHealth.ConsciousState == ConsciousState.BARELY_CONSCIOUS) return false;

			return true;
		}
	}
}
