using Cupboards;
using Matrix;
using UnityEngine;
using UnityEngine.Networking;

namespace Objects
{
    public class ClosetHealthBehaviour : HealthBehaviour
    {
        private Collider2D[] colliders;
        private RegisterTile registerTile;
        private PushPull objectActions;
        private ClosetControl closetControl;

        private void Awake()
        {
            colliders = GetComponents<Collider2D>();
            registerTile = GetComponent<RegisterTile>();
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
            for ( var i = 0; i < colliders.Length; i++ ) colliders[i].enabled = false;

            objectActions.BreakPull();
            registerTile.UpdateTileType(TileType.None);
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
           transform.Rotate(0,0,90);
        }
    }
}