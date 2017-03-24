using Network;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Cupboards {

    public class ClosetControl: NetworkBehaviour {

        public Sprite doorOpened;
        private Sprite doorClosed;

        private SpriteRenderer spriteRenderer;

        private LockLightController lockLight;
        private GameObject items;

        public bool IsClosed { get; private set; }

        void Start() {
            spriteRenderer = GetComponent<SpriteRenderer>();
            doorClosed = spriteRenderer.sprite;
            lockLight = transform.GetComponentInChildren<LockLightController>();
            items = transform.FindChild("Items").gameObject;
            IsClosed = true;
        }

        [Command]
		public void CmdOpen(){
			_Open();
			RpcOpen();
		}
        
		[ClientRpc]
		void RpcOpen(){
			_Open();
		}

		public void _Open() {
            if(IsClosed) {
                IsClosed = false;

                SoundManager.Play("OpenClose");

                spriteRenderer.sprite = doorOpened;
                if(lockLight != null) {
                    lockLight.Hide();
                }

                ShowItems();
            }
        }

        [Command]
		public void CmdClose(){
			_Close();
			RpcClose();
		} 

		[ClientRpc]
		void RpcClose(){
			_Close();
		}

		public void _Close() {
            if(!IsClosed) {
                IsClosed = true;

                SoundManager.Play("OpenClose");

                spriteRenderer.sprite = doorClosed;
                if(lockLight != null) {
                    lockLight.Show();
                }

                HideItems();
            }
        }

        [Command]
        public void CmdLockLight() {
            if(lockLight != null) {
                lockLight.Unlock();
            }
			RpcLockLight();
        }

		[ClientRpc]
		void RpcLockLight(){
			if(lockLight != null) {
				lockLight.Unlock();
			}
		}
			
		[Command]
		void CmdDropItem(NetworkInstanceId ID){
			RpcDropItem(ID);
		}

        //Add item to cupboard
		[ClientRpc]
		public void RpcDropItem(NetworkInstanceId itemID) {
			var item = NetworkItemDB.Items[itemID];
            item.transform.parent = items.transform;
            item.transform.localPosition = new Vector3(0, 0, item.transform.localPosition.z);

            //Tell the item that it is no longer in players inventory
            BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);

        }

        public bool TryDropItem() {
            GameObject item = UIManager.Hands.CurrentSlot.Clear();

            if(item != null) {
				NetworkInstanceId itemID = item.GetComponent<NetworkInstanceId>();
				CmdDropItem(itemID);
                return true;
            }
            return false;
        }

        private void ShowItems() {
            items.SetActive(true);
        }

        private void HideItems() {
            items.SetActive(false);
        }
    }
}