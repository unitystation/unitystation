using UnityEngine;
using HealthV2;
using Items.Implants.Organs;

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
			var eaterHungerState = eater.playerHealth.DigestiveSystem.HungerState;
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

		public override void Eat(PlayerScript eater, PlayerScript feeder)
		{
			// TODO: missing sound?
			//SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos, sourceObj: eater.gameObject);

			var Stomachs = eater.playerHealth.GetStomachs();
			if (Stomachs.Count == 0)
			{
				//No stomachs?!
				return;
			}


			bool success = false;
			foreach (var Stomach in Stomachs)
			{
				if (Stomach.AddObjectToStomach(this) == true)
				{
					success = true;
					break;
				}
			}

			if (success == false) return;

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

			Stomach stomachToImplant = player.GetStomachs()[0] as Stomach;
			if (stomachToImplant == null) return;

			stomachToImplant.RelatedPart.OrganStorage.ServerTryAdd(embryo);
		}
	}
}
