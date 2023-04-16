using System.Collections;
using UnityEngine;
using Mirror;
using Systems.Explosions;
using AddressableReferences;

namespace Objects
{
	/// <summary>
	/// Spawns a drop pod. Falls from the "sky"!
	/// </summary>
	public class DropPod : NetworkBehaviour
	{
		private const int DROP_HEIGHT = 32; // How many tiles high the drop pod should spawn from the landing zone.
		private const int TRAVEL_TIME = 4; // How many seconds it takes to get from spawn point to landing zone.
		private const int EXPLOSION_STRENGTH = 10; // The strength of the explosion upon landing. 80 ~= soft crit.

		[Tooltip("Whether the drop-pod should spawn stationary or falling.")]
		[SerializeField]
		private bool spawnFalling = true;

		[SerializeField]
		private AddressableAudioSource RocketLand;

		[Header("SpriteHandlers")]
		[SerializeField]
		private SpriteHandler baseSpriteHandler = default;
		[SerializeField]
		private SpriteHandler doorSpriteHandler = default;
		[SerializeField]
		private SpriteHandler landingSpriteHandler = default;

		private RegisterObject registerObject;
		private ClosetControl closetControl;

		[SyncVar(hook = nameof(SyncIsLanding))]
		private bool isLanding = false;

		private bool IsServer => CustomNetworkManager.IsServer;

		private Vector3Int worldPosition;
		private Vector3Int WorldPosition
		{
			get
			{
				if (worldPosition == default)
				{
					worldPosition = gameObject.RegisterTile().WorldPosition;
				}
				return worldPosition;
			}
		}

		private enum BaseSprite
		{
			Stationary = 0,
			Falling = 1
		}

		#region Lifecycle

		private void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			closetControl = GetComponent<ClosetControl>();
		}

		private void Start()
		{
			if (spawnFalling)
			{
				isLanding = true;
				SyncIsLanding(isLanding, isLanding);
			}
		}

		#endregion Lifecycle

		private void SyncIsLanding(bool oldState, bool newState)
		{
			isLanding = newState;
			if (isLanding)
			{
				StartCoroutine(RunLandingSequence());
				StartCoroutine(DelayLandingSFX());
			}
		}

		private IEnumerator RunLandingSequence()
		{
			GameObject targetReticule = landingSpriteHandler.gameObject;
			GameObject dropPod = baseSpriteHandler.gameObject;

			// Initialise drop pod sprite to the start of falling animation.
			baseSpriteHandler.ChangeSprite((int)BaseSprite.Falling, false);
			registerObject.SetPassable(false, true);

			// ClosetControl initialises, redisplaying the door, so wait a frame...
			yield return WaitFor.EndOfFrame;
			doorSpriteHandler.PushClear(false);

			// Animate the drop-pod falling.
			dropPod.LeanScale(Vector3.one, TRAVEL_TIME).setFrom(Vector3.zero).setEaseInQuad();
			dropPod.LeanMoveLocalY(0, TRAVEL_TIME).setFrom(DROP_HEIGHT).setEaseInQuad();

			// Animate the target reticule.
			targetReticule.LeanScale(Vector3.one, TRAVEL_TIME / 2f).setFrom(new Vector3(1.5f, 1.5f, 1)).setEaseOutQuad();
			targetReticule.LeanRotateZ(-270, TRAVEL_TIME);
			targetReticule.LeanAlpha(0.75f, TRAVEL_TIME / 2f).setFrom(0.25f).setEaseOutQuad();

			yield return WaitFor.Seconds(TRAVEL_TIME);

			// Swap to stationary drop pod.
			baseSpriteHandler.ChangeSprite((int) BaseSprite.Stationary, false);
			doorSpriteHandler.PushTexture(false);
			landingSpriteHandler.PushClear(false);
			registerObject.SetPassable(false, false);

			// Create a small explosion to apply damage to objects underneath.
			var matrixInfo = MatrixManager.AtPoint(WorldPosition, IsServer);
			Explosion.StartExplosion(registerObject.WorldPositionServer, EXPLOSION_STRENGTH);

			isLanding = false;
		}

		private IEnumerator DelayLandingSFX()
		{
			yield return WaitFor.Seconds(TRAVEL_TIME - 1);
			_ = SoundManager.PlayAtPosition(RocketLand, WorldPosition, gameObject);
		}
	}
}
