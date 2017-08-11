using InputControl;
using PlayGroup;
using UI;

namespace Items {
    public class PickUpTrigger: InputTrigger {
        public override void Interact() {
            if ( !isServer )
            {
                InteractMessage.Send(gameObject, UIManager.Hands.CurrentSlot.eventName);
            }
            else
            {
                if ( originator )
                {   //someone else tried to pick up
//                    originator.BroadcastMessage("TryToPickUpObject", gameObject);
                    originator.GetComponent<PlayerNetworkActions>().
                        TryToPickUpObject(gameObject, hand);
                }
                else
                {  //serverplayer picks something up himself
                   PlayerManager.LocalPlayerScript.playerNetworkActions.
                       TryToPickUpObject(gameObject);  
                }
            }
        }
    }
}
