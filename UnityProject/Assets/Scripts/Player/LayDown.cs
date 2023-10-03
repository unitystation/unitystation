using HealthV2;
using Mirror;
using Player.Movement;
using UnityEngine;

namespace Player
{
	public class LayDown : NetworkBehaviour
	{
		[SerializeField] private Transform sprites;
		public Transform Sprites => sprites;
		[SerializeField] private LivingHealthMasterBase health;
		[SerializeField] private Rotatable playerDirectional;
		[SerializeField] private PlayerScript playerScript;
		[SerializeField] private Util.NetworkedLeanTween networkedLean;
		[SerializeField] private bool disabled = false;
		[SerializeField] private float clientAutoCorrectInterval = 1.35f;
		private readonly Quaternion layingDownRotation = Quaternion.Euler(0, 0, -90);
		private readonly Quaternion standingUp = Quaternion.Euler(0, 0, 0);

		[field: SyncVar] public bool IsLayingDown { get; private set; } = false;

		private void Awake()
		{
			playerScript ??= GetComponent<PlayerScript>();
			playerDirectional ??= GetComponent<Rotatable>();
			health ??= GetComponent<LivingHealthMasterBase>();
			networkedLean ??= GetComponent<Util.NetworkedLeanTween>();
			if(CustomNetworkManager.IsServer) UpdateManager.Add(AutoCorrect, clientAutoCorrectInterval);
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			ClientEnsureCorrectState();
		}

		[ClientRpc]
		private void AutoCorrect()
		{
			//(Max): This is a quick bandage for the ping issue related to laydown behaviors on the clients getting desynced at high pings.
			//It's still unclear why the state gets desynced at high pings, but this should stop bodies from standing up whenever
			//they're affected by wind or are thrown around while laying down.
			ClientEnsureCorrectState();
		}


		[Client]
		public void ClientEnsureCorrectState()
		{
			CorrectState();
		}

		[ClientRpc]
		public void ServerEnsureCorrectState()
		{
			CorrectState();
		}

		private void CorrectState()
		{
			gameObject.transform.rotation = Quaternion.Euler(0, 0, 0);
			if (disabled) return;
			var state = IsLayingDown;
			if (health != null)
			{
				if (health.IsDead || health.IsCrit)
				{
					state = true;
				}
			}
			if (state)
			{
				LayingDownLogic(true);
			}
			else
			{
				UpLogic(true);
			}
		}

		[ClientRpc]
		public void LayDownState(bool isLayingDown)
		{
			IsLayingDown = isLayingDown;
			if (isLayingDown)
			{
				LayingDownLogic();
			}
			else
			{
				UpLogic();
			}
			HandleGetupAnimation(isLayingDown == false);
		}

		private void LayingDownLogic(bool forceState = false)
		{
			if (forceState && sprites != null)
			{
				sprites.localRotation = layingDownRotation;
			}
			foreach (SpriteRenderer spriteRenderer in GetComponentsInChildren<SpriteRenderer>())
			{
				spriteRenderer.sortingLayerName = "Bodies";
			}
			playerScript.PlayerSync.CurrentMovementType  = MovementType.Crawling;
			playerDirectional.LockDirectionTo(true, playerDirectional.CurrentDirection);
			playerScript.OnLayDown?.Invoke();
		}

		private void UpLogic(bool forceState = false)
		{
			if (forceState && sprites != null)
			{
				sprites.localRotation = standingUp;
			}
			foreach (SpriteRenderer spriteRenderer in GetComponentsInChildren<SpriteRenderer>())
			{
				spriteRenderer.sortingLayerName = "Players";
			}
			if (playerScript == null || playerScript.PlayerSync == null) return;
			playerDirectional.LockDirectionTo(false, playerDirectional.CurrentDirection);
			playerScript.PlayerSync.CurrentMovementType = MovementType.Running;
		}

		private void HandleGetupAnimation(bool getUp)
		{
			if (getUp == false && networkedLean.Target.rotation.z > -90)
			{
				networkedLean.RotateGameObject(new Vector3(0, 0, -90), 0.15f, sprites.gameObject);
			}
			else if (getUp && networkedLean.Target.rotation.z < 90)
			{
				networkedLean.RotateGameObject(new Vector3(0, 0, 0), 0.19f, sprites.gameObject);
			}
		}
	}
}
