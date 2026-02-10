using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Items;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Lifecycle;
using US13.Items;
using Util;

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

			if (interaction.HandObject.TryGetComponent<ItemAttributesV2>(out var attributes))
			{
				for (int i = 0; i < acceptedItems.Length; i++)
				{
					if(Validations.HasItemTrait(interaction.HandObject, acceptedItems[i]))
					{
						inputtedItem = i;
						break;
					}
				}
			}

			if (interaction.HandObject.TryGetComponent<Stackable>(out var stackable) && despawnItemOnFeed)
			{
				if (stackable.ServerConsume(consumedItemAmount) == false)
				{
					//Not enough items in stack
					Chat.AddExamineMsgFromServer(interaction.Performer, "The artifact looks unimpressed");
					return;
				}
			}
			else if(despawnItemOnFeed)
			{
				Despawn.ServerSingle(interaction.HandObject);
			}
			
			Spawn.ServerPrefab(ItemResults[inputtedItem], SpawnDestination.At(interaction.Performer));
		}
	}
}

