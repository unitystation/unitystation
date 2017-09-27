using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using PlayGroup;
using Matrix;
using Sprites;
using AccessType;

namespace Doors
{
	public class DoorController : NetworkBehaviour
	{
		public DoorType doorType;
		private RegisterTile registerTile;
		public Access restriction;
		[Tooltip("Does it have a glass window you can see trough?")]
		public bool isWindowedDoor = false;
		[Tooltip("how many sprites in the main door animation")]
		public int doorAnimationSize;
		[Tooltip("first frame of the door animation")]
		public int DoorSpriteOffset = 0;
		[Tooltip("first frame of the light animation")]
		public int DoorLightSpriteOffset = 0;
		[Tooltip("first frame of the door Cover/window animation")]
		public int DoorCoverSpriteOffset = 0;
		[HideInInspector]
		public bool isPerformingAction = false;
		public float maxTimeOpen = 5;
		public DoorAnimator doorAnimator;
		private bool openTrigger = false;
		private GameObject playerOpeningIt;
		private IEnumerator coWaitOpened;
		public AudioSource openSFX;
		public AudioSource closeSFX;


		private int closedLayer;
		private int openLayer;
		private int closedSortingLayer;
		private int openSortingLayer;
		private int doorDirection;
		public bool IsOpened;
		//public bool isWindowed = false;
		public enum OppeningDirection : int
		{
			Horizontal,
			Vertical
		};
		public OppeningDirection oppeningDirection;

		public override void OnStartClient()
		{
			base.OnStartClient();
			if (!isWindowedDoor) {
				closedLayer = LayerMask.NameToLayer("Door Closed");
			} else {
				closedLayer = LayerMask.NameToLayer("Windows");
			}
			closedSortingLayer = SortingLayer.NameToID("Doors Open");
			openLayer = LayerMask.NameToLayer("Door Open");
			openSortingLayer = SortingLayer.NameToID("Doors Closed");

			registerTile = gameObject.GetComponent<RegisterTile>();
		}

		public void BoxCollToggleOn()
		{
			registerTile.UpdateTileType(TileType.Door);
			gameObject.layer = closedLayer;
			GetComponentInChildren<SpriteRenderer>().sortingLayerID = closedSortingLayer;
		}

		public void BoxCollToggleOff()
		{
			registerTile.UpdateTileType(TileType.None);
			gameObject.layer = openLayer;
			GetComponentInChildren<SpriteRenderer>().sortingLayerID = openSortingLayer;
		}

		private IEnumerator WaitUntilClose()
		{
			// After the door opens, wait until it's supposed to close.
			yield return new WaitForSeconds(maxTimeOpen);
			if (isServer)
				CmdTryClose();
		}

		//3d sounds
		public void PlayOpenSound()
		{
			if (openSFX != null)
				openSFX.Play();
		}

		public void PlayCloseSound()
		{
			if (closeSFX != null)
				closeSFX.Play();
		}

		public void PlayCloseSFXshort()
		{
			if (closeSFX != null) {
				closeSFX.time = 0.6f;
				closeSFX.Play();
			}
		}

		[Command]
		public void CmdTryOpen(GameObject playerObj)
		{
			if (!IsOpened && !isPerformingAction) {
				RpcOpen(playerObj);

				ResetWaiting();
			}
		}

		[Command]
		public void CmdTryClose()
		{

			if (IsOpened && !isPerformingAction && Matrix.Matrix.At(transform.position).IsPassable()) {
				RpcClose();
			} else {
				ResetWaiting();
			}
		}
		[Command]
		public void CmdTryDenied()
		{
			if (!IsOpened && !isPerformingAction) {
				RpcAccessDenied();
			}
		}

		private void ResetWaiting()
		{
			if (coWaitOpened != null) {
				StopCoroutine(coWaitOpened);
				coWaitOpened = null;
			}

			coWaitOpened = WaitUntilClose();
			StartCoroutine(coWaitOpened);
		}

		void Update()
		{
			if (openTrigger) {
				float distToTriggerPlayer = Vector3.Distance(playerOpeningIt.transform.position, transform.position);
				if (distToTriggerPlayer < 1.5f) {
					openTrigger = false;
					OpenAction();
				}
			}
		}

		[ClientRpc]
		public void RpcAccessDenied()
		{

			if (!isPerformingAction) {
				doorAnimator.AccessDenied();
			}

		}

		[ClientRpc]
		public void RpcOpen(GameObject _playerOpeningIt)
		{
			if (_playerOpeningIt == null)
				return;

			openTrigger = true;
			playerOpeningIt = _playerOpeningIt;
		}

		public virtual void OpenAction()
		{
			IsOpened = true;

			if (!isPerformingAction) {
				doorAnimator.OpenDoor();
			}

		}

		[ClientRpc]
		public void RpcClose()
		{
			IsOpened = false;
			playerOpeningIt = null;
			if (!isPerformingAction) {
				doorAnimator.CloseDoor();
			}
		}
	}
}
