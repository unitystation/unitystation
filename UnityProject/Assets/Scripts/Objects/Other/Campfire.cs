using System;
using System.Collections.Generic;
using System.Linq;
using Core.Physics;
using Cysharp.Threading.Tasks;
using Health.Objects;
using Items.Food;
using Systems.Atmospherics;
using UnityEngine;
using Util.Independent.FluentRichText;

namespace Objects.Other
{
	public class Campfire : EnterTileBase, ICheckedInteractable<HandApply>, IExaminable
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
		[SerializeField] private float smokeMolesToAddToTheAtmosphere = 0.09f;
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
			components ??= GetComponent<CommonComponents>();
			if (CustomNetworkManager.IsServer == false) return;
			ChangeState(currentState);
			if (components.TrySafeGetComponent<Integrity>(out var integrity))
			{
				integrity.OnApplyDamage += OnApplyDamage;
			}
		}

		private void OnApplyDamage(DamageInfo dmgInfo)
		{
			if (dmgInfo.DamageType != DamageType.Burn ||
			    dmgInfo.AttackType != AttackType.Energy  || dmgInfo.AttackType != AttackType.Fire) return;
			switch (currentState)
			{
				case CampfireState.Lit:
					AddStacks((int)(dmgInfo.Damage * 2));
					break;
				case CampfireState.Unlit:
				default:
					LightCampUp();
					break;
			}
		}

		private void UpdateMe()
		{
			var objectsOnSameTile = registerObject.Matrix.Get<CommonComponents>(registerObject.LocalPosition, true).ToList();
			AddStacks(-objectsOnSameTile.Count);
			foreach (var obj in objectsOnSameTile)
			{
				AffectObjectOnCampfire(obj);
			}
			AtmosInteractions();
			SetSpritesBasedOnStatus();
		}

		private void ChangeState(CampfireState newState)
		{
			if (currentState == newState) return;
			var previousState = currentState;
			currentState = newState;
			switch (currentState)
			{
				case CampfireState.Lit:
					UpdateManager.Add(UpdateMe,secondsPerStack);
					stacks = maxStacks;
					break;
				case CampfireState.Unlit:
				default:
					UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
					break;
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
					LightCampUp();
					Chat.AddActionMsgToChat(interaction.Performer, $"{interaction.PerformerPlayerScript.visibleName} has successfully started a fire with the {interaction.HandObject.ExpensiveName()}.".Color(Color.green));
					_ = Despawn.ServerSingle(interaction.HandObject);
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
				interaction.HandSlot.ItemStorage.ServerTryRemove(obj.gameObject, Destroy: true);
				AddStacks(1);
				return;
			}
			if (interaction.HandSlot.ItemStorage.ServerTryRemove(obj.gameObject))
			{
				var objComponents = obj.gameObject.GetCommonComponents();
				if (objComponents != null && objComponents.TrySafeGetComponent<UniversalObjectPhysics>(out var physics))
				{
					physics.AppearAtWorldPositionServer(registerObject.WorldPosition);
				}
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

		private void AtmosInteractions()
		{
			MetaDataNode node = registerObject.Matrix.MetaDataLayer.Get(registerObject.LocalPositionServer);
			if (currentState == CampfireState.Lit && node?.GasMixLocal.GetMoles(Gas.Oxygen) < 1)
			{
				ChangeState(CampfireState.Unlit);
				return;
			}
			node?.GasMixLocal.AddGasWithTemperature(Gas.Smoke, smokeMolesToAddToTheAtmosphere, Kelvin.FromC(100f));
			node?.GasMixLocal.ChangeTemperature(Kelvin.FromC(25f));
		}

		private void AffectObjectOnCampfire(CommonComponents common)
		{
			if (common == components) return;
			var commonsAttribute = common.SafeGetComponent<Attributes>();
			if (common.TrySafeGetComponent<Cookable>(out var cookable))
			{
				if (cookable.AddCookingTime(secondsPerStack / 12))
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

		public string Examine(Vector3 worldPos = default)
		{
			switch (currentState)
			{
				case CampfireState.Lit:
					return $"Campfire Stacks: {stacks}";
				case CampfireState.Unlit:
				default:
					return "This campfire is inactive";
			}
		}
	}
}