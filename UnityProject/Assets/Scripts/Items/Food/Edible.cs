using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AddressableReferences;
using Chemistry;
using Chemistry.Components;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Messages.Server.SoundMessages;
using Mirror;
using Systems.Score;
using UI.Systems.Tooltips.HoverTooltips;

namespace Items.Food
{
	/// <summary>
	/// Indicates an edible object
	/// </summary>
	[RequireComponent(typeof(RegisterItem))]
	[RequireComponent(typeof(ItemAttributesV2))]
	[RequireComponent(typeof(ReagentContainer))]
	public class Edible : Consumable, ICheckedInteractable<HandActivate>, IHoverTooltip
	{
		public GameObject leavings;
		[SerializeField, SyncVar] private int currentBites;
		[SerializeField] private int maxBites = 1;
		[SerializeField] private bool setCurrentBitesToMaxBitesOnAwake = true;

		[SerializeField] private AddressableAudioSource sound = null;

		private float RandomPitch => Random.Range(0.7f, 1.3f);

		private static readonly StandardProgressActionConfig ProgressConfig
			= new StandardProgressActionConfig(StandardProgressActionType.Restrain);

		protected ItemAttributesV2 itemAttributes;
		private Stackable stackable;
		private RegisterItem item;
		protected ReagentContainer FoodContents;

		private string Name => itemAttributes.ArticleName;

		private void Awake()
		{
			FoodContents = GetComponent<ReagentContainer>();
			item = GetComponent<RegisterItem>();
			itemAttributes = GetComponent<ItemAttributesV2>();
			stackable = GetComponent<Stackable>();

			if (itemAttributes != null)
			{
				itemAttributes.AddTrait(CommonTraits.Instance.Food);
			}
			else
			{
				Logger.LogErrorFormat("{0} prefab is missing ItemAttributes", Category.Objects, name);
			}

			if (setCurrentBitesToMaxBitesOnAwake) currentBites = maxBites;
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			return true;
		}

		/// <summary>
		/// Eat by activating from inventory
		/// </summary>
		public void ServerPerformInteraction(HandActivate interaction)
		{
			TryConsume(interaction.PerformerPlayerScript.gameObject);
		}

		public override void TryConsume(GameObject feederGO, GameObject eaterGO)
		{
			var eater = eaterGO.GetComponent<PlayerScript>();
			if (eater == null)
			{
				// todo: implement non-player eating
				AudioSourceParameters eatSoundParameters = new AudioSourceParameters(pitch: RandomPitch);
				SoundManager.PlayNetworkedAtPos(sound, item.WorldPosition, eatSoundParameters);
				if (leavings != null)
				{
					Spawn.ServerPrefab(leavings, item.WorldPosition, transform.parent);
				}

				_ = Despawn.ServerSingle(gameObject);
				return;
			}

			var feeder = feederGO.GetComponent<PlayerScript>();

			// Check if player is wearing clothing that prevents eating or drinking
			if (eater.Equipment.OrNull()?.CanConsume() == false)
			{
				Chat.AddExamineMsgFromServer(eater.gameObject, $"Remove items that cover your mouth first!");
				return;
			}

			// Show eater message
			var eaterHungerState = eater.playerHealth.HungerState;
			ConsumableTextUtils.SendGenericConsumeMessage(feeder, eater, eaterHungerState, Name, "eat");

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
			else
			{
				Eat(eater, feeder);
			}
		}

		protected virtual void Eat(PlayerScript eater, PlayerScript feeder)
		{
			//TODO: Reimplement metabolism.
			AudioSourceParameters eatSoundParameters = new AudioSourceParameters(pitch: RandomPitch);
			SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos, eatSoundParameters, sourceObj: eater.gameObject);

			var Stomachs = eater.playerHealth.GetStomachs();
			if (Stomachs.Count == 0)
			{
				//No stomachs?!
				return;
			}

			float SpareSpace = 0;

			foreach (var Stomach in Stomachs)
			{
				SpareSpace += Stomach.StomachContents.SpareCapacity;
			}

			if (SpareSpace < 0.5f)
			{
				if (eater == feeder)
				{
					Chat.AddActionMsgToChat(feeder.gameObject,
						"you try the stuff The food into your mouth but your stomach has no more room",
						"{performer} Tries to stuff food into the mouth but is unable to");
				}
				else
				{
					Chat.AddActionMsgToChat(feeder.gameObject,
						"You try and stuff more food into your targets mouth but no more seems to go in",
						"{performer} Tries to stuff food into Their targets mouth but no more food is going in");
				}

				return;
			}

			if (SpareSpace < FoodContents.CurrentReagentMix.Total)
			{
				Chat.AddActionMsgToChat(feeder.gameObject, "You unwillingly eat the food",
					"{performer} Unwillingly force themselves to eat the food");
			}

			ReagentMix incomingFood = FoodContents.CurrentReagentMix.Clone();


			incomingFood.Divide(Stomachs.Count);
			foreach (var Stomach in Stomachs)
			{
				Stomach.StomachContents.Add(incomingFood.Clone());
			}


			var feederSlot = feeder.DynamicItemStorage.GetActiveHandSlot();
			//If food has a stack component, decrease amount by one instead of deleting the entire stack.
			if (stackable != null)
			{
				stackable.ServerConsume(1);
			}
			else
			{
				Bite(feeder, feederSlot);
			}

			ScoreMachine.AddToScoreInt(1, RoundEndScoreBuilder.COMMON_SCORE_FOODEATEN);
		}

		private void Bite(PlayerScript feeder, ItemSlot feederSlot)
		{
			currentBites--;
			if (currentBites > 0) return;
			if (leavings != null)
			{
				var leavingsInstance = Spawn.ServerPrefab(leavings).GameObject;
				var pickupable = leavingsInstance.GetComponent<Pickupable>();
				bool added = Inventory.ServerAdd(pickupable, feederSlot);
				if (added == false)
				{
					//If stackable has leavings and they couldn't go in the same slot, they should be dropped
					pickupable.UniversalObjectPhysics.DropAtAndInheritMomentum(
						feeder.GetComponent<UniversalObjectPhysics>());
				}
			}
			_ = Inventory.ServerDespawn(gameObject);
		}

		public string HoverTip()
		{
			var biteStatus = "";
			if (currentBites == maxBites) biteStatus = "it is untouched.";
			if (currentBites < maxBites) biteStatus = "someone took a bite out of it.";
			if (currentBites <= maxBites / 2) biteStatus = "it is half eaten.";
			return $"It appears that {biteStatus}";
		}

		public string CustomTitle() { return null; }
		public Sprite CustomIcon() { return null; }
		public List<Sprite> IconIndicators() { return null; }

		public List<TextColor> InteractionsStrings()
		{
			var list = new List<TextColor>();
			list.Add(new TextColor { Color = Color.green, Text = "Click on target to feed." });
			list.Add(new TextColor { Color = Color.green,
				Text = $"Press {KeybindManager.Instance.userKeybinds[KeyAction.HandActivate].PrimaryCombo} to feed yourself." });
			return list;
		}
	}
}