using System.Collections.Generic;
using System.Linq;
using AddressableReferences;
using Core;
using HealthV2;
using Items.Food;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;
using Util.Independent.FluentRichText;

namespace Objects.Alien.SpaceAnts
{
	public class AntHill : MonoBehaviour, ICheckedInteractable<HandApply>, IHoverTooltip
	{
		[SerializeField] private float routineDelay = 30f;
		[SerializeField] private AttackType biteType = AttackType.Melee;
		[SerializeField] private DamageType damageType = DamageType.Brute;
		[SerializeField] private AntHillState state = AntHillState.Single;
		[SerializeField] private ItemTrait filthTrait;
		[SerializeField] private List<ItemTrait> flySwatterTrait;
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
				if (attributes.InitialTraits.Any(x => flySwatterTrait.Contains(x)))
				{
					Chat.AddExamineMsg(interaction.Performer, "You destroy the ant hill.");
					_ = Despawn.ServerSingle(gameObject);
					return;
				}
			}
			Chat.AddExamineMsg(interaction.Performer, "You reach your hand towards the ant hill.. and a swarm of ants attack you! Ouch!");
			PlayStepAudio();
		}

		private void AntRoutine()
		{
			LookForFood();
			LookForFilth();
		}

		private void LookForFood()
		{
			var edibles = ComponentsTracker<Edible>.GetAllNearbyTypesToTarget(gameObject, SearchRadius(), eatsYourLiver);
			if (edibles.Count == 0) return;
			AntEat(edibles.PickRandom().gameObject);
		}

		private int SearchRadius()
		{
			var searchRadius = state switch
			{
				AntHillState.Single => 2,
				AntHillState.Small => 3,
				AntHillState.Large => 4,
				AntHillState.Swarm => 5,
				_ => 3
			};
			return searchRadius;
		}

		private void LookForFilth()
		{
			var edibles = ComponentsTracker<Attributes>.GetAllNearbyTypesToTarget(gameObject, SearchRadius(), eatsYourLiver)
				.Where(x => x.InitialTraits.Contains(filthTrait)).ToList();
			if (edibles.Count == 0) return;
			var edible = edibles.PickRandom();
			if (DMMath.Prob(3) && state is AntHillState.Large or AntHillState.Swarm)
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
					Color = Color.red,
					Text = "Destroy with a Fly Swatter or a bar of soap"
				}
			};
			return strings;
		}
	}
}