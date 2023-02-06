using UnityEngine;

namespace Items
{
	public class LastTouch : MonoBehaviour
	{
		public PlayerInfo LastTouchedBy { get; set; }
		private Pickupable pickupable;

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
			pickupable.OnMoveToPlayerInventory.AddListener(SetLastTouch);
		}

		private void SetLastTouch(GameObject player)
		{
			if (pickupable.ItemSlot.Player == null) return;
			LastTouchedBy = pickupable.ItemSlot.Player.PlayerScript.PlayerInfo;
		}
	}
}