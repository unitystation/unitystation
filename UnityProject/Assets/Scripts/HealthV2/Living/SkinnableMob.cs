using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using Mirror;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;
using Util.Independent.FluentRichText;

namespace HealthV2.Living
{
	/// <summary>
	/// A component that is used to define the behavior of skinning MobV2s to obtain skin and meat.
	/// </summary>
	[RequireComponent(typeof(LivingHealthMasterBase))]
	[RequireComponent(typeof(GibbableMob))]
	public class SkinnableMob : NetworkBehaviour, ICheckedInteractable<HandApply>, IHoverTooltip, ICooldown
	{
		[SerializeField] private GibbableMob gibbableMob;
		[SerializeField] private AddressableAudioSource defaultSkinningSound;
		private LivingHealthMasterBase health => gibbableMob.Health;

		private const int TIME_MIN_SKINNING = 6;
		private const int TIME_MAX_SKINNING = 30;

		private bool coolDownInteract = false;

		public float DefaultTime { get; } = 0.65f;

		private void Start()
		{
			if (gibbableMob == null) gibbableMob = GetComponent<GibbableMob>();
		}

		// NOTE: There is a flaw currently in the design of this, it heavily relies on the identified species of a character
		// This means that if a human transitions into a moth via surgery, they will still produce human meat instead of moth meat.
		// In the future, this should be updated to skin indiviual body parts and debone them.
		[Server]
		public void SpawnSpeciesProduce(int maximumProduce, bool gibOnDeathFromSkin = false)
		{
			if (health == null || health.InitialSpecies == null) return;
			if (health.InitialSpecies.Base.MeatProduce != null)
			{
				Spawn.ServerPrefab(health.InitialSpecies.Base.MeatProduce,
					gameObject.AssumedWorldPosServer(), count: Random.Range(1, maximumProduce),
					scatterRadius: 0.5f);
			}
			if (health.InitialSpecies.Base.SkinProduce != null)
			{
				Spawn.ServerPrefab(health.InitialSpecies.Base.SkinProduce,
					gameObject.AssumedWorldPosServer(), count: Random.Range(1, maximumProduce),
					scatterRadius: 0.5f);
			}
			PlayAudio();
			if (gibOnDeathFromSkin == false) return;
			DamageMob(null);
		}

		private void DamageMob(GameObject perp)
		{
			health.ApplyDamageAll(perp, Random.Range(health.MaxHealth / 6,  health.MaxHealth / 3), AttackType.Melee, DamageType.Brute,
				traumaticDamageTypes: TraumaticDamageTypes.SLASH, traumaChance: 100);
			if (health.IsDead) gibbableMob.OnGib();
		}

		private void PlayAudio()
		{
			// TODO: Gibbing sounds are different for various mobs. We'll need to read it from their species SO when we add them.
			_ = SoundManager.PlayAtPosition(defaultSkinningSound, gameObject.AssumedWorldPosServer(),
				gameObject);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.Intent != Intent.Harm && interaction.IsAltClick == false) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandObject == null) return false;
			if (interaction.HandObject.Item().GetTraits().Contains(health.InitialSpecies.Base.SkinningItemTrait) ==
			    false) return false;
			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Cooldowns.TryStart(interaction, this, NetworkSide.Server) == false) return;
			var bar = StandardProgressAction.Create(
				new StandardProgressActionConfig(StandardProgressActionType.Restrain, true, false),
				()=> SpawnSpeciesProduce(Random.Range(1, 3), true));
			var time = Mathf.Clamp(health.OverallHealth / 8, TIME_MIN_SKINNING, TIME_MAX_SKINNING);
			bar.ServerStartProgress(gibbableMob.gameObject.AssumedWorldPosServer(), time, interaction.Performer);
			Chat.AddActionMsgToChat(gameObject,
				$"{interaction.PerformerPlayerScript.visibleName} starts skinning {interaction.TargetObject.ExpensiveName()}.".Color(Color.red));
			PlayAudio();
			health.ApplyDamageToRandomBodyPart(interaction.Performer, 10, AttackType.Melee, DamageType.Brute,
				traumaticDamageTypes: TraumaticDamageTypes.PIERCE, traumaChance:100);
		}

		public string HoverTip()
		{
			return null;
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			var hands = PlayerManager.Equipment.ItemStorage.GetActiveHandSlot();
			RaceSOSingleton.TryGetRaceByName(gibbableMob.Mob.Mind.CurrentCharacterSettings.Species, out var species);
			var result = new List<TextColor>();
			if (hands != null && hands.ItemAttributes != null && hands.ItemAttributes.GetTraits().Contains(species.Base.SkinningItemTrait))
			{
				result.Add(new TextColor() {Text = "Alt click while in harm intent to skin this creature.", Color = Color.red});
			}
			return result;
		}
	}
}