using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using InputControl;
using Matrix;

namespace Cupboards
{
    public class ClosetControl: InputTrigger
    {
        public Sprite doorOpened;
        private Sprite doorClosed;

        public SpriteRenderer spriteRenderer;
		private RegisterTile registerTile;
		private List<ItemControl> heldItems = new List<ItemControl>();

		[SyncVar(hook="LockUnlock")]
		public bool IsLocked;
        public LockLightController lockLight;
        public GameObject items;

		[SyncVar(hook="OpenClose")]
		public bool IsClosed;

        void Start()
        {
            doorClosed = spriteRenderer.sprite;
			registerTile = GetComponent<RegisterTile>();
            IsClosed = true;
        }

		public override void OnStartServer(){
            StartCoroutine(WaitForServerReg());
			base.OnStartServer();
		}

        IEnumerator WaitForServerReg()
        {
            yield return new WaitForSeconds(0.2f);
            SetItems(!IsClosed);
        }

        public override void OnStartClient()
        {
            StartCoroutine(WaitForLoad());
            base.OnStartClient();
        }

        IEnumerator WaitForLoad(){
			yield return new WaitForSeconds(0.2f);
			OpenClose(IsClosed);
		}

		[Server]
        public void ServerToggleCupboard(){
			if (IsClosed){
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
			else{
				IsClosed = true;
				SetItems(false);
			}
        }

		void OpenClose(bool isClosed){
			if (isClosed) {
				Close();
			} else {
				Open();
			}
		}

		void LockUnlock(bool lockIt){
			if (lockIt) {
			
			} else {
				lockLight.Unlock();
			}
		}

        void Close()
        {
			if (registerTile == null) {
				registerTile = gameObject.GetComponent<RegisterTile>();
			}
			registerTile.UpdateTileType(TileType.Object);
			SoundManager.PlayAtPosition("OpenClose",transform.position);
            spriteRenderer.sprite = doorClosed;
            if (lockLight != null)
            {
                lockLight.Show();
            }
        }

        void Open()
        {
			if (registerTile == null) {
				registerTile = gameObject.GetComponent<RegisterTile>();
			}
			registerTile.UpdateTileType(TileType.None);
			SoundManager.PlayAtPosition("OpenClose",transform.position);
            spriteRenderer.sprite = doorOpened;
            if (lockLight != null)
            {
                lockLight.Hide();
            }
        }

		public override void Interact(){
			if (Input.GetKey(KeyCode.LeftControl))
				return;

			if (PlayerManager.PlayerInReach(transform))
			{
				if (IsClosed)
				{
					PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleCupboard(gameObject);
					return;
				}

				GameObject item = UIManager.Hands.CurrentSlot.PlaceItemInScene();
				if (item != null)
				{
					var targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
					targetPosition.z = -0.2f;
					PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPlaceItem(UIManager.Hands.CurrentSlot.eventName, registerTile.editModeControl.Snap(transform.position), null);

					item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
					//
				} else {
					PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleCupboard(gameObject);
				}
			}
		}

		private void SetItems(bool open){

			if (!open) {
				heldItems = Matrix.Matrix.At(registerTile.editModeControl.Snap(transform.position)).GetItems();
//				Debug.Log("Get items at pos: " + registerTile.editModeControl.Snap(transform.position));
				ItemControl[] tempList = heldItems.ToArray();
				for (int i = 0; i < tempList.Length; i++) {
					tempList[i].aliveState = false;
				}
			} else {
				ItemControl[] tempList = heldItems.ToArray();
				for (int i = 0; i < tempList.Length; i++) {
					tempList[i].transform.position = transform.position;
					tempList[i].aliveState = true;
				}
			}
		}
    }
}