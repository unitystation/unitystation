using System.Collections;
using Sprites;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using Tilemaps.Scripts;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Doors
{
	public class DoorController : ManagedNetworkBehaviour
	{
		//public bool isWindowed = false;
		public enum OppeningDirection
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

		//TODO: useful tooltip
		public bool FullDoor = true;

		public bool IsOpened;
		[HideInInspector] public bool isPerformingAction;
		[Tooltip("Does it have a glass window you can see trough?")] public bool isWindowedDoor;
		public float maxTimeOpen = 5;
		private int openLayer;
		public AudioSource openSFX;
		private int openSortingLayer;
		private bool openTrigger;

		public OppeningDirection oppeningDirection;
		private GameObject playerOpeningIt;
		private RegisterDoor registerTile;
		private Matrix matrix => registerTile.Matrix;

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
				CmdTryClose();
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

		[Command]
		public void CmdTryOpen(GameObject playerObj)
		{
			if (!IsOpened && !isPerformingAction)
			{
				RpcOpen(playerObj);

				ResetWaiting();
			}
		}

		[Command]
		public void CmdTryClose()
		{
			if (!FullDoor && IsOpened)
			{
				RpcClose();
				return;
			}

			if (IsOpened && !isPerformingAction && matrix.IsPassableAt(registerTile.Position))
			{
				RpcClose();
			}
			else
			{
				ResetWaiting();
			}
		}

		[Command]
		public void CmdTryDenied()
		{
			if (!IsOpened && !isPerformingAction)
			{
				RpcAccessDenied();
			}
		}

		// How the client attempts to open the door. If there is no AccessRestrictions component, it returns an error and everything goes about its business.
		[Command]
		public void CmdCheckDoorPermissions(GameObject Door, GameObject Originator)
		{
			if (Door.GetComponent<AccessRestrictions>() != null)
			{
				if (Door.GetComponent<AccessRestrictions>().CheckAccess(Originator, Door))
				{
					CmdTryOpen(Originator);
				}
				else
				{
					CmdTryDenied();
				}
			}
			else
			{
				Debug.LogError("Door lacks access restriction component!");
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

		public override void UpdateMe()
		{
			if (openTrigger && playerOpeningIt)
			{
				float distToTriggerPlayer = Vector3.Distance(playerOpeningIt.transform.position, transform.position);
				if (distToTriggerPlayer < 1.5f)
				{
					openTrigger = false;
					OpenAction();
				}
			}
		}

		[ClientRpc]
		public void RpcAccessDenied()
		{
			if (!isPerformingAction)
			{
				doorAnimator.AccessDenied();
			}
		}

		[ClientRpc]
		public void RpcOpen(GameObject _playerOpeningIt)
		{
			if (_playerOpeningIt == null)
			{
				return;
			}

			openTrigger = true;
			playerOpeningIt = _playerOpeningIt;
		}

		public virtual void OpenAction()
		{
			IsOpened = true;

			if (!isPerformingAction)
			{
				doorAnimator.OpenDoor();
			}
		}

		[ClientRpc]
		public void RpcClose()
		{
			IsOpened = false;
			playerOpeningIt = null;
			if (!isPerformingAction)
			{
				doorAnimator.CloseDoor();
			}
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