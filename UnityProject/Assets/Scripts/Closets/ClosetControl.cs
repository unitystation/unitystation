﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


	public class ClosetControl : InputTrigger
	{
		private Sprite doorClosed;
		public Sprite doorOpened;

		//Inventory
		private IEnumerable<ObjectBehaviour> heldItems = new List<ObjectBehaviour>();

		private IEnumerable<ObjectBehaviour> heldPlayers = new List<ObjectBehaviour>();

		[SyncVar(hook = nameof(OpenClose))] public bool IsClosed;

		[SyncVar(hook = nameof(LockUnlock))] public bool IsLocked;
		public GameObject items;
		public LockLightController lockLight;
		
		private RegisterCloset registerTile;
		private Matrix matrix => registerTile.Matrix;

		public SpriteRenderer spriteRenderer;

		private void Awake()
		{
			doorClosed = spriteRenderer.sprite;
		}

		private void Start()
		{
			registerTile = GetComponent<RegisterCloset>();
		}

		public override void OnStartServer()
		{
			StartCoroutine(WaitForServerReg());
			base.OnStartServer();
		}

		private IEnumerator WaitForServerReg()
		{
			yield return new WaitForSeconds(1f);
			IsClosed = true;
			SetItems(!IsClosed);
		}

		public override void OnStartClient()
		{
			StartCoroutine(WaitForLoad());
			base.OnStartClient();
		}

		private IEnumerator WaitForLoad()
		{
			yield return new WaitForSeconds(3f);
			bool iC = IsClosed;
			bool iL = IsLocked;
			OpenClose(iC);
			LockUnlock(iL);
		}

		[Server]
		public void ServerToggleCupboard()
		{
			if (IsClosed)
			{
				if (lockLight != null)
				{
					if (lockLight.IsLocked())
					{
						IsLocked = false;
						return;
					}
					IsClosed = false;
					SetItems(true);
				}
				else
				{
					IsClosed = false;
					SetItems(true);
				}
			}
			else
			{
				IsClosed = true;
				SetItems(false);
			}
		}

		private void OpenClose(bool isClosed)
		{
			IsClosed = isClosed;
			if (isClosed)
			{
				Close();
			}
			else
			{
				Open();
			}
		}

		private void LockUnlock(bool lockIt)
		{
			IsLocked = lockIt;
			if (lockLight == null)
			{
				return;
			}
			if (lockIt)
			{
			}
			else
			{
				lockLight.Unlock();
			}
		}

		private void Close()
		{
			registerTile.IsClosed = true;
			SoundManager.PlayAtPosition("OpenClose", transform.position);
			spriteRenderer.sprite = doorClosed;
			if (lockLight != null)
			{
				lockLight.Show();
			}
		}

		private void Open()
		{
			registerTile.IsClosed = false;
			SoundManager.PlayAtPosition("OpenClose", transform.position);
			spriteRenderer.sprite = doorOpened;
			if (lockLight != null)
			{
				lockLight.Hide();
			}
		}
		[ContextMethod("Open/close","hand")]
		public void GUIInteract(){
			Interact(
				PlayerManager.LocalPlayerScript.gameObject,
				PlayerManager.LocalPlayerScript.WorldPos,
				UIManager.Instance.hands.CurrentSlot.eventName );
		}
		public override void Interact(GameObject originator, Vector3 position, string hand)
		{
			//FIXME this should be rewritten to net messages, see i.e. TableTrigger
			if (Input.GetKey(KeyCode.LeftControl))
			{
				return;
			}

			if (PlayerManager.PlayerInReach(transform))
			{
				if (IsClosed)
				{
					PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleCupboard(gameObject);
					return;
				}

				GameObject item = UIManager.Hands.CurrentSlot.Item;
				if (item != null)
				{
					Vector3 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
					targetPosition.z = 0f;
					PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPlaceItem(
						UIManager.Hands.CurrentSlot.eventName, transform.position, null, false);

					item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
				}
				else
				{
					PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleCupboard(gameObject);
				}
			}
		}

		private void SetItems(bool open)
		{
			if (!open)
			{
				SetItemsAliveState(false);
				SetPlayersAliveState(false);
			}
			else
			{
				SetItemsAliveState(true);
				SetPlayersAliveState(true);
			}
		}

		private void SetItemsAliveState(bool on)
		{
			if (!on)
			{
				heldItems = matrix.Get<ObjectBehaviour>(registerTile.Position, ObjectType.Item);
			}
			foreach (ObjectBehaviour item in heldItems)
			{
				CustomNetTransform netTransform = item.GetComponent<CustomNetTransform>();
				if (on)
				{
					netTransform.AppearAtPositionServer(transform.position);
//					item.transform.position = transform.position;
				}
				else
				{
					netTransform.DisappearFromWorldServer();
				}

				item.visibleState = on;
			}
		}

		private void SetPlayersAliveState(bool on)
		{
			if (!on)
			{
				heldPlayers = matrix.Get<ObjectBehaviour>(registerTile.Position, ObjectType.Player);
			}

			foreach (ObjectBehaviour player in heldPlayers)
			{
				if (on)
				{
					player.transform.position = transform.position;
					player.GetComponent<IPlayerSync>().SetPosition(transform.position);
				}
				player.visibleState = on;

				if (!on)
				{
					//Make sure a ClosetPlayerHandler is created on the client to monitor 
					//the players input inside the storage. The handler also controls the camera follow targets:
					if (!player.GetComponent<PlayerMove>().isGhost) {
						ClosetHandlerMessage.Send(player.gameObject, gameObject);
					}
				}
			}
		}
	}
