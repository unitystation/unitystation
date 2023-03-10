using UnityEngine;

namespace Items
{
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
			Logger.LogError($"[{gameObject.name}/AddBackToStorageOnDropOrThrow] - Something went wrong while trying to re-add this item back to their item storage.");
		}
	}
}