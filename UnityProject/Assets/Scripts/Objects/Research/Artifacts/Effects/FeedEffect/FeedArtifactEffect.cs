using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Systems.Research
{
	[CreateAssetMenu(fileName = "FeedEffect", menuName = "ScriptableObjects/Systems/Artifacts/FeedEffect")]
	public class FeedArtifactEffect : ArtifactEffect
	{
		public ItemTrait[] acceptedItems;

		public string[] acceptedItemMessages;
		public string[] emptyHandMessages;
		public string[] wrongItemMessages;

		public bool despawnItemOnFeed = true;

		public int consumedItemAmount = 1;

		public int delayOnSuccess = 0;

		public virtual void DoEffectTouch(HandApply interaction)
		{
			//Hand touched
			if (interaction.HandObject == null)
			{
				BareHandEffect(interaction);
				return;
			}

			//Check for right itemtrait
			if (!Validations.HasAnyTrait(interaction.HandObject, acceptedItems))
			{
				WrongEffect(interaction);
				return;
			}

			CorrectEffect(interaction);
		}

		protected virtual void BareHandEffect(HandApply interaction)
		{
			Chat.AddWarningMsgFromServer(interaction.Performer, emptyHandMessages.PickRandom());
		}

		protected virtual void WrongEffect(HandApply interaction)
		{
			Chat.AddWarningMsgFromServer(interaction.Performer, emptyHandMessages.PickRandom());
		}

		protected virtual void CorrectEffect(HandApply interaction)
		{
			//Is correct item
			if (despawnItemOnFeed)
			{
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

			Chat.AddExamineMsgFromServer(interaction.Performer, acceptedItemMessages.PickRandom());
		}
	}
}

