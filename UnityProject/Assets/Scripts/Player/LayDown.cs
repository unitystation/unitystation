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
		private readonly Quaternion layingDownRotation = Quaternion.Euler(0, 0, -90);
		private readonly Quaternion standingUp = Quaternion.Euler(0, 0, 0);

		[SyncVar(hook = nameof(OnLayDown))] public bool IsLayingDown = false;

		private void Awake()
		{
			playerScript ??= GetComponent<PlayerScript>();
			playerDirectional ??= GetComponent<Rotatable>();
			health ??= GetComponent<LivingHealthMasterBase>();
			networkedLean ??= GetComponent<Util.NetworkedLeanTween>();
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
			if (disabled || health == null) return;
			if (IsLayingDown || health.IsCrit || health.IsDead)
			{
				LayingDownLogic(true);
			}
			else
			{
				UpLogic(true);
			}
		}

		public void OnLayDown(bool oldValue, bool newValue)
		{
			if (newValue)
			{
				LayingDownLogic();
			}
			else
			{
				UpLogic();
			}
			HandleGetupAnimation(newValue == false);
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
			if (CustomNetworkManager.IsServer == false) return;
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
			if (CustomNetworkManager.IsServer == false) return;
			playerDirectional.LockDirectionTo(false, playerDirectional.CurrentDirection);
			playerScript.PlayerSync.CurrentMovementType = MovementType.Running;
		}

		private void HandleGetupAnimation(bool getUp)
		{
			if (CustomNetworkManager.IsHeadless) return;
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
