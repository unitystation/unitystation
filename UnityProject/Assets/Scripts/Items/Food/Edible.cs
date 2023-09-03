using System.Collections.Generic;
using AddressableReferences;
using Chemistry;
using Chemistry.Components;
using HealthV2.Living.PolymorphicSystems;
using Logs;
using UnityEngine;
using Random = UnityEngine.Random;
using Messages.Server.SoundMessages;
using Mirror;
using Systems.Score;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine.Serialization;

namespace Items.Food
{
	/// <summary>
	/// Indicates an edible object
	/// </summary>
	[RequireComponent(typeof(RegisterItem))]
	[RequireComponent(typeof(ItemAttributesV2))]
	[RequireComponent(typeof(ReagentContainer))]
	public class Edible : Consumable, ICheckedInteractable<HandActivate>, IHoverTooltip, IServerSpawn
	{
		public GameObject leavings;
		[SerializeField, SyncVar] private int currentBites;
		[SerializeField] private int maxBites = 1;
		[SerializeField] private float forceFeedTime = 3f;
		[SerializeField] private bool setCurrentBitesToMaxBitesOnServerSpawn = true;

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
				Loggy.LogErrorFormat("{0} prefab is missing ItemAttributes", Category.Objects, name);
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (setCurrentBitesToMaxBitesOnServerSpawn) currentBites = maxBites;
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
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
					var LeavingSpawned = Spawn.ServerPrefab(leavings, item.WorldPosition, transform.parent).GameObject;
					var Pickupable = this.GetComponent<Pickupable>();
					if (Pickupable != null && Pickupable.ItemSlot != null)
					{
						Inventory.ServerAdd(LeavingSpawned.GetComponent<Pickupable>(), Pickupable.ItemSlot,
							ReplacementStrategy.DropOther);
					}
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

			var sys = eater.playerHealth.GetSystem<HungerSystem>();
			HungerState eaterHungerState = HungerState.Normal;

			if (sys != null)
			{
				eaterHungerState = sys.CashedHungerState;
			}

			ConsumableTextUtils.SendGenericConsumeMessage(feeder, eater, eaterHungerState, Name, "eat");

			if (feeder != eater) //If you're feeding it to someone else.
			{
				StandardProgressAction.Create(ProgressConfig, () =>
				{
					ConsumableTextUtils.SendGenericForceFeedMessage(feeder, eater, eaterHungerState, Name, "eat");
					Eat(eater, feeder);
				}).ServerStartProgress(eater.RegisterPlayer, forceFeedTime, feeder.gameObject);
				return;
			}
			StandardProgressAction.Create(ProgressConfig, () =>
				{
					Eat(eater, feeder);
				}).ServerStartProgress(eater.RegisterPlayer, consumeTime, feeder.gameObject);
		}

		protected virtual void Eat(PlayerScript eater, PlayerScript feeder)
		{
			//TODO: Reimplement metabolism.
			var stomachs = eater.playerHealth.GetStomachs();
			if (stomachs.Count == 0)
			{
				//No stomachs?!
				return;
			}

			float SpareSpace = 0;

			foreach (var stomach in stomachs)
			{
				SpareSpace += stomach.StomachContents.SpareCapacity;
			}

			if (SpareSpace < 0.5f)
			{
				if (eater == feeder)
				{
					Chat.AddActionMsgToChat(feeder.gameObject,
						"you try the stuff The food into your mouth but your stomach has no more room",
						$"{feeder} Tries to stuff food into the mouth but is unable to");
				}
				else
				{
					Chat.AddActionMsgToChat(feeder.gameObject,
						"You try and stuff more food into your targets mouth but no more seems to go in",
						$"{feeder} Tries to stuff food into Their targets mouth but no more food is going in");
				}

				return;
			}

			if (SpareSpace < FoodContents.CurrentReagentMix.Total)
			{
				Chat.AddActionMsgToChat(feeder.gameObject, "You unwillingly eat the food",
					$"{eater} Unwillingly force themselves to eat the food");
			}

			ReagentMix incomingFood = GetMixForBite(feeder);

			foreach (var stomach in stomachs)
			{
				stomach.StomachContents.Add(incomingFood.Clone());
			}

			AudioSourceParameters eatSoundParameters = new AudioSourceParameters(pitch: RandomPitch);
			SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos, eatSoundParameters, sourceObj: eater.gameObject);
			ScoreMachine.AddToScoreInt(1, RoundEndScoreBuilder.COMMON_SCORE_FOODEATEN);
		}


		public ReagentMix GetMixForBite(PlayerScript feeder)
		{
			ReagentMix incomingFood = FoodContents.CurrentReagentMix.Clone();
			if (stackable == null) //Since it just consumes one
			{
				incomingFood.Divide(maxBites);
			}

			if (stackable != null)
			{
				stackable.ServerConsume(1);
			}
			else
			{

				currentBites--;


				if (currentBites <= 0)
				{
					if (leavings != null)
					{
						var leavingsInstance = Spawn.ServerPrefab(leavings).GameObject;
						var pickupable = leavingsInstance.GetComponent<Pickupable>();
						bool added = false;
						var ToDropOn = gameObject;

						if (feeder != null)
						{
							var feederSlot = feeder.DynamicItemStorage.GetActiveHandSlot();

							ToDropOn = feeder.gameObject;
							added = Inventory.ServerAdd(pickupable, feederSlot, ReplacementStrategy.DropOther);
						}

						if (added == false)
						{
							//If stackable has leavings and they couldn't go in the same slot, they should be dropped
							pickupable.UniversalObjectPhysics.DropAtAndInheritMomentum(
								ToDropOn.GetComponent<UniversalObjectPhysics>());
						}
					}
					_ = Inventory.ServerDespawn(gameObject);
				}
			}

			return incomingFood;
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