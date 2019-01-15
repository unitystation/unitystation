using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;


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
		[Tooltip("how many sprites in the main door animation")] public int doorAnimationSize = 6;
		public DoorAnimator doorAnimator;
		[Tooltip("first frame of the light animation")] public int DoorDeniedSpriteOffset = 12;
		[Tooltip("first frame of the door Cover/window animation")] public int DoorCoverSpriteOffset;
		private int doorDirection;
		[Tooltip("first frame of the light animation")] public int DoorLightSpriteOffset;
		[Tooltip("first frame of the door animation")] public int DoorSpriteOffset;
		public DoorType doorType;

		public bool IsOpened;
		[HideInInspector] public bool isPerformingAction;
		[Tooltip("Does it have a glass window you can see trough?")] public bool isWindowedDoor;
		[Tooltip("Does the door light animation only need 1 frame?")] public bool useSimpleLightAnimation = false;
		[Tooltip("Does the denied light animation only toggle 1 frame on and?")] public bool useSimpleDeniedAnimation = false;
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

		/// <summary>
		/// Invoked by doorAnimator once a door animation finishes
		/// </summary>
		public void OnAnimationFinished()
		{
			isPerformingAction = false;
			//check if the door is closing on something, and reopen it if so.

			//When the door first closes, it checks if anything is blocking it, but it is still possible
			//for a laggy client to go into the door while it is closing. There are 2 cases:
			// 1. Client enters door after server knows the door is impassable, but before client knows it is impassable.
			// 2. Client enters door after the close begins but before server marks the door as impassable and before
			// 		the client knows it is impassable. This is rare but there is a slight delay (.15 s) between when the door close
			//		begins and when the server registers the door as impassable, so it is possible (See AirLockAnimator.MakeSolid)
			// Case 1 is handled by our rollback code - the client will be lerp'd back to their previous position.
			// Case 2 won't be handled by the rollback code because the client enters the passable tile while the
			//	server still thinks its passable. So, for the rare situation that case 2 occurs, we will apply
			// the below logic and reopen the door if the client got stuck in the door in the .15 s gap.

			//only do this check when door is closing, and only for doors that block all directions (like airlocks)
			if (isServer && !IsOpened && !registerTile.OneDirectionRestricted)
			{
				if (!MatrixManager.IsPassableAt(registerTile.WorldPosition, registerTile.WorldPosition, true,
					this.gameObject))
				{
					//something is in the way, open back up
					//set this field to false so open command will actually work
					isPerformingAction = false;
					Open();
				}
			}

		}

		public void BoxCollToggleOn()
		{
			registerTile.IsClosed = true;

			SetLayer(closedLayer);

			spriteRenderer.sortingLayerID = closedSortingLayer;
		}

		public void BoxCollToggleOff()
		{
			registerTile.IsClosed = false;

			SetLayer(openLayer);

			spriteRenderer.sortingLayerID = openSortingLayer;
		}

		private void SetLayer(int layer)
		{
			gameObject.layer = layer;
			foreach (Transform child in transform)
			{
				child.gameObject.layer = layer;
			}
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
		public void Close() {
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
				Logger.LogError("Door lacks access restriction component!", Category.Doors);
			}
		}
		[Server]
		private void AccessDenied() {
			if ( !isPerformingAction ) {
				DoorUpdateMessage.SendToAll( gameObject, DoorUpdateType.AccessDenied );
			}
		}

		[Server]
		public void Open() {
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

		public void OnHoverStart()
		{
			UIManager.SetToolTip = doorType + " Door";
		}

		public void OnHoverEnd()
		{
			UIManager.SetToolTip = "";
		}

		#endregion
	}
