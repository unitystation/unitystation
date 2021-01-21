using NaughtyAttributes;
using UnityEngine;
using WebSocketSharp;

namespace Clothing
{
	public class WearablePseudonym: MonoBehaviour, IServerInventoryMove
	{
		[SerializeField]
		[Tooltip("When wore in this slot, the pseudonym effect will take place.")]
		private NamedSlot slot = NamedSlot.mask;

		[SerializeField]
		[ReorderableList]
		[Tooltip("Populate this list to add suggested names. A name will be randomly chosen when the player" +
		         " triggers the setPseudonym screen. This is useful to for example suggest a random clown name.")]
		private string[] suggestedNicks = default;

		private string realName;

		public void OnInventoryMoveServer(InventoryMove info)
		{
			//Wearing
			if (info.ToSlot?.NamedSlot != null && info.ToSlot.NamedSlot == slot)
			{
				GetOrAddNewPseudonym(info.ToPlayer.PlayerScript);
			}
			//taking off
			if (info.FromSlot?.NamedSlot != null)
			{
				RemovePseudonym(info.FromPlayer.PlayerScript);
			}
		}

		private void GetOrAddNewPseudonym(PlayerScript player)
		{
			if (player.mind.Pseudonym.IsNullOrEmpty())
			{
				ShowPseudonymDialog();
				return;
			}

			realName = player.playerName;
			player.SetPermanentName(player.mind.Pseudonym);
		}

		private void RemovePseudonym(PlayerScript player)
		{
			if (realName.IsNullOrEmpty())
			{
				Logger.LogError($"Character with name {player.playerName} tried to get back their real name" +
				                $" but it isn't stored in {gameObject.name} for some forsaken reason", Category.Character);

				return;
			}

			player.SetPermanentName(realName);
		}

		private void ShowPseudonymDialog()
		{

		}
	}
}