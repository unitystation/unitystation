using System.Collections.Generic;
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
		[SerializeField] private bool isLit = false;
		[SerializeField] private List<ItemTrait> lightingItems;
		[SerializeField] private List<ItemTrait> proddingItems;
		[SerializeField] private int stacks = 0;
		[SerializeField] private int maxStacks = 20;
		[SerializeField] private float secondsPerStack = 30f;
		[SerializeField] private float startingEffortTime = 8f;

		private RegisterObject registerObject => components.SafeGetComponent<RegisterObject>();
		private Attributes attributes => components.SafeGetComponent<Attributes>();


		protected override void Awake()
		{
			base.Awake();
			spriteHandler ??= GetComponentInChildren <SpriteHandler>();
			if (CustomNetworkManager.IsServer == false) return;
			SetSpritesBasedOnStatus();
		}

		private void UpdateMe()
		{
			AddStacks(-1);
			if (stacks <= 0)
			{
				isLit = false;
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			}
			SetSpritesBasedOnStatus();
		}

		private void SetSpritesBasedOnStatus()
		{
			spriteHandler.SetSpriteSO(isLit ? campfireActive : campfireInactive);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return interaction.HandObject != null && DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject == null) return;
			if (isLit) LitInteractions(interaction);
			else LightingUpInteractions(interaction);
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
			stacks = maxStacks;
			isLit = true;
			SetSpritesBasedOnStatus();
			UpdateManager.Add(UpdateMe,secondsPerStack);
		}

		private void AddStacks(int stacksToAdd)
		{
			stacks = Mathf.Clamp(stacks + stacksToAdd, 0, maxStacks);
		}
	}
}