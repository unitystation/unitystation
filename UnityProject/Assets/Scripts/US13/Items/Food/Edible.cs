using System.Collections.Generic;
using Chemistry;
using Logs;
using Mirror;
using SecureStuff;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Core;
using US13.Core.Addressables.Types;
using US13.Core.Chat;
using US13.Core.Input_System;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.HealthV2.Living.Metabolism;
using US13.HealthV2.Living.PolymorphicSystems;
using US13.Items.Traits;
using US13.Managers;
using US13.Messages.Server.SoundMessages;
using US13.Player;
using US13.Systems.Inventory;
using US13.Systems.Score;
using US13.Tilemaps.Behaviours.Objects;
using US13.UI.Core.ProgressBar;
using US13.UI.Systems.MainHUD.UI_Bottom;
using US13.UI.Systems.Tooltips.HoverTooltips;
using Util;
using Random = UnityEngine.Random;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Items.Food
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
		[PlayModeOnly, SerializeField, SyncVar] private int currentBites;
		[SerializeField] private int maxBites = 1;
		[SerializeField] private float forceFeedTime = 3f;
		[SerializeField] private bool setCurrentBitesToMaxBitesOnServerSpawn = true;

		[SerializeField] private AddressableAudioSource sound = null;

		private float RandomPitch => Random.Range(0.7f, 1.3f);

		private static readonly StandardProgressActionConfig progressConfig = new(StandardProgressActionType.Restrain);

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
				Loggy.Error().Format("{0} prefab is missing ItemAttributes", Category.Objects, name);
			}

			ComponentsTracker<Edible>.Instances.Add(this);
		}

		private void OnDestroy()
		{
			ComponentsTracker<Edible>.Instances.Remove(this);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (setCurrentBitesToMaxBitesOnServerSpawn) currentBites = maxBites;
		}

		public void SetMaxBites(int newMaxBites, bool resetCurrentBites = false)
		{
			maxBites = newMaxBites;
			if (resetCurrentBites == false) return;
			currentBites = maxBites;
			if (stackable != null && stackable.Amount > 1)
			{
				stackable.ServerSetAmount(newMaxBites);
			}
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.Intent != Intent.Help) return false;
			return true;
		}

		/// <summary>
		/// Eat by activating from inventory
		/// </summary>
		public void ServerPerformInteraction(HandActivate interaction)
		{
			TryConsume(interaction.PerformerPlayerScript.gameObject);
		}

		public override void TryConsume(GameObject feederGo, GameObject eaterGo, bool projectileFed = false)
		{
			var eater = eaterGo.GetComponent<PlayerScript>();
			if (eater == null)
			{
				ConsumeAsNonPlayer();
				return;
			}

			if (CanPlayerConsume(eater) == false) return;

			if (feederGo.TryGetComponentCustom(out PlayerScript feeder) == false)
			{
				StartSelfFeed(eater);
				return;
			}

			if (projectileFed)
			{
				Eat(eater, feeder, projectileFed);
			}
			else if (feeder != eater)
			{
				StartForceFeed(feeder, eater);
			}
		}

		private void ConsumeAsNonPlayer()
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
		}

		private bool CanPlayerConsume(PlayerScript eater)
		{
			if (eater.Equipment.OrNull()?.CanConsume() == false)
			{
				Chat.AddExamineMsgFromServer(eater.gameObject, $"Remove items that cover your mouth first!");
				return false;
			}

			return true;
		}

		private HungerState GetHungerState(PlayerScript eater)
		{
			var sys = eater.playerHealth.GetSystem<HungerSystem>();
			if (sys != null)
			{
				return sys.CashedHungerState;
			}

			return HungerState.Normal;
		}

		private void StartForceFeed(PlayerScript feeder, PlayerScript eater)
		{
			var eaterHungerState = GetHungerState(eater);
			StandardProgressAction.Create(progressConfig, () =>
			{
				ConsumableTextUtils.SendGenericForceFeedMessage(feeder, eater, eaterHungerState, Name, "eat");
				Eat(eater, feeder);
			}).ServerStartProgress(eater.RegisterPlayer, forceFeedTime, feeder.gameObject);
		}

		private void StartSelfFeed(PlayerScript eater)
		{
			var eaterHungerState = GetHungerState(eater);
			ConsumableTextUtils.SendGenericConsumeMessage(eater, eater, eaterHungerState, Name, "eat");
			StandardProgressAction.Create(progressConfig, () =>
			{
				Eat(eater, eater);
			}).ServerStartProgress(eater.RegisterPlayer, consumeTime, eater.gameObject);
		}

		protected virtual void Eat(PlayerScript eater, PlayerScript feeder, bool projectileFed = false)
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
				if (feeder != null && eater == feeder)
				{
					Chat.AddActionMsgToChat(feeder.gameObject,
						"you try the stuff The food into your mouth but your stomach has no more room",
						$"{feeder.gameObject.ExpensiveName()} Tries to stuff food into the mouth but is unable to");
				}
				else if(feeder == null)
				{
					Chat.AddActionMsgToChat(feeder.gameObject,
						"You try and stuff more food into your targets mouth but no more seems to go in",
						$"{feeder.gameObject.ExpensiveName()} Tries to stuff food into Their targets mouth but no more food is going in");
				}
				else
				{
					Chat.AddActionMsgToChat(this.gameObject,
						$"You fly into {eater}'s mouth!",
						$"The {gameObject.ExpensiveName()} flies into {eater}'s mouth"); //maybe at some point a player might be the burger?
				}

				return;
			}

			if (SpareSpace < FoodContents.CurrentReagentMix.Total)
			{
				if(feeder == null)
				{
					Chat.AddActionMsgToChat(this.gameObject, $"You unwillingly get eaten by {eater}",
					$"{eater.gameObject.ExpensiveName()} Unwillingly force themselves to eat the food");

				}
				else
				{
					Chat.AddActionMsgToChat(feeder.gameObject, "You unwillingly eat the food",
					$"{eater.gameObject.ExpensiveName()} Unwillingly force themselves to eat the food");
				}
			}

			ReagentMix incomingFood;
			if (projectileFed == false) incomingFood = GetMixForBite(feeder);
			else incomingFood = FullConsume(feeder);

			foreach (var stomach in stomachs)
			{
				stomach.StomachContents.Add(incomingFood.Clone());
			}

			AudioSourceParameters eatSoundParameters = new AudioSourceParameters(pitch: RandomPitch);
			SoundManager.PlayNetworkedAtPos(sound, eater.WorldPos, eatSoundParameters, sourceObj: eater.gameObject);
			ScoreMachine.AddToScoreInt(1, RoundEndScoreBuilder.COMMON_SCORE_FOODEATEN);
		}

		public ReagentMix FullConsume(PlayerScript feeder)
		{
			ReagentMix incomingFood = FoodContents.CurrentReagentMix.Clone();
			SpawnLeavingsAndDespawn(feeder);
			return incomingFood;
		}

		public ReagentMix GetMixForBite(PlayerScript feeder)
		{
			ReagentMix incomingFood = FoodContents.CurrentReagentMix.Clone();

			if (stackable != null)
			{
				stackable.ServerConsume(1);
			}
			else
			{
				incomingFood.Divide(maxBites);
				currentBites--;

				if (currentBites <= 0)
				{
					SpawnLeavingsAndDespawn(feeder);
				}
			}

			return incomingFood;
		}

		private void SpawnLeavingsAndDespawn(PlayerScript feeder)
		{
			if (leavings != null)
			{
				var leavingsInstance = Spawn.ServerPrefab(leavings).GameObject;
				var pickupable = leavingsInstance.GetComponent<Pickupable>();
				bool added = false;
				var dropOn = gameObject;

				if (feeder != null)
				{
					var feederSlot = feeder.DynamicItemStorage.GetActiveHandSlot();
					dropOn = feeder.gameObject;
					added = Inventory.ServerAdd(pickupable, feederSlot, ReplacementStrategy.DropOther);
				}

				if (added == false)
				{
					pickupable.UniversalObjectPhysics.DropAtAndInheritMomentum(
						dropOn.GetComponent<UniversalObjectPhysics>());
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