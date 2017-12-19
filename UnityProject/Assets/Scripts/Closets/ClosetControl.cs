using System.Collections;
using System.Collections.Generic;
using PlayGroup;
using PlayGroups.Input;
using Tilemaps.Scripts;
using Tilemaps.Scripts.Behaviours.Objects;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Cupboards
{
    public class ClosetControl : InputTrigger
    {
        private Sprite doorClosed;
        public Sprite doorOpened;

        //Inventory
        private IEnumerable<ObjectBehaviour> heldItems = new List<ObjectBehaviour>();

        private IEnumerable<ObjectBehaviour> heldPlayers = new List<ObjectBehaviour>();

        [SyncVar(hook = "OpenClose")] public bool IsClosed;

        [SyncVar(hook = "LockUnlock")] public bool IsLocked;
        public GameObject items;
        public LockLightController lockLight;
        private Matrix matrix;
        private RegisterCloset registerTile;

        public SpriteRenderer spriteRenderer;

        private void Awake()
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

        private IEnumerator WaitForServerReg()
        {
            yield return new WaitForSeconds(1f);
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
                        UIManager.Hands.CurrentSlot.eventName, transform.position, null);

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
                if (on)
                {
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

            foreach (ObjectBehaviour player in heldPlayers)
            {
                if (on)
                {
                    player.transform.position = transform.position;
                }
                player.visibleState = on;
            }
        }
    }
}