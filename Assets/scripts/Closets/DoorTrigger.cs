using Network;
using PlayGroup;
using UI;
using UnityEngine;

namespace Cupboards {

    public class DoorTrigger: Photon.PunBehaviour {

        public Sprite doorOpened;
        public Vector2 offsetOpened;
        public Vector2 sizeOpened;

        public bool IsClosed { get; private set; }

        private SpriteRenderer spriteRenderer;
        private Vector2 offsetClosed;
        private Vector2 sizeClosed;

        private Sprite doorClosed;
        private LockLightController lockLight;
        private GameObject items;


        void Start() {
            spriteRenderer = GetComponent<SpriteRenderer>();
            doorClosed = spriteRenderer.sprite;
            lockLight = transform.GetComponentInChildren<LockLightController>();

            offsetClosed = GetComponent<BoxCollider2D>().offset;
            sizeClosed = GetComponent<BoxCollider2D>().size;

            items = transform.FindChild("Items").gameObject;
            IsClosed = true;
        }

        void OnMouseDown() {
            if(PlayerManager.PlayerScript != null) {
                var headingToPlayer = PlayerManager.PlayerScript.transform.position - transform.position;
                var distance = headingToPlayer.magnitude;

                if(distance <= 2f) {
                    if(lockLight != null && lockLight.IsLocked()) {
                        photonView.RPC("LockLight", PhotonTargets.All);
                    } else {
                        if(IsClosed) { 
                            photonView.RPC("Open", PhotonTargets.All);
                        } else if(!TryDropItem()) {
                            photonView.RPC("Close", PhotonTargets.All);
                        }
                    }
                }
            }
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
                GetComponent<BoxCollider2D>().offset = offsetOpened;
                GetComponent<BoxCollider2D>().size = sizeOpened;

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
                GetComponent<BoxCollider2D>().offset = offsetClosed;
                GetComponent<BoxCollider2D>().size = sizeClosed;

                HideItems();
            }
        }

        [PunRPC]
        void LockLight() {
            if(lockLight != null) {
                lockLight.Unlock();
            }
        }

        [PunRPC]
        void DropItem(int itemViewID) {
            NetworkItemDB.Items[itemViewID].transform.parent = items.transform;
            NetworkItemDB.Items[itemViewID].transform.localPosition = new Vector3(0, 0, -0.2f);
        }

        private bool TryDropItem() {
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