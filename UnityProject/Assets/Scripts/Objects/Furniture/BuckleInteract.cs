using System.Linq;
using Player.Movement;
using UnityEngine;

namespace Objects
{
	/// <summary>
	/// Buckle a player in when they are dragged and dropped while on this object, then unbuckle
	/// them when the object is hand-applied to.
	/// </summary>
	public class BuckleInteract : MonoBehaviour, ICheckedInteractable<MouseDrop>, ICheckedInteractable<HandApply>,
		IServerLifecycle
	{
		//may be null
		private OccupiableDirectionalSprite occupiableDirectionalSprite;
		private Integrity integrity;

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

		private void Start()
		{
			occupiableDirectionalSprite = GetComponent<OccupiableDirectionalSprite>();
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
				var playerPushPull = playerScript.pushPull;
				if (side == NetworkSide.Server)
				{
					canPush = playerPushPull.CanPushServer((Vector3Int)playerWorldPos, dir);
				}
				else
				{
					canPush = playerPushPull.CanPushClient((Vector3Int)playerWorldPos, dir);
				}

				if (canPush == false) return false;
			}
			return true;
		}

		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (Validations.HasComponent<PlayerMove>(interaction.DroppedObject) == false) return false;

			var playerMove = interaction.DroppedObject.GetComponent<PlayerMove>();
			var registerPlayer = playerMove.GetComponent<RegisterPlayer>();
			// Determine if a push into the tile would be necessary or insufficient.

			if (allowImpassable == false && IsPushEnough(interaction, side, registerPlayer.PlayerScript, out _, out _) == false) return false;

			//if there are any restrained players already here, we can't restrain another one here
			if (MatrixManager.GetAt<PlayerMove>(interaction.TargetObject, side)
				.Any(pm => pm.IsBuckled))
			{
				return false;
			}

			//can't buckle during movement
			var playerSync = interaction.DroppedObject.GetComponent<PlayerSync>();
			if (playerSync.IsMoving) return false;

			//if the player to buckle is currently downed, we cannot buckle if there is another player on the tile
			//(because buckling a player causes the tile to become unpassable, thus a player could end up
			//occupying another player's space)
			//player to buckle is up, no need to check for other players on the tile
			if (registerPlayer.IsLayingDown == false) return true;

			//Player to buckle is down,
			//return false if there are any blocking players on this tile (because if we buckle this player
			//they would become blocking, and we can't have 2 blocking players on the same tile).
			return MatrixManager.GetAt<PlayerMove>(interaction.TargetObject, side)
				.Any(pm => pm != playerMove && pm.GetComponent<RegisterPlayer>().IsBlocking) == false;
		}

		public void ServerPerformInteraction(MouseDrop drop)
		{
			var playerScript = drop.UsedObject.GetComponent<PlayerScript>();

			IsPushEnough(drop, NetworkSide.Server, playerScript, out bool sameSquare, out Vector2Int dir);

			if (sameSquare == false)
			{
				playerScript.pushPull.QueuePush(dir, forcePush: allowImpassable);
			}

			BucklePlayer(playerScript);
		}

		/// <summary>
		/// Don't use it without proper validation!
		/// </summary>
		public void BucklePlayer(PlayerScript playerScript)
		{
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Click01, gameObject.WorldPosServer(), sourceObj: gameObject);

			playerScript.playerMove.ServerBuckle(gameObject, OnUnbuckle);

			//if this is a directional sprite, we render it in front of the player
			//when they are buckled
			occupiableDirectionalSprite?.SetOccupant(playerScript.netId);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject != gameObject) return false;
			//can only do this empty handed
			if (interaction.HandObject != null) return false;

			//can only do this if there is a buckled player here
			return MatrixManager.GetAt<PlayerMove>(interaction.TargetObject, side)
				.Any(pm => pm.IsBuckled);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Click01, interaction.TargetObject.WorldPosServer(), sourceObj: gameObject);

			Unbuckle();
		}

		/// <summary>
		/// Eject whoever is buckled to this
		/// </summary>
		public void Unbuckle()
		{
			if (CustomNetworkManager.IsServer == false)
			{
				return;
			}
			foreach (var playerMove in MatrixManager.GetAt<PlayerMove>(gameObject, NetworkSide.Server))
			{
				if (playerMove.IsBuckled)
				{
					playerMove.Unbuckle();
					return;
				}
			}
		}

		//delegate invoked from playerMove when they are unrestrained from this
		private void OnUnbuckle()
		{
			occupiableDirectionalSprite?.SetOccupant(NetId.Empty);
		}

		public void OnSpawnServer(SpawnInfo info) { }

		public void OnDespawnServer(DespawnInfo info)
		{
			Unbuckle();
		}
	}
}
