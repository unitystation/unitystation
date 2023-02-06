using System;
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

		private void OnDestroy()
		{
			pickupable.OnMoveToPlayerInventory.RemoveListener(SetLastTouch);
			pickupable = null;
			LastTouchedBy = null;
		}

		private void SetLastTouch(GameObject player)
		{
			if (player.TryGetComponent<PlayerScript>(out var info) == false) return;
			LastTouchedBy = info.PlayerInfo;
		}
	}
}