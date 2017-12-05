using Cupboards;
using Tilemaps.Scripts.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;

namespace Objects
{
    public class ClosetHealthBehaviour : HealthBehaviour
    {
        private Collider2D[] colliders;
        private RegisterCloset registerTile;
        private PushPull objectActions;
        private ClosetControl closetControl;

        private void Awake()
        {
            colliders = GetComponents<Collider2D>();
            registerTile = GetComponent<RegisterCloset>();
            objectActions = GetComponent<PushPull>();
            closetControl = GetComponent<ClosetControl>();
        }

        protected override void OnDeathActions()
        {
            if (isServer)
            {
                ServerDeathActions();
            }
        }

        [Server]
        private void ServerDeathActions()
        {
            //            disableInteraction();
            openCloset();
            RpcClientDeathActions();
        }

        [ClientRpc]
        private void RpcClientDeathActions()
        {
            disableInteraction(); //todo: refactor to use interaction bool w/ server validations
            playDeathSound();
            rotateSprites();
        }

        private void disableInteraction()
        {
            for (var i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

            objectActions.BreakPull();
            registerTile.IsClosed = false;
            objectActions.allowedToMove = false;
            objectActions.isPushable = false;
        }

        private void playDeathSound()
        {
            Instantiate(SoundManager.Instance["smash"], transform.position, Quaternion.identity).Play();
        }

        private void openCloset()
        {

            if (closetControl.IsClosed)
            {
                closetControl.ServerToggleCupboard();
            }
        }

        private void rotateSprites()
        {
            transform.Rotate(0, 0, 90);
        }
    }
}