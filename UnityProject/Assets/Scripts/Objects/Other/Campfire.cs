using System.Collections.Generic;
using Health.Objects;
using Items.Food;
using UnityEngine;
using Util.Independent.FluentRichText;

namespace Objects.Other
{
	public class Campfire : EnterTileBase, ICheckedInteractable<HandApply>
	{
		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private CommonComponents components;
		[SerializeField] private SpriteDataSO campfireActive;
		[SerializeField] private SpriteDataSO campfireInactive;
		[SerializeField] private List<ItemTrait> lightingItems;
		[SerializeField] private List<ItemTrait> proddingItems;
		[SerializeField] private CampfireState currentState = CampfireState.Unlit;
		[SerializeField] private int stacks = 0;
		[SerializeField] private int maxStacks = 20;
		[SerializeField] private float secondsPerStack = 30f;
		[SerializeField] private float startingEffortTime = 8f;
		[SerializeField] private bool canAffectPlayersOnEnter = true;
		[SerializeField] private bool canAffectObjectsOnEnter = true;

		private RegisterObject registerObject => components.SafeGetComponent<RegisterObject>();
		private Attributes attributes => components.SafeGetComponent<Attributes>();

		private enum CampfireState
		{
			Unlit,
			Lit,
		}

		protected override void Awake()
		{
			base.Awake();
			spriteHandler ??= GetComponentInChildren <SpriteHandler>();
			if (CustomNetworkManager.IsServer == false) return;
			ChangeState(currentState);
		}

		private void UpdateMe()
		{
			AddStacks(-1);
			if (stacks <= 0)
			{
				ChangeState(CampfireState.Unlit);
			}
			SetSpritesBasedOnStatus();
		}

		private void ChangeState(CampfireState newState)
		{
			if (currentState == newState) return;
			if (currentState == CampfireState.Lit) // removing if previous state is Lit
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			}

			currentState = newState;

			if (currentState == CampfireState.Lit) // adding if new state is lit
			{
				UpdateManager.Add(UpdateMe,secondsPerStack);
				stacks = maxStacks;
			}

			SetSpritesBasedOnStatus();
		}

		private void SetSpritesBasedOnStatus()
		{
			switch (currentState)
			{
				case CampfireState.Lit:
					spriteHandler.SetSpriteSO(campfireActive);
					break;
				case CampfireState.Unlit:
				default:
					spriteHandler.SetSpriteSO(campfireInactive);
					break;
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return interaction.HandObject != null && DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject == null) return;
			switch (currentState)
			{
				case CampfireState.Unlit:
					LightingUpInteractions(interaction);
					break;
				case CampfireState.Lit:
					LitInteractions(interaction);
					break;
				default:
					Chat.AddExamineMsg(interaction.Performer, "huh?");
					break;
			}
		}

		private void LightingUpInteractions(HandApply interaction)
		{
			var commonComponents = interaction.HandObject.GetComponent<CommonComponents>();
			if (commonComponents.ItemAttributes.HasAnyTraitZeroAlloc(lightingItems) == false)
			{
				Chat.AddExamineMsg(interaction.Performer,
					$"The {commonComponents.ItemAttributes.ArticleName} you're holding isn't a good enough tool to start a campfire.");
				return;
			}
			Chat.AddActionMsgToChat(interaction.Performer, $"{interaction.PerformerPlayerScript.visibleName} is attempting to start the campfire with {commonComponents.ItemAttributes.ArticleName}");

			StandardProgressActionConfig cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction, false, false, false);
			StandardProgressAction.Create(cfg, () =>
			{
				if (DMMath.Prob(35) && interaction.HandObject)
				{
					Chat.AddActionMsgToChat(interaction.Performer, $"{interaction.PerformerPlayerScript.visibleName} has successfully started a fire with the {interaction.HandObject}.".Color(Color.green));
					_ = Despawn.ServerSingle(interaction.HandObject);
					LightCampUp();
				}
				else
				{
					Chat.AddExamineMsg(interaction.Performer, "You've failed at trying to start a fire. Maybe try again?".Color(Color.yellow));
					SetSpritesBasedOnStatus();
				}
			}).ServerStartProgress(interaction.PerformerPlayerScript.RegisterPlayer.LocalPosition.To3(), startingEffortTime, interaction.Performer);
		}

		private void LitInteractions(HandApply interaction)
		{
			if (interaction.HandSlot.Item == null) return;
			var obj = interaction.HandSlot.Item.ItemAttributesV2;
			if (obj.HasAnyTraitZeroAlloc(proddingItems))
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"{interaction.PerformerPlayerScript.visibleName} prods the {attributes.ArticleName} with the {obj.ArticleName}, making it lightly glow in reaction.");
				if (DMMath.Prob(25)) AddStacks(1);
				return;
			}
			if (obj.HasAnyTraitZeroAlloc(lightingItems))
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"{interaction.PerformerPlayerScript.visibleName} adds {obj.ArticleName} as fuel for the {attributes.ArticleName}, extending its lifespan.");
				AddStacks(1);
				return;
			}
			if (interaction.HandSlot.ItemStorage.ServerTryRemove(obj.gameObject, DroppedAtWorldPositionOrThrowVector: registerObject.WorldPosition))
			{
				Chat.AddActionMsgToChat(interaction.Performer,
					$"{interaction.PerformerPlayerScript.visibleName} throws the {obj.ArticleName} onto the {attributes.ArticleName}'s fire.");
			}
			else
			{
				Chat.AddExamineMsg(interaction.Performer, "Something compels you not to throw that in the fire.");
			}
		}

		private void LightCampUp()
		{
			ChangeState(CampfireState.Lit);
			UpdateManager.Add(UpdateMe,secondsPerStack);
		}

		private void AddStacks(int stacksToAdd)
		{
			stacks = Mathf.Clamp(stacks + stacksToAdd, 0, maxStacks);
			if (stacks <= 0)
			{
				ChangeState(CampfireState.Unlit);
			}
		}

		private void AffectObjectOnCampfire(CommonComponents common)
		{
			var commonsAttribute = common.SafeGetComponent<Attributes>();
			if (common.TrySafeGetComponent<Cookable>(out var cookable))
			{
				if (cookable.AddCookingTime(secondsPerStack / 4))
				{
					Chat.AddActionMsgToChat(gameObject,
						$"The {commonsAttribute.ArticleName}'s aroma fills the air, as it is done being cooked on the {attributes.ArticleName}");
				}
				else
				{
					Chat.AddActionMsgToChat(gameObject,
						$"The {commonsAttribute.ArticleName} sizzles on the {attributes.ArticleName}.");
				}
				return;
			}

			if (common.TrySafeGetComponent<Flammable>(out var flammable))
			{
				flammable.AddFireStacks(1);
			}
		}

		public override bool WillAffectPlayer(PlayerScript playerScript)
		{
			return canAffectPlayersOnEnter;
		}

		public override bool WillAffectObject(GameObject eventData)
		{
			return canAffectObjectsOnEnter;
		}

		public override void OnPlayerStep(PlayerScript playerScript)
		{
			base.OnPlayerStep(playerScript);
			AddStacks(-5);
			if (DMMath.Prob(50))
			{
				playerScript.playerHealth.ChangeFireStacks(2f);
				Chat.AddExamineMsg(playerScript.GameObject, "You step on the campfire and catch on fire!".Color(Color.red));
			}
			else
			{
				Chat.AddExamineMsg(playerScript.GameObject, "You step on the campfire, and some of its flames get snuffed out.".Color(Color.yellow));
			}
		}

		public override void OnObjectEnter(GameObject eventData)
		{
			base.OnObjectEnter(eventData);
			if (eventData.TryGetComponent<CommonComponents>(out var common) == false) return;
			AffectObjectOnCampfire(common);
		}
	}
}