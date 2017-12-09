using Items;
using UI;
using UnityEngine;

namespace PlayGroups.Input.Triggers
{
    public class ScrewdriverTrigger : PickUpTrigger
    {

        private Headset headset;

        void Start()
        {
            headset = GetComponent<Headset>();
        }

        public override void Interact(GameObject originator, Vector3 position, string hand)
        {
			Debug.Log("in headset interact");
            var item = UIManager.Hands.CurrentSlot.Item;

            if (item && item.GetComponent<Scewdriver>()) {
				RemoveEncryptionKeyMessage.Send(gameObject);
            }
        }
    }
}