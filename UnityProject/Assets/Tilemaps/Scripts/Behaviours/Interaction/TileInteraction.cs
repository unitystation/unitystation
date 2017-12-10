using UnityEngine;
using UnityEngine.Networking;

namespace Tilemaps.Scripts.Behaviours.Interaction
{
    public abstract class TileInteraction
    {
        protected readonly GameObject gameObject;
        protected readonly GameObject originator;
        protected Vector3 position;
        protected readonly string hand;

        public TileInteraction(GameObject gameObject, GameObject originator, Vector3 position, string hand )
        {
            this.gameObject = gameObject;
            this.originator = originator;
            this.position = position;
            this.hand = hand;
        }

        public void Interact(bool isServer)
        {
            if (isServer)
            {
                ServerAction();
            }
            else
            {
                ClientAction();
            }
        }

        public abstract void ClientAction();
        
        [Server]
        public abstract void ServerAction();
    }
}