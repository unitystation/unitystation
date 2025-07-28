using System.Collections.Generic;
using UnityEngine;
using Util.Independent.FluentRichText;

namespace Objects.Other
{
	public class Campfire : EnterTileBase, ICheckedInteractable<HandApply>
	{
		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private SpriteDataSO campfireActive;
		[SerializeField] private SpriteDataSO campfireInactive;
		[SerializeField] private bool isLit = false;
		[SerializeField] private List<ItemTrait> lightingItems;
		[SerializeField] private int stacks = 0;
		[SerializeField] private int maxStacks = 20;
		[SerializeField] private float secondsPerStack = 30f;
		[SerializeField] private float startingEffortTime = 8f;


		protected override void Awake()
		{
			base.Awake();
			spriteHandler ??= GetComponentInChildren <SpriteHandler>();
			if (CustomNetworkManager.IsServer == false) return;
			SetSpritesBasedOnStatus();
		}

		private void UpdateMe()
		{

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

		}

		private void LightCampUp()
		{
			stacks = maxStacks;
			isLit = true;
			SetSpritesBasedOnStatus();
		}
	}
}