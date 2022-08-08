using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Items;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "TransmuteArtifactEffect", menuName = "ScriptableObjects/Systems/Artifacts/TransmuteEffect")]
	public class TransmuteArtifactEffect : InteractEffectBase
	{
		public GameObject[] ItemResults = null;

		int inputtedItem = 0;

		protected override void BareHandEffect(HandApply interaction)
		{
			Chat.AddWarningMsgFromServer(interaction.Performer, emptyHandMessages.PickRandom());
		}

		protected override void WrongEffect(HandApply interaction)
		{
			Chat.AddWarningMsgFromServer(interaction.Performer, emptyHandMessages.PickRandom());
		}

		protected override void CorrectEffect(HandApply interaction)
		{
			//Is correct item
			if (despawnItemOnFeed)
			{
				if (interaction.HandObject.TryGetComponent<ItemAttributesV2>(out var attributes))
				{
					foreach (ItemTrait itemTrait in attributes.GetTraits())
					{
						for (int i = 0; i < acceptedItems.Length; i++)
						{
							if (acceptedItems[i] == itemTrait)
							{
								inputtedItem = i;
								break;
							}
						}
					}

					if (interaction.HandObject.TryGetComponent<Stackable>(out var stackable))
					{
						if (!stackable.ServerConsume(consumedItemAmount))
						{
							//Not enough items in stack
							Chat.AddExamineMsgFromServer(interaction.Performer, "The artifact looks unimpressed");
							return;
						}
					}
					else
					{
						Despawn.ServerSingle(interaction.HandObject);
					}
				}

				Spawn.ServerPrefab(ItemResults[inputtedItem], SpawnDestination.At(interaction.Performer));
			}
		}
	}
}

