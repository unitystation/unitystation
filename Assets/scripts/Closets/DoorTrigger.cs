using Network;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

namespace Cupboards {
    public class DoorTrigger: Photon.PunBehaviour {

        public Sprite doorOpened;
        public Vector2 offsetOpened;
        public Vector2 sizeOpened;

        private SpriteRenderer spriteRenderer;
        private Vector2 offsetClosed;
        private Vector2 sizeClosed;

        private Sprite doorClosed;
        private LockLightController lockLight;
        private GameObject items;

        private bool synced = false;

        private bool closed = true;

        void Start() {
            spriteRenderer = GetComponent<SpriteRenderer>();
            doorClosed = spriteRenderer.sprite;
            lockLight = transform.GetComponentInChildren<LockLightController>();

            offsetClosed = GetComponent<BoxCollider2D>().offset;
            sizeClosed = GetComponent<BoxCollider2D>().size;

            items = transform.FindChild("Items").gameObject;

            if(PhotonNetwork.connectedAndReady) {
                //Has been instantiated at runtime and you received instantiate of this object from photon on room join
                StartSync();
            }
        }

        void OnMouseDown() {
            if(PlayerManager.PlayerScript != null) {
                var headingToPlayer = PlayerManager.PlayerScript.transform.position - transform.position;
                var distance = headingToPlayer.magnitude;

                if(distance <= 2f) {
                    if(lockLight != null && lockLight.IsLocked()) {
                        photonView.RPC("LockLight", PhotonTargets.All, null);
                    } else {
                        SoundManager.control.Play("OpenClose");
                        if(closed) {
                            photonView.RPC("Open", PhotonTargets.All, null);
                        } else if(!TryDropItem()) {
                            photonView.RPC("Close", PhotonTargets.All, null);
                        }
                    }
                }
            }
        }


        [PunRPC]
        void Open() {
            closed = false;
            spriteRenderer.sprite = doorOpened;
            if(lockLight != null) {
                lockLight.Hide();
            }
            GetComponent<BoxCollider2D>().offset = offsetOpened;
            GetComponent<BoxCollider2D>().size = sizeOpened;

            ShowItems();
        }

        [PunRPC]
        void Close() {
            closed = true;
            spriteRenderer.sprite = doorClosed;
            if(lockLight != null) {
                lockLight.Show();
            }
            GetComponent<BoxCollider2D>().offset = offsetClosed;
            GetComponent<BoxCollider2D>().size = sizeClosed;

            HideItems();
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

        //PUN Sync
        [PunRPC]
        void SendCurrentState(string playerRequesting) {
            if(PhotonNetwork.isMasterClient) {
                PhotonView[] objectsInCupB = items.GetPhotonViewsInChildren();
                if(objectsInCupB != null) {
                    foreach(PhotonView p in objectsInCupB) {
                        photonView.RPC("DropItem", PhotonTargets.Others, p.viewID); //Make sure all spawneditems are where they should be on new player join
                    }
                }

                if(lockLight != null) {
                    photonView.RPC("ReceiveCurrentState", PhotonTargets.Others, playerRequesting, lockLight.IsLocked(), closed, transform.parent.transform.position); //Gather the values and send back
                } else {
                    photonView.RPC("ReceiveCurrentState", PhotonTargets.Others, playerRequesting, false, closed, transform.parent.transform.position); //Gather the values and send back 
                }
            }
        }

        [PunRPC]
        void ReceiveCurrentState(string playerIdent, bool isLocked, bool isClosed, Vector3 pos) {
            if(PhotonNetwork.player.NickName == playerIdent) {
                if(isClosed) {
                    Close();

                } else {
                    Open();
                }

                if(lockLight != null) {
                    if(isLocked != lockLight.IsLocked()) // Locked or unlocked
                    {

                        if(isLocked) {
                            lockLight.Lock();
                        } else {
                            lockLight.Unlock();
                        }
                    }
                }

                if(transform.parent.transform.position != pos) //Position of cupboard
                {
                    transform.parent.transform.position = pos;
                }
            }
        }

        //PUN Callbacks

        public override void OnJoinedRoom() {
            StartSync();
        }

        void StartSync() {
            if(!synced) {
                if(!PhotonNetwork.isMasterClient) {
                    //If you are not the master then update the current IG state of this object from the master
                    photonView.RPC("SendCurrentState", PhotonTargets.MasterClient, PhotonNetwork.player.NickName);
                }

                NetworkItemDB.AddCupboard(photonView.viewID, this);
                synced = true;
            }
        }


    }
}

//TODOS
//TODO update the transform through photonView if it is changed