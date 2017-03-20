using Network;
using PlayGroup;
using UI;
using UnityEngine;

namespace Cupboards {

    public class ClosetControl: Photon.PunBehaviour {

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

        [PunRPC]
        public void Open() {
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

        [PunRPC]
        public void Close() {
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

        [PunRPC]
        void LockLight() {
            if(lockLight != null) {
                lockLight.Unlock();
            }
        }

        //Add item to cupboard
        [PunRPC]
        void DropItem(int itemViewID) {
            var item = NetworkItemDB.Items[itemViewID];
            item.transform.parent = items.transform;
            item.transform.localPosition = new Vector3(0, 0, item.transform.localPosition.z);

            //Tell the item that it is no longer in players inventory
            BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);

        }

        public bool TryDropItem() {
            GameObject item = UIManager.Hands.CurrentSlot.Clear();

            if(item != null) {
                //TODO add to all cupboards on all clients
                PhotonView itemView = item.GetComponent<PhotonView>();
                photonView.RPC("DropItem", PhotonTargets.All, itemView.viewID);

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