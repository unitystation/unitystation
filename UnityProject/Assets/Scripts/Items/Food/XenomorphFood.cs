using System;
using UnityEngine;
using System.Threading.Tasks;
using HealthV2;
using Items;

namespace Items.Food
{
	[RequireComponent(typeof(RegisterItem))]
	[RequireComponent(typeof(ItemAttributesV2))]
	[RequireComponent(typeof(Edible))]
	public class XenomorphFood : Edible
	{
		[SerializeField]
		private int killTime = 400;
		[SerializeField]
		private GameObject larvae = null;

		private string Name => itemAttributes.ArticleName;
		private static readonly StandardProgressActionConfig ProgressConfig
			= new StandardProgressActionConfig(StandardProgressActionType.Restrain);

		public override void TryConsume(GameObject feederGO, GameObject eaterGO)
		{
			var eater = eaterGO.GetComponent<PlayerScript>();
			if (eater == null)
			{
				// TODO: implement non-player eating
				//SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos);
				_ = Despawn.ServerSingle(gameObject);
				return;
			}

			var feeder = feederGO.GetComponent<PlayerScript>();

			// Show eater message
			var eaterHungerState = eater.playerHealth.HungerState;
			ConsumableTextUtils.SendGenericConsumeMessage(feeder, eater, eaterHungerState, Name, "eat");

			// Check if eater can eat anything
			if (feeder != eater)  //If you're feeding it to someone else.
			{
				//Wait 3 seconds before you can feed
				StandardProgressAction.Create(ProgressConfig, () =>
				{
					ConsumableTextUtils.SendGenericForceFeedMessage(feeder, eater, eaterHungerState, Name, "eat");
					Eat(eater, feeder);
				}).ServerStartProgress(eater.registerTile, 3f, feeder.gameObject);
				return;
			}
			else
			{
				Eat(eater, feeder);
			}
		}

		public override void Eat(PlayerScript eater, PlayerScript feeder)
		{
			// TODO: missing sound?
			//SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos, sourceObj: eater.gameObject);

			var stomachs = eater.playerHealth.GetStomachs();
			if (stomachs.Count == 0)
			{
				//No stomachs?!
				return;
			}
			FoodContents.Divide(stomachs.Count);
			foreach (var stomach in stomachs)
			{
				stomach.StomachContents.Add(FoodContents.CurrentReagentMix.Clone());
			}

			_ = Pregnancy(eater.playerHealth);
			var feederSlot = feeder.DynamicItemStorage.GetActiveHandSlot();
			Inventory.ServerDespawn(feederSlot);
		}

		private async Task Pregnancy(PlayerHealthV2 player)
		{
			await Task.Delay(TimeSpan.FromSeconds(killTime - (killTime / 8)));
			Chat.AddActionMsgToChat(player.gameObject, "Your stomach gurgles uncomfortably...",
				$"A dangerous sounding gurgle emanates from " + player.name + "!");
			await Task.Delay(TimeSpan.FromSeconds(killTime / 8));
			player.ApplyDamageToBodyPart(
				gameObject,
				200,
				AttackType.Internal,
				DamageType.Brute,
				BodyPartType.Chest);
			Spawn.ServerPrefab(larvae, player.gameObject.RegisterTile().WorldPositionServer);
		}
	}
}
