using System;
using UnityEngine;
using System.Threading.Tasks;
using HealthV2;
using HealthV2.Living.PolymorphicSystems;
using Items;

namespace Items.Food
{
	[RequireComponent(typeof(RegisterItem))]
	[RequireComponent(typeof(ItemAttributesV2))]
	[RequireComponent(typeof(Edible))]
	public class XenomorphFood : Edible
	{
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
			var sys = eater.playerHealth.GetSystem<HungerSystem>();
			HungerState eaterHungerState = HungerState.Normal;

			if (sys != null)
			{
				eaterHungerState = sys.CashedHungerState;
			}
			ConsumableTextUtils.SendGenericConsumeMessage(feeder, eater, eaterHungerState, Name, "eat");

			// Check if eater can eat anything
			if (feeder != eater)  //If you're feeding it to someone else.
			{
				//Wait 3 seconds before you can feed
				StandardProgressAction.Create(ProgressConfig, () =>
				{
					ConsumableTextUtils.SendGenericForceFeedMessage(feeder, eater, eaterHungerState, Name, "eat");
					Eat(eater, feeder);
				}).ServerStartProgress(eater.RegisterPlayer, 3f, feeder.gameObject);
				return;
			}
			else
			{
				Eat(eater, feeder);
			}
		}

		protected override void Eat(PlayerScript eater, PlayerScript feeder)
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

			Pregnancy(eater.playerHealth);
			var feederSlot = feeder.DynamicItemStorage.GetActiveHandSlot();
			Inventory.ServerDespawn(feederSlot);
		}

		private void Pregnancy(PlayerHealthV2 player)
		{
			Chat.AddActionMsgToChat(player.gameObject, "Your stomach gurgles uncomfortably...",
			$"A dangerous sounding gurgle emanates from " + player.name + "!");

			GameObject embryo = Spawn.ServerPrefab(larvae, SpawnDestination.At(gameObject), 1).GameObject;

			if (player.GetStomachs().Count == 0) return;

			player.GetStomachs()[0].RelatedPart.OrganStorage.ServerTryAdd(embryo);
		}
	}
}
