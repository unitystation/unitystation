using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using Chemistry;
using Chemistry.Components;
using HealthV2.Living.PolymorphicSystems;
using Logs;
using UnityEngine;

namespace Items
{
	/// <summary>
	/// Indicates an edible object
	/// </summary>
	[RequireComponent(typeof(RegisterItem))]
	[RequireComponent(typeof(ItemAttributesV2))]
	[RequireComponent(typeof(ReagentContainer))]
	public class NotHandEdible : Consumable
	{
		[SerializeField]
		private GameObject leavings;

		[SerializeField]
		private AddressableAudioSource sound = null;

		private static readonly StandardProgressActionConfig ProgressConfig
			= new StandardProgressActionConfig(StandardProgressActionType.Restrain);

		[SerializeField]
		private int StartingNutrients = 10;

		[SerializeField]
		private Reagent Nutriment;

		protected ItemAttributesV2 itemAttributes;
		private Stackable stackable;
		private RegisterItem item;
		private ReagentContainer FoodContents;

		private string Name => itemAttributes.ArticleName;

		private void Awake()
		{
			FoodContents = GetComponent<ReagentContainer>();
			item = GetComponent<RegisterItem>();
			itemAttributes = GetComponent<ItemAttributesV2>();
			stackable = GetComponent<Stackable>();

			FoodContents.Add(new ReagentMix(Nutriment, StartingNutrients, TemperatureUtils.ToKelvin(20f, TemeratureUnits.C)));

			if (itemAttributes != null)
			{
				itemAttributes.AddTrait(CommonTraits.Instance.Food);
			}
			else
			{
				Loggy.LogErrorFormat("{0} prefab is missing ItemAttributes", Category.Objects, name);
			}
		}

		public override void TryConsume(GameObject feederGO, GameObject eaterGO)
		{
			var eater = eaterGO.GetComponent<PlayerScript>();
			if (eater == null)
			{
				// todo: implement non-player eating
				SoundManager.PlayNetworkedAtPos(sound, item.WorldPosition);
				if (leavings != null)
				{
					Spawn.ServerPrefab(leavings, item.WorldPosition, transform.parent);
				}

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
			if (eaterHungerState != HungerState.Full)
			{
				if (feeder != eater) //If you're feeding it to someone else.
				{
					//Wait 3 seconds before you can feed
					StandardProgressAction.Create(ProgressConfig, () =>
					{
						ConsumableTextUtils.SendGenericForceFeedMessage(feeder, eater, eaterHungerState, Name, "eat");
						Eat(eater, feeder);
					}).ServerStartProgress(eater.RegisterPlayer, 3f, feeder.gameObject);
					return;
				}

				Eat(eater, feeder);
			}
		}

		public virtual void Eat(PlayerScript eater, PlayerScript feeder)
		{
			//TODO: Reimplement metabolism.
			SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos, sourceObj: eater.gameObject);

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

			var feederSlot = feeder.DynamicItemStorage.GetActiveHandSlot();

			//If food has a stack component, decrease amount by one instead of deleting the entire stack.
			if (stackable != null)
			{
				stackable.ServerConsume(1);
			}
			else
			{
				_ = Inventory.ServerDespawn(gameObject);
			}

			if (leavings != null)
			{
				var leavingsInstance = Spawn.ServerPrefab(leavings).GameObject;
				var pickupable = leavingsInstance.GetComponent<Pickupable>();
				bool added = Inventory.ServerAdd(pickupable, feederSlot);
				if (!added)
				{
					//If stackable has leavings and they couldn't go in the same slot, they should be dropped
					pickupable.UniversalObjectPhysics.AppearAtWorldPositionServer(feeder.WorldPos);
				}
			}
		}
	}
}
