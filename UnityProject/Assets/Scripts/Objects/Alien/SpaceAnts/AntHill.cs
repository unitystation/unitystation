﻿using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using Core;
using HealthV2;
using Items.Food;
using NUnit.Framework;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;
using Util.Independent.FluentRichText;
using Random = UnityEngine.Random;

namespace Objects.Alien.SpaceAnts
{
	public class AntHill : MonoBehaviour, ICheckedInteractable<HandApply>, IHoverTooltip
	{
		private List<LivingHealthMasterBase> inflictedMobs = new List<LivingHealthMasterBase>();
		[SerializeField] private float routineDelay = 30f;
		[SerializeField] private Vector2 minMaxAntDamage = new Vector2(1, 5);
		[SerializeField] private AttackType biteType = AttackType.Melee;
		[SerializeField] private DamageType damageType = DamageType.Brute;
		[SerializeField] private AntHillState state = AntHillState.Single;
		[SerializeField] private ItemTrait filthTrait;
		[SerializeField] private ItemTrait flySwatterTrait;
		[SerializeField] private List<SpriteDataSO> stateSprites = new List<SpriteDataSO>();
		[SerializeField] private SpriteHandler stateSpritesHandler;
		[SerializeField] private List<AddressableAudioSource> onStepSounds = new List<AddressableAudioSource>();
		[SerializeField] private List<GameObject> antMultiplyPrefabs = new List<GameObject>();
		[SerializeField] private bool eatsYourLiver = false;

		enum AntHillState
		{
			Single = 0,
			Small = 1,
			Large = 2,
			Swarm = 3,
		}

		private void Awake()
		{
			if(CustomNetworkManager.IsServer == false) return;
			UpdateManager.Add(AntRoutine, routineDelay);
		}

		private void OnDestroy()
		{
			if(CustomNetworkManager.IsServer == false) return;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, AntRoutine);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject != null &&
			    interaction.HandObject.TryGetComponent<Attributes>(out var attributes))
			{
				if (attributes.InitialTraits.Contains(flySwatterTrait))
				{
					Chat.AddExamineMsg(interaction.Performer, "You smack the ant hill so hard, all of its occupants turn into mush.");
					_ = Despawn.ServerSingle(gameObject);
					return;
				}
			}
			Chat.AddExamineMsg(interaction.Performer, "You reach your hand towards the ant hill.. and a swarm of ants attack you! Ouch!");
			InflictMob(interaction.Performer);
			PlayStepAudio();
		}

		public void InflictMob(GameObject mob)
		{
			if (state is not AntHillState.Swarm) return;
			if (DMMath.Prob(75) || mob.TryGetComponent<LivingHealthMasterBase>(out var health) == false) return;
			inflictedMobs.Add(health);
		}

		private void AntRoutine()
		{
			BiteInflictedMobs();
			LookForFood();
			LookForFilth();
		}

		private void BiteInflictedMobs()
		{
			var damage = state is AntHillState.Swarm ? Random.Range(minMaxAntDamage.x, minMaxAntDamage.y) : 1;
			var mobsToDispel = new List<LivingHealthMasterBase>();
			foreach (var mob in inflictedMobs)
			{
				if (mob == null) continue;
				if (( mob.MaxHealth - mob.OverallHealth) >  minMaxAntDamage.y) continue;
				mob.ApplyDamageToRandomBodyPart(gameObject, damage, biteType, damageType, damageSplit: true);
				Chat.AddExamineMsg(mob.gameObject, "you feel itchy all over yourself.");
				PlayStepAudio(mob.gameObject);
				if (DMMath.Prob(90)) mobsToDispel.Add(mob);
			}
			foreach (var mobToRemove in mobsToDispel)
			{
				inflictedMobs.Remove(mobToRemove);
			}
		}

		private void LookForFood()
		{
			var searchRadius = state switch
			{
				AntHillState.Single => 3,
				AntHillState.Small => 6,
				AntHillState.Large => 12,
				AntHillState.Swarm => 32,
				_ => 3
			};
			var edibles = ComponentsTracker<Edible>.GetAllNearbyTypesToTarget(gameObject, searchRadius, eatsYourLiver);
			if (edibles.Count > 0) AntEat(edibles.PickRandom().gameObject);
		}

		private void LookForFilth()
		{
			var edibles = ComponentsTracker<Attributes>.GetAllNearbyTypesToTarget(gameObject, 32, eatsYourLiver)
				.Where(x => x.InitialTraits.Contains(filthTrait)).ToList();
			if (edibles.Count == 0) return;
			var edible = edibles.PickRandom();
			if (DMMath.Prob(5) && state is AntHillState.Large or AntHillState.Swarm)
			{
				Spawn.ServerClone(antMultiplyPrefabs.PickRandom(), edible.gameObject.AssumedWorldPosServer());
			}
			AntEat(edible.gameObject);
		}

		private void AntEat(GameObject objectToEat)
		{
			Chat.AddLocalMsgToChat($"An army of ants appears and eats all of the {objectToEat.ExpensiveName()}".Color(Color.red), objectToEat);
			_ = Despawn.ServerSingle(objectToEat);
			if (state is AntHillState.Swarm) return;
			IncreaseState();
		}

		private void IncreaseState()
		{
			state++;
			stateSpritesHandler.SetSpriteSO(stateSprites[(int)state]);
		}

		private void PlayStepAudio(GameObject target = null)
		{
			if(onStepSounds.Count == 0) return;
			SoundManager.PlayNetworkedAtPos(onStepSounds.PickRandom(),
				target == null ? gameObject.AssumedWorldPosServer() : target.AssumedWorldPosServer());
		}

		public void AddFireStackToMob(GameObject mob)
		{
			if (mob.TryGetComponent<LivingHealthMasterBase>(out var health) == false) return;
			health.ChangeFireStacks(health.FireStacks + 1);
		}


		public string HoverTip()
		{
			var searchRadius = state switch
			{
				AntHillState.Single => "A Seemingly Empty Ant Hill",
				AntHillState.Small => "This is a Small Ant Hill",
				AntHillState.Large => "This Ant Hill is fairly decent sized.",
				AntHillState.Swarm => "A swarm of ants live within this ant hill.",
				_ => "An ant hill"
			};
			return searchRadius;
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
			List<TextColor> strings = new List<TextColor>()
			{
				new TextColor()
				{
					Color = Color.green,
					Text = "Destroy with a Fly Swatter"
				}
			};
			return strings;
		}
	}
}