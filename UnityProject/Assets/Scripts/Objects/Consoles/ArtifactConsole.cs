using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items.Science;
using Objects.Research;

namespace Objects.Research
{
	public class ArtifactConsole : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private RegisterObject registerObject;
		public ItemStorage itemStorage;

		public Artifact connectedArtifact;
		public ArtifactDataDisk dataDisk;

		private void Awake()
		{
			itemStorage = GetComponent<ItemStorage>();
			registerObject = GetComponent<RegisterObject>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.HandSlot.IsEmpty) return false;
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.everyTraitOutThere[403])) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (itemStorage.GetIndexedItemSlot(0).IsEmpty)
			{
				Inventory.ServerTransfer(interaction.HandSlot, itemStorage.GetIndexedItemSlot(0));
				dataDisk = itemStorage.GetIndexedItemSlot(0).ItemObject.GetComponent<ArtifactDataDisk>();

				Chat.AddActionMsgToChat(interaction.Performer, "You insert the drive into the console.",
					interaction.Performer.ExpensiveName() + " inserts the dirve into the console.");
			}
			else
			{
				Chat.AddActionMsgToChat(interaction.Performer, gameObject.ExpensiveName() + " already contains a drive", gameObject.ExpensiveName() + " already contains a drive");
			}

		}
	}
}
