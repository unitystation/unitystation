using Logs;
using UnityEngine;

namespace Items
{
	/// <summary>
	/// Component that houses functionality for the OnDrop and OnThrow events too hook onto via the unity inspector,
	/// or dynamically. Does not do anything on its own and requires the OnDropOrThrow function to be subscribed to an event.
	/// Re-adds this item back to an item storage when dropped, mainly used for things like the Defib Paddles.
	/// </summary>
	public class AddBackToStorageOnDropOrThrow : MonoBehaviour
	{
		[SerializeField] private ItemStorage storage;
		[SerializeField] private string OnAddBackMessage = "The paddles spring back into its storage unit.";

		private void Start()
		{
			if (storage == null) storage = gameObject.PickupableOrNull().ItemSlot.ItemStorage;
		}

		public void OnDropOrThrow(GameObject droppedObject)
		{
			if (storage == null) return;
			if (storage.ServerTryAdd(gameObject))
			{
				Chat.AddActionMsgToChat(gameObject, OnAddBackMessage);
				return;
			}
			Loggy.LogError($"[{gameObject.name}/AddBackToStorageOnDropOrThrow] - Something went wrong while trying to re-add this item back to their item storage.");
		}
	}
}