using System.Collections.Generic;
using UnityEngine;

//Do not derive from NetworkBehaviour, this is also used on tilemap layers
/// <summary>
/// Allows an object or tiles to be attacked by melee.
/// </summary>
public class Meleeable : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	[SerializeField]
	private ItemTrait butcherKnifeTrait;

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
		var playerScript = interaction.Performer.GetComponent<PlayerScript>();
		if (!playerScript.weaponNetworkActions.AllowAttack)
		{
			return false;
		}

		//not punching unless harm intent
		if (interaction.HandObject == null && interaction.Intent != Intent.Harm) return false;

		//if attacking tiles, only some layers are allowed to be attacked
		if (interactableTiles != null)
		{
			var tileAt = interactableTiles.LayerTileAt(interaction.WorldPositionTarget);
			if (!attackableLayers.Contains(tileAt.LayerType))
			{
				return interaction.Intent == Intent.Harm && harmIntentOnlyAttackableLayers.Contains(tileAt.LayerType);
			}
		}

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		var wna = interaction.Performer.GetComponent<WeaponNetworkActions>();
		if (interactableTiles != null)
		{
			//attacking tiles
			var tileAt = interactableTiles.LayerTileAt(interaction.WorldPositionTarget);
			wna.CmdRequestMeleeAttack(gameObject, interaction.TargetVector, BodyPartType.None, tileAt.LayerType);
		}
		else
		{
			//attacking objects

			//butcher check
			GameObject victim = interaction.TargetObject;
			var healthComponent = victim.GetComponent<LivingHealthBehaviour>();
			if (healthComponent && healthComponent.allowKnifeHarvest && healthComponent.IsDead && Validations.HasItemTrait(interaction.HandObject, butcherKnifeTrait) && interaction.Intent == Intent.Harm)
			{
				GameObject performer = interaction.Performer;

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
				wna.CmdRequestMeleeAttack(gameObject, interaction.TargetVector, interaction.TargetBodyPart, LayerType.None);
			}
		}
	}
}