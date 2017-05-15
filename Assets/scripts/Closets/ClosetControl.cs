using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Cupboards
{
    public class ClosetControl: NetworkBehaviour
    {
        public Sprite doorOpened;
        private Sprite doorClosed;

        public SpriteRenderer spriteRenderer;

        public LockLightController lockLight;
        public GameObject items;

        public bool IsClosed { get; private set; }

        void Start()
        {
            doorClosed = spriteRenderer.sprite;
            IsClosed = true;
        }

		public override void OnStartClient(){
			HideItems();
			base.OnStartClient();
		}
        //Called by server only
        public void ServerToggleCupboard(){
            RpcToggleCupboard();
        }

        [ClientRpc]
        void RpcToggleCupboard()
        {
            if (IsClosed){
                if (lockLight != null)
                {
                    if (lockLight.IsLocked())
                    {
                        lockLight.Unlock();
                        return;
                    }
                    Open();
                }
                else
                {
                    Open();
                }
            }
            else{
                Close();
            }
        }

        void Close()
        {
            IsClosed = true;
			SoundManager.PlayAtPosition("OpenClose",transform.position);
            spriteRenderer.sprite = doorClosed;
            if (lockLight != null)
            {
                lockLight.Show();
            }
            HideItems();
        }

        void Open()
        {
            IsClosed = false;
			SoundManager.PlayAtPosition("OpenClose",transform.position);
            spriteRenderer.sprite = doorOpened;
            if (lockLight != null)
            {
                lockLight.Hide();
            }
            ShowItems();
        }

        void OnMouseDown()
        {
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
                    PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPlaceItemCupB(UIManager.Hands.CurrentSlot.eventName, targetPosition, gameObject);

                    item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
                    //
                } else {
                    PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleCupboard(gameObject);
                }
            }
        }

        private void ShowItems()
        {
            items.SetActive(true);
        }

        private void HideItems()
        {
            items.SetActive(false);
        }
    }
}