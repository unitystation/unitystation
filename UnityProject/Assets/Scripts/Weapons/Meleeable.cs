using System.Collections.Generic;
using UnityEngine;

//Do not derive from NetworkBehaviour, this is also used on tilemap layers
/// <summary>
/// Allows an object or tiles to be attacked by melee.
/// </summary>
public class Meleeable : MonoBehaviour, IPredictedCheckedInteractable<PositionalHandApply>
{
	[SerializeField]
	private static readonly StandardProgressActionConfig ProgressConfig
	= new StandardProgressActionConfig(StandardProgressActionType.Restrain);

	[SerializeField]
	private float butcherTime = 2.0f;

	[SerializeField]
	private string butcherSound = "BladeSlice";

	/// <summary>
	/// Which layers are allowed to be attacked on tiles regardless of intent
	/// </summary>
	private static readonly HashSet<LayerType> attackableLayers = new HashSet<LayerType>(
	new[] {
		LayerType.Grills,
		LayerType.Walls,
		LayerType.Windows
	});

	/// <summary>
	/// Which layers are allowed to be attacked on tiles only on harm intent
	/// </summary>
	/// NOTE: Not allowing attacking base or floors now because it's annoying during combat when you misclick
	// private static readonly HashSet<LayerType> harmIntentOnlyAttackableLayers = new HashSet<LayerType>(
	// 	new[] {
	// 		LayerType.Base,
	// 		LayerType.Floors
	// 	});
	private static readonly HashSet<LayerType> harmIntentOnlyAttackableLayers = new HashSet<LayerType>();

	private InteractableTiles interactableTiles;

	private void Start()
	{
		interactableTiles = GetComponent<InteractableTiles>();
	}

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		//are we in range
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//must be targeting us
		if (interaction.TargetObject != gameObject) return false;
		//allowed to attack due to cooldown?
		//note: actual cooldown is started in WeaponNetworkActions melee logic on server side,
		//clientPredictInteraction on clientside
		if (Cooldowns.IsOn(interaction, CooldownID.Asset(CommonCooldowns.Instance.Melee, side)))
		{
			interaction = PositionalHandApply.Invalid;
			return true;
		}

		//not punching unless harm intent
		if (interaction.HandObject == null && interaction.Intent != Intent.Harm) return false;

		//if attacking tiles, only some layers are allowed to be attacked
		if (interactableTiles != null)
		{
			var tileAt = interactableTiles.LayerTileAt(interaction.WorldPositionTarget, true);

			//Nothing there, could be space?
			if (tileAt == null) return false;

			if (!attackableLayers.Contains(tileAt.LayerType))
			{
				return interaction.Intent == Intent.Harm && harmIntentOnlyAttackableLayers.Contains(tileAt.LayerType);
			}
		}

		return true;
	}


	public void ClientPredictInteraction(PositionalHandApply interaction)
	{
		//start clientside melee cooldown so we don't try to spam melee
		//requests to server
		Cooldowns.TryStartClient(interaction, CommonCooldowns.Instance.Melee);
	}

	//no rollback logic
	public void ServerRollbackClient(PositionalHandApply interaction) { }

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		var handObject = interaction.HandObject;

		bool emptyHand = interaction.HandSlot.IsEmpty;

		var wna = interaction.Performer.GetComponent<WeaponNetworkActions>();
		if (interactableTiles != null && !emptyHand)
		{
			//attacking tiles
			var tileAt = interactableTiles.LayerTileAt(interaction.WorldPositionTarget, true);
			if (tileAt == null)
			{
				return;
			}
			if (tileAt.TileType == TileType.Wall)
			{
				return;
			}
			wna.ServerPerformMeleeAttack(gameObject, interaction.TargetVector, BodyPartType.None, tileAt.LayerType);
			if (Validations.HasItemTrait(handObject, CommonTraits.Instance.Breakable))
			{
				handObject.GetComponent<ItemBreakable>().AddDamage();
			}
		}
		else
		{
			//attacking objects

			//butcher check
			GameObject victim = interaction.TargetObject;
			var healthComponent = victim.GetComponent<LivingHealthBehaviour>();

			if (healthComponent
			    && healthComponent.allowKnifeHarvest
			    && healthComponent.IsDead
			    && Validations.HasItemTrait(handObject, CommonTraits.Instance.Knife)
			    && interaction.Intent == Intent.Harm)
			{
				GameObject performer = interaction.Performer;

				var playerMove = victim.GetComponent<PlayerMove>();
				if (playerMove != null && playerMove.IsBuckled)
				{
					return;
				}
				void ProgressFinishAction()
				{
					LivingHealthBehaviour victimHealth = victim.GetComponent<LivingHealthBehaviour>();
					victimHealth.Harvest();
					SoundManager.PlayNetworkedAtPos(butcherSound, victim.RegisterTile().WorldPositionServer);
				}

				var bar = StandardProgressAction.Create(ProgressConfig, ProgressFinishAction)
					.ServerStartProgress(victim.RegisterTile(), butcherTime, performer);
			}
			else
			{
				if (gameObject.GetComponent<Integrity>() && emptyHand) return;

				wna.ServerPerformMeleeAttack(gameObject, interaction.TargetVector, interaction.TargetBodyPart, LayerType.None);
				if (Validations.HasItemTrait(handObject, CommonTraits.Instance.Breakable))
				{
					handObject.GetComponent<ItemBreakable>().AddDamage();
				}
			}
		}
	}

}