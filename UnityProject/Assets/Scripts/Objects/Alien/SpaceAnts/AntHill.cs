using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using Core;
using HealthV2;
using Items.Food;
using UnityEngine;
using Util.Independent.FluentRichText;
using Random = UnityEngine.Random;

namespace Objects.Alien.SpaceAnts
{
	public class AntHill : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private List<LivingHealthMasterBase> inflictedMobs = new List<LivingHealthMasterBase>();
		[SerializeField] private float routineDelay = 30f;
		[SerializeField] private Vector2 minMaxAntDamage = new Vector2(2, 7);
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
			var damage = state is AntHillState.Small ? 1 : Random.Range(minMaxAntDamage.x, minMaxAntDamage.y);
			var mobsToDispel = new List<LivingHealthMasterBase>();
			foreach (var mob in inflictedMobs)
			{
				if (mob == null) continue;
				mob.ApplyDamageAll(gameObject, damage, biteType, damageType);
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
			var edibles = ComponentsTracker<Attributes>.GetAllNearbyTypesToTarget(gameObject, 32, eatsYourLiver);
			foreach (var edible in edibles.Where(x => x.InitialTraits.Contains(filthTrait)).Reverse())
			{
				if (DMMath.Prob(5))
				{
					Spawn.ServerClone(antMultiplyPrefabs.PickRandom(), edible.gameObject.AssumedWorldPosServer());
				}
				AntEat(edible.gameObject);
				if (DMMath.Prob(75)) return;
			}
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
	}
}