using System.Linq;
using Logs;
using UnityEngine;
using US13.Actions.V2.Trackers;
using US13.Core.Chat;
using US13.Managers.NetworkManagement;
using US13.Player;
using US13.Systems.Inventory;
using Util;

namespace US13.Items.Others
{
	[RequireComponent(typeof(FlashLight))]
	[RequireComponent(typeof(ItemSlotActionTracker))]
	public class BlindingFlashlight : MonoBehaviour
	{
		[SerializeField] private FlashLight flashLight;
		[SerializeField] private Pickupable pickupable;
		[SerializeField] private float flashingDuration = 4.0f;

		public void Start()
		{
			flashLight ??= GetComponent<FlashLight>();
			pickupable ??= GetComponent<Pickupable>();
		}

		public void AttemptBlind(Vector2 worldMousePosition)
		{
			var player = pickupable.ItemSlot.Player;
			if (pickupable.ItemSlot.Player == null)
			{
				Loggy.Error("How did we manage to run this without a player?");
				return;
			}
			if (flashLight.IsOn == false)
			{
				Chat.AddExamineMsg(player.gameObject, "You hear clicking sound from the flashlight, but nothing happens. It's turned off.");
			}

			var matrix = player.gameObject.RegisterTile().Matrix;
			var objectsOnTile = matrix
				.Get<PlayerScript>(worldMousePosition.To3Int().ToLocal(matrix).CutToInt(), CustomNetworkManager.IsServer).ToList();
			if (objectsOnTile.Count == 0)
			{
				Chat.AddExamineMsg(player.gameObject, "You click the flashlight, but no one is here to blind.");
				return;
			}

			var pos = worldMousePosition.To3Int().ToLocal();
			if (Vector3.Distance(player.gameObject.AssumedWorldPosServer().ToLocalInt(matrix), pos) > 14f)
			{
				Chat.AddExamineMsg(player.gameObject, "You try to blind someone with the flashlight, but they're too far away.");
				return;
			}
			Chat.AddActionMsgToChat(player.gameObject,
				$"{player.PlayerScript.visibleName}'s flashlight booms as it flashes brightly, blinding whoever it is being pointed at.");
			objectsOnTile[0].playerHealth.TryFlash(flashingDuration);
			objectsOnTile[0].playerHealth.TryDeafen(player.gameObject, flashingDuration);
		}
	}
}