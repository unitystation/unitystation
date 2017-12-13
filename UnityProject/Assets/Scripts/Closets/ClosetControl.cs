using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using InputControl;
using PlayGroups.Input;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours.Objects;

namespace Cupboards
{
    public class ClosetControl : InputTrigger
    {
        public Sprite doorOpened;
        private Sprite doorClosed;

        public SpriteRenderer spriteRenderer;
        private RegisterCloset registerTile;
        private Matrix matrix;

        [SyncVar(hook = "LockUnlock")]
        public bool IsLocked;
        public LockLightController lockLight;
        public GameObject items;

        [SyncVar(hook = "OpenClose")]
        public bool IsClosed;

        //Inventory
        private IEnumerable<ObjectBehaviour> heldItems = new List<ObjectBehaviour>();
        private IEnumerable<ObjectBehaviour> heldPlayers = new List<ObjectBehaviour>();

        void Awake()
        {
            doorClosed = spriteRenderer.sprite;
        }

        private void Start()
        {
            registerTile = GetComponent<RegisterCloset>();
            matrix = Matrix.GetMatrix(this);
        }

        public override void OnStartServer()
        {
            StartCoroutine(WaitForServerReg());
            IsClosed = true;
            base.OnStartServer();
        }

        IEnumerator WaitForServerReg()
        {
            yield return new WaitForSeconds(1f);
            SetItems(!IsClosed);
        }

        public override void OnStartClient()
        {
            StartCoroutine(WaitForLoad());
            base.OnStartClient();
        }

        IEnumerator WaitForLoad()
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

        void OpenClose(bool isClosed)
        {
            if (isClosed)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        void LockUnlock(bool lockIt)
        {
            if (lockLight == null)
                return;
            if (lockIt)
            {

            }
            else
            {
                lockLight.Unlock();
            }
        }

        void Close()
        {
            registerTile.IsClosed = true;
            SoundManager.PlayAtPosition("OpenClose", transform.position);
            spriteRenderer.sprite = doorClosed;
            if (lockLight != null)
            {
                lockLight.Show();
            }
        }

        void Open()
        {
            registerTile.IsClosed = false;
            SoundManager.PlayAtPosition("OpenClose", transform.position);
            spriteRenderer.sprite = doorOpened;
            if (lockLight != null)
            {
                lockLight.Hide();
            }
        }

        public override void Interact(GameObject originator, Vector3 position, string hand)
        {
            //FIXME this should be rewritten to net messages, see i.e. TableTrigger
            if (Input.GetKey(KeyCode.LeftControl))
                return;

            if (PlayerManager.PlayerInReach(transform))
            {
                if (IsClosed)
                {
                    PlayerManager.LocalPlayerScript.playerNetworkActions.CmdToggleCupboard(gameObject);
                    return;
                }

                GameObject item = UIManager.Hands.CurrentSlot.Clear();
                if (item != null)
                {
                    var targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    targetPosition.z = 0f;
                    PlayerManager.LocalPlayerScript.playerNetworkActions.PlaceItem(UIManager.Hands.CurrentSlot.eventName, transform.position, null);

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
            foreach (var item in heldItems)
            {
                if (on) {
					item.transform.position = transform.position;
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

            foreach (var player in heldPlayers)
            {
                if (on)
                    player.transform.position = transform.position;
                player.visibleState = on;
            }
        }
    }
}
