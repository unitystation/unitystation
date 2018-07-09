using System.Collections;
using Sprites;
using Tilemaps;
using Tilemaps.Behaviours.Layers;
using Tilemaps.Behaviours.Objects;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Doors
{
	public class DoorController : ManagedNetworkBehaviour
	{
		//public bool isWindowed = false;
		public enum OpeningDirection
		{
			Horizontal,
			Vertical
		}

		private int closedLayer;
		private int closedSortingLayer;
		public AudioSource closeSFX;
		private IEnumerator coWaitOpened;
		[Tooltip("how many sprites in the main door animation")] public int doorAnimationSize;
		public DoorAnimator doorAnimator;
		[Tooltip("first frame of the door Cover/window animation")] public int DoorCoverSpriteOffset;
		private int doorDirection;
		[Tooltip("first frame of the light animation")] public int DoorLightSpriteOffset;
		[Tooltip("first frame of the door animation")] public int DoorSpriteOffset;
		public DoorType doorType;

		public bool IsOpened;
		[HideInInspector] public bool isPerformingAction;
		[Tooltip("Does it have a glass window you can see trough?")] public bool isWindowedDoor;
		public float maxTimeOpen = 5;
		private int openLayer;
		public AudioSource openSFX;
		private int openSortingLayer;

		public OpeningDirection openingDirection;
		private RegisterDoor registerTile;
		private Matrix matrix => registerTile.Matrix;
		
		private AccessRestrictions accessRestrictions;
		public AccessRestrictions AccessRestrictions {
			get {
				if ( !accessRestrictions ) {
					accessRestrictions = GetComponent<AccessRestrictions>();
				}
				return accessRestrictions;
			}
		}

		[HideInInspector] public SpriteRenderer spriteRenderer;

		public override void OnStartClient()
		{
			base.OnStartClient();
			if (!isWindowedDoor)
			{
				closedLayer = LayerMask.NameToLayer("Door Closed");
			}
			else
			{
				closedLayer = LayerMask.NameToLayer("Windows");
			}
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
			closedSortingLayer = SortingLayer.NameToID("Doors Closed");
			openSortingLayer = SortingLayer.NameToID("Doors Open");
			openLayer = LayerMask.NameToLayer("Door Open");


			registerTile = gameObject.GetComponent<RegisterDoor>();
		}

		public void BoxCollToggleOn()
		{
			registerTile.IsClosed = true;
			gameObject.layer = closedLayer;
			spriteRenderer.sortingLayerID = closedSortingLayer;
		}

		public void BoxCollToggleOff()
		{
			registerTile.IsClosed = false;
			gameObject.layer = openLayer;
			spriteRenderer.sortingLayerID = openSortingLayer;
		}

		private IEnumerator WaitUntilClose()
		{
			// After the door opens, wait until it's supposed to close.
			yield return new WaitForSeconds(maxTimeOpen);
			if (isServer)
			{
				TryClose();
			}
		}

		//3d sounds
		public void PlayOpenSound()
		{
			if (openSFX != null)
			{
				openSFX.Play();
			}
		}

		public void PlayCloseSound()
		{
			if (closeSFX != null)
			{
				closeSFX.Play();
			}
		}

		public void PlayCloseSFXshort()
		{
			if (closeSFX != null)
			{
				closeSFX.time = 0.6f;
				closeSFX.Play();
			}
		}

		[Server]
		public void TryClose()
		{
			// Sliding door is not passable according to matrix
            if( IsOpened && !isPerformingAction && ( matrix.IsPassableAt( registerTile.Position ) || doorType == DoorType.sliding ) ) {
	            Close();
            }
			else
			{
				ResetWaiting();
			}
		}
		
		[Server]
		private void Close() {
			IsOpened = false;
			if ( !isPerformingAction ) {
				DoorUpdateMessage.SendToAll( gameObject, DoorUpdateType.Close );
			}
		}

		[Server]
		public void TryOpen(GameObject Originator, string hand)
		{
			if (AccessRestrictions != null)
			{
				if (AccessRestrictions.CheckAccess(Originator, hand)) {
					if (!IsOpened && !isPerformingAction) {
						Open();
					}
				}
				else {
					if (!IsOpened && !isPerformingAction) {
						AccessDenied();
					}
				}
			}
			else
			{
				Debug.LogError("Door lacks access restriction component!");
			}
		}
		[Server]
		private void AccessDenied() {
			if ( !isPerformingAction ) {
				DoorUpdateMessage.SendToAll( gameObject, DoorUpdateType.AccessDenied );
			}
		}

		[Server]
		private void Open() {
			ResetWaiting();
			IsOpened = true;

			if (!isPerformingAction)
			{
				DoorUpdateMessage.SendToAll( gameObject, DoorUpdateType.Open );
			}
		}

		private void ResetWaiting()
		{
			if (coWaitOpened != null)
			{
				StopCoroutine(coWaitOpened);
				coWaitOpened = null;
			}

			coWaitOpened = WaitUntilClose();
			StartCoroutine(coWaitOpened);
		}

		#region UI Mouse Actions

		public void OnMouseEnter()
		{
			UIManager.SetToolTip = doorType + " Door";
		}

		public void OnMouseExit()
		{
			UIManager.SetToolTip = "";
		}

		#endregion
	}
}