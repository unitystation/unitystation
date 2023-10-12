using System.Collections;
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

		[SyncVar(hook = nameof(SyncLayDownState))] private bool isLayingDown= false;

		public bool IsLayingDown => isLayingDown;

		private UprightSprites UprightSprites = null;

		private void Awake()
		{
			UprightSprites = this.GetComponentCustom<UprightSprites>();
			playerScript ??= GetComponent<PlayerScript>();
			playerDirectional ??= GetComponent<Rotatable>();
			health ??= GetComponent<LivingHealthMasterBase>();
			networkedLean ??= GetComponent<Util.NetworkedLeanTween>();
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			EnsureCorrectState();
		}

		public void EnsureCorrectState()
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

		public void SyncLayDownState(bool OldState, bool NewState)
		{
			isLayingDown = NewState;
			if (NewState)
			{
				LayingDownLogic();
			}
			else
			{
				UpLogic();
			}
			StartCoroutine(HandleGetupAnimation(NewState == false));
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
			if (playerScript == null || playerScript.PlayerSync == null) return;
			if (CustomNetworkManager.IsServer == false) return;
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
			if (CustomNetworkManager.IsServer == false) return;
			playerDirectional.LockDirectionTo(false, playerDirectional.CurrentDirection);
			playerScript.PlayerSync.CurrentMovementType = MovementType.Running;
		}

		private IEnumerator HandleGetupAnimation(bool getUp)
		{
			if (getUp == false && networkedLean.Target.rotation.z > -90)
			{
				networkedLean.RotateGameObject(new Vector3(0, 0, -90), 0.15f, sprites.gameObject);
				yield return WaitFor.Seconds(0.15f);
				UprightSprites.ExtraRotation = Quaternion.Euler(new Vector3(0, 0, -90));

			}
			else if (getUp && networkedLean.Target.rotation.z < 90)
			{

				networkedLean.RotateGameObject(new Vector3(0, 0, 0), 0.19f, sprites.gameObject);
				yield return WaitFor.Seconds(0.19f);
				UprightSprites.ExtraRotation = Quaternion.Euler(new Vector3(0, 0, 0));
			}
		}
	}
}
